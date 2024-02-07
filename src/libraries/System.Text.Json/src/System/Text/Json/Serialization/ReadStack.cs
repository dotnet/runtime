// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal struct ReadStack
    {
        /// <summary>
        /// Exposes the stack frame that is currently active.
        /// </summary>
        public ReadStackFrame Current;

        /// <summary>
        /// Gets the parent stack frame, if it exists.
        /// </summary>
        public readonly ref ReadStackFrame Parent
        {
            get
            {
                Debug.Assert(_count > 1);
                Debug.Assert(_stack is not null);
                return ref _stack[_count - 2];
            }
        }

        public readonly JsonPropertyInfo? ParentProperty
            => Current.HasParentObject ? Parent.JsonPropertyInfo : null;

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

        /// <summary>
        /// Indicates that the state still contains suspended frames waiting re-entry.
        /// </summary>
        public readonly bool IsContinuation => _continuationCount != 0;

        // The bag of preservable references.
        public ReferenceResolver ReferenceResolver;

        /// <summary>
        /// Whether we need to read ahead in the inner read loop.
        /// </summary>
        public bool SupportContinuation;

        /// <summary>
        /// Holds the value of $id or $ref of the currently read object
        /// </summary>
        public string? ReferenceId;

        /// <summary>
        /// Holds the value of $type of the currently read object
        /// </summary>
        public object? PolymorphicTypeDiscriminator;

        /// <summary>
        /// Global flag indicating whether we can read preserved references.
        /// </summary>
        public bool PreserveReferences;

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

        internal void Initialize(JsonTypeInfo jsonTypeInfo, bool supportContinuation = false)
        {
            JsonSerializerOptions options = jsonTypeInfo.Options;
            if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.Preserve)
            {
                ReferenceResolver = options.ReferenceHandler!.CreateResolver(writing: false);
                PreserveReferences = true;
            }

            Current.JsonTypeInfo = jsonTypeInfo;
            Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
            Current.NumberHandling = Current.JsonPropertyInfo.EffectiveNumberHandling;
            Current.CanContainMetadata = PreserveReferences || jsonTypeInfo.PolymorphicTypeResolver?.UsesTypeDiscriminators == true;
            SupportContinuation = supportContinuation;
        }

        public void Push()
        {
            if (_continuationCount == 0)
            {
                if (_count == 0)
                {
                    // Performance optimization: reuse the first stack frame on the first push operation.
                    // NB need to be careful when making writes to Current _before_ the first `Push`
                    // operation is performed.
                    _count = 1;
                }
                else
                {
                    JsonTypeInfo jsonTypeInfo = Current.JsonPropertyInfo?.JsonTypeInfo ?? Current.CtorArgumentState!.JsonParameterInfo!.JsonTypeInfo;
                    JsonNumberHandling? numberHandling = Current.NumberHandling;

                    EnsurePushCapacity();
                    _stack[_count - 1] = Current;
                    Current = default;
                    _count++;

                    Current.JsonTypeInfo = jsonTypeInfo;
                    Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
                    // Allow number handling on property to win over handling on type.
                    Current.NumberHandling = numberHandling ?? Current.JsonPropertyInfo.EffectiveNumberHandling;
                    Current.CanContainMetadata = PreserveReferences || jsonTypeInfo.PolymorphicTypeResolver?.UsesTypeDiscriminators == true;
                }
            }
            else
            {
                // We are re-entering a continuation, adjust indices accordingly.

                if (_count++ > 0)
                {
                    _stack[_count - 2] = Current;
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
            Debug.Assert(JsonPath() is not null);

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
        }

        /// <summary>
        /// Configures the current stack frame for a polymorphic converter.
        /// </summary>
        public JsonConverter InitializePolymorphicReEntry(JsonTypeInfo derivedJsonTypeInfo)
        {
            Debug.Assert(!IsContinuation);
            Debug.Assert(Current.PolymorphicJsonTypeInfo == null);
            Debug.Assert(Current.PolymorphicSerializationState == PolymorphicSerializationState.None);

            Current.PolymorphicJsonTypeInfo = Current.JsonTypeInfo;
            Current.JsonTypeInfo = derivedJsonTypeInfo;
            Current.JsonPropertyInfo = derivedJsonTypeInfo.PropertyInfoForTypeInfo;
            Current.NumberHandling ??= Current.JsonPropertyInfo.NumberHandling;
            Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
            SetConstructorArgumentState();

            return derivedJsonTypeInfo.Converter;
        }


        /// <summary>
        /// Configures the current frame for a continuation of a polymorphic converter.
        /// </summary>
        public JsonConverter ResumePolymorphicReEntry()
        {
            Debug.Assert(Current.PolymorphicJsonTypeInfo != null);
            Debug.Assert(Current.PolymorphicSerializationState == PolymorphicSerializationState.PolymorphicReEntrySuspended);

            // Swap out the two values as we resume the polymorphic converter
            (Current.JsonTypeInfo, Current.PolymorphicJsonTypeInfo) = (Current.PolymorphicJsonTypeInfo, Current.JsonTypeInfo);
            Current.PolymorphicSerializationState = PolymorphicSerializationState.PolymorphicReEntryStarted;
            return Current.JsonTypeInfo.Converter;
        }

        /// <summary>
        /// Updates frame state after a polymorphic converter has returned.
        /// </summary>
        public void ExitPolymorphicConverter(bool success)
        {
            Debug.Assert(Current.PolymorphicJsonTypeInfo != null);
            Debug.Assert(Current.PolymorphicSerializationState == PolymorphicSerializationState.PolymorphicReEntryStarted);

            // Swap out the two values as we exit the polymorphic converter
            (Current.JsonTypeInfo, Current.PolymorphicJsonTypeInfo) = (Current.PolymorphicJsonTypeInfo, Current.JsonTypeInfo);
            Current.PolymorphicSerializationState = success ? PolymorphicSerializationState.None : PolymorphicSerializationState.PolymorphicReEntrySuspended;
        }

        // Return a JSONPath using simple dot-notation when possible. When special characters are present, bracket-notation is used:
        // $.x.y[0].z
        // $['PropertyName.With.Special.Chars']
        public string JsonPath()
        {
            StringBuilder sb = new StringBuilder("$");

            (int frameCount, bool includeCurrentFrame) = _continuationCount switch
            {
                0 => (_count - 1, true), // Not a continuation, report previous frames and Current.
                1 => (0, true), // Continuation of depth 1, just report Current frame.
                int c => (c, false) // Continuation of depth > 1, report the entire stack.
            };

            for (int i = 0; i < frameCount; i++)
            {
                AppendStackFrame(sb, ref _stack[i]);
            }

            if (includeCurrentFrame)
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
                    if (propertyName.AsSpan().ContainsSpecialCharacters())
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

        // Traverses the stack for the outermost object being deserialized using constructor parameters
        // Only called when calculating exception information.
        public JsonTypeInfo GetTopJsonTypeInfoWithParameterizedConstructor()
        {
            Debug.Assert(!IsContinuation);

            for (int i = 0; i < _count - 1; i++)
            {
                if (_stack[i].JsonTypeInfo.UsesParameterizedConstructor)
                {
                    return _stack[i].JsonTypeInfo;
                }
            }

            Debug.Assert(Current.JsonTypeInfo.UsesParameterizedConstructor);
            return Current.JsonTypeInfo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetConstructorArgumentState()
        {
            if (Current.JsonTypeInfo.UsesParameterizedConstructor)
            {
                Current.CtorArgumentState ??= new();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"Path = {JsonPath()}, Current = ConverterStrategy.{Current.JsonTypeInfo?.Converter.ConverterStrategy}, {Current.JsonTypeInfo?.Type.Name}";
    }
}
