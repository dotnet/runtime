// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    [DebuggerDisplay("Path:{JsonPath()} Current: ConverterStrategy.{Current.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy}, {Current.JsonTypeInfo.Type.Name}")]
    internal struct ReadStack
    {
        internal static readonly char[] SpecialCharacters = { '.', ' ', '\'', '/', '"', '[', ']', '(', ')', '\t', '\n', '\r', '\f', '\b', '\\', '\u0085', '\u2028', '\u2029' };

        /// <summary>
        /// Exposes the stackframe that is currently active.
        /// </summary>
        public ReadStackFrame Current;

        /// <summary>
        /// Buffer containing all frames in the stack. For performance it is only populated for serialization depths > 1.
        /// </summary>
        private ReadStackFrame[] _stack;

        /// <summary>
        /// Tracks the current depth of the stack.
        /// </summary>
        private int _count;

        /// <summary>
        /// If not zero, indicates that the stack is part of a re-entrant continuation of given depth.
        /// </summary>
        private int _continuationCount;

        // State cache when deserializing objects with parameterized constructors.
        private List<ArgumentState>? _ctorArgStateCache;

        /// <summary>
        /// Bytes consumed in the current loop.
        /// </summary>
        public long BytesConsumed;

        /// <summary>
        /// Indicates that the state still contains suspended frames waiting re-entry.
        /// </summary>
        public bool IsContinuation => _continuationCount != 0;

        /// <summary>
        /// Internal flag to let us know that we need to read ahead in the inner read loop.
        /// </summary>
        public bool ReadAhead;

        // The bag of preservable references.
        public ReferenceResolver ReferenceResolver;

        /// <summary>
        /// Whether we need to read ahead in the inner read loop.
        /// </summary>
        public bool SupportContinuation;

        /// <summary>
        /// Whether we can read without the need of saving state for stream and preserve references cases.
        /// </summary>
        public bool UseFastPath;

        /// <summary>
        /// Ensures that the stack buffer has sufficient capacity to hold an additional frame.
        /// </summary>
        private void EnsurePushCapacity()
        {
            if (_stack is null)
            {
                _stack = new ReadStackFrame[4];
            }
            else if (_count - 1 == _stack.Length)
            {
                Array.Resize(ref _stack, 2 * _stack.Length);
            }
        }

        public void Initialize(Type type, JsonSerializerOptions options, bool supportContinuation)
        {
            JsonTypeInfo jsonTypeInfo = options.GetOrAddClassForRootType(type);
            Initialize(jsonTypeInfo, supportContinuation);
        }

        internal void Initialize(JsonTypeInfo jsonTypeInfo, bool supportContinuation = false)
        {
            Current.JsonTypeInfo = jsonTypeInfo;

            // The initial JsonPropertyInfo will be used to obtain the converter.
            Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;

            Current.NumberHandling = Current.JsonPropertyInfo.NumberHandling;

            JsonSerializerOptions options = jsonTypeInfo.Options;
            bool preserveReferences = options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve;
            if (preserveReferences)
            {
                ReferenceResolver = options.ReferenceHandler!.CreateResolver(writing: false);
            }

            SupportContinuation = supportContinuation;
            UseFastPath = !supportContinuation && !preserveReferences;
        }

        public void Push()
        {
            if (_continuationCount == 0)
            {
                if (_count == 0)
                {
                    // The first stack frame is held in Current.
                    _count = 1;
                }
                else
                {
                    JsonTypeInfo jsonTypeInfo;
                    JsonNumberHandling? numberHandling = Current.NumberHandling;
                    ConverterStrategy converterStrategy = Current.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy;

                    if (converterStrategy == ConverterStrategy.Object)
                    {
                        if (Current.JsonPropertyInfo != null)
                        {
                            jsonTypeInfo = Current.JsonPropertyInfo.RuntimeTypeInfo;
                        }
                        else
                        {
                            jsonTypeInfo = Current.CtorArgumentState!.JsonParameterInfo!.RuntimeTypeInfo;
                        }
                    }
                    else if (converterStrategy == ConverterStrategy.Value)
                    {
                        // Although ConverterStrategy.Value doesn't push, a custom custom converter may re-enter serialization.
                        jsonTypeInfo = Current.JsonPropertyInfo!.RuntimeTypeInfo;
                    }
                    else
                    {
                        Debug.Assert(((ConverterStrategy.Enumerable | ConverterStrategy.Dictionary) & converterStrategy) != 0);
                        jsonTypeInfo = Current.JsonTypeInfo.ElementTypeInfo!;
                    }

                    EnsurePushCapacity();
                    _stack[_count - 1] = Current;
                    Current = default;
                    _count++;

                    Current.JsonTypeInfo = jsonTypeInfo;
                    Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
                    // Allow number handling on property to win over handling on type.
                    Current.NumberHandling = numberHandling ?? Current.JsonPropertyInfo.NumberHandling;
                }
            }
            else
            {
                // We are re-entering a continuation, adjust indices accordingly
                if (_count++ > 0)
                {
                    Current = _stack[_count - 1];
                }

                // check if we are done
                if (_continuationCount == _count)
                {
                    _continuationCount = 0;
                }
            }

            SetConstructorArgumentState();
#if DEBUG
            // Ensure the method is always exercised in debug builds.
            _ = JsonPath();
#endif
        }

        public void Pop(bool success)
        {
            Debug.Assert(_count > 0);

            if (!success)
            {
                // Check if we need to initialize the continuation.
                if (_continuationCount == 0)
                {
                    if (_count == 1)
                    {
                        // No need to copy any frames here.
                        _continuationCount = 1;
                        _count = 0;
                        return;
                    }

                    // Need to push the Current frame to the stack,
                    // ensure that we have sufficient capacity.
                    EnsurePushCapacity();
                    _continuationCount = _count--;
                }
                else if (--_count == 0)
                {
                    // reached the root, no need to copy frames.
                    return;
                }

                _stack[_count] = Current;
                Current = _stack[_count - 1];
            }
            else
            {
                Debug.Assert(_continuationCount == 0);

                if (--_count > 0)
                {
                    Current = _stack[_count - 1];
                }
            }

            SetConstructorArgumentState();
        }

        // Return a JSONPath using simple dot-notation when possible. When special characters are present, bracket-notation is used:
        // $.x.y[0].z
        // $['PropertyName.With.Special.Chars']
        public string JsonPath()
        {
            StringBuilder sb = new StringBuilder("$");

            // If a continuation, always report back full stack.
            int count = Math.Max(_count, _continuationCount);

            for (int i = 0; i < count - 1; i++)
            {
                AppendStackFrame(sb, ref _stack[i]);
            }

            if (_continuationCount == 0)
            {
                AppendStackFrame(sb, ref Current);
            }

            return sb.ToString();

            static void AppendStackFrame(StringBuilder sb, ref ReadStackFrame frame)
            {
                // Append the property name.
                string? propertyName = GetPropertyName(ref frame);
                AppendPropertyName(sb, propertyName);

                if (frame.JsonTypeInfo != null && frame.IsProcessingEnumerable())
                {
                    if (frame.ReturnValue is not IEnumerable enumerable)
                    {
                        return;
                    }

                    // For continuation scenarios only, before or after all elements are read, the exception is not within the array.
                    if (frame.ObjectState == StackFrameObjectState.None ||
                        frame.ObjectState == StackFrameObjectState.CreatedObject ||
                        frame.ObjectState == StackFrameObjectState.ReadElements)
                    {
                        sb.Append('[');
                        sb.Append(GetCount(enumerable));
                        sb.Append(']');
                    }
                }
            }

            static int GetCount(IEnumerable enumerable)
            {
                if (enumerable is ICollection collection)
                {
                    return collection.Count;
                }

                int count = 0;
                IEnumerator enumerator = enumerable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    count++;
                }

                return count;
            }

            static void AppendPropertyName(StringBuilder sb, string? propertyName)
            {
                if (propertyName != null)
                {
                    if (propertyName.IndexOfAny(SpecialCharacters) != -1)
                    {
                        sb.Append(@"['");
                        sb.Append(propertyName);
                        sb.Append(@"']");
                    }
                    else
                    {
                        sb.Append('.');
                        sb.Append(propertyName);
                    }
                }
            }

            static string? GetPropertyName(ref ReadStackFrame frame)
            {
                string? propertyName = null;

                // Attempt to get the JSON property name from the frame.
                byte[]? utf8PropertyName = frame.JsonPropertyName;
                if (utf8PropertyName == null)
                {
                    if (frame.JsonPropertyNameAsString != null)
                    {
                        // Attempt to get the JSON property name set manually for dictionary
                        // keys and KeyValuePair property names.
                        propertyName = frame.JsonPropertyNameAsString;
                    }
                    else
                    {
                        // Attempt to get the JSON property name from the JsonPropertyInfo or JsonParameterInfo.
                        utf8PropertyName = frame.JsonPropertyInfo?.NameAsUtf8Bytes ??
                            frame.CtorArgumentState?.JsonParameterInfo?.NameAsUtf8Bytes;
                    }
                }

                if (utf8PropertyName != null)
                {
                    propertyName = JsonHelpers.Utf8GetString(utf8PropertyName);
                }

                return propertyName;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetConstructorArgumentState()
        {
            if (Current.JsonTypeInfo.IsObjectWithParameterizedCtor)
            {
                // A zero index indicates a new stack frame.
                if (Current.CtorArgumentStateIndex == 0)
                {
                    _ctorArgStateCache ??= new List<ArgumentState>();

                    var newState = new ArgumentState();
                    _ctorArgStateCache.Add(newState);

                    (Current.CtorArgumentStateIndex, Current.CtorArgumentState) = (_ctorArgStateCache.Count, newState);
                }
                else
                {
                    Current.CtorArgumentState = _ctorArgStateCache![Current.CtorArgumentStateIndex - 1];
                }
            }
        }
    }
}
