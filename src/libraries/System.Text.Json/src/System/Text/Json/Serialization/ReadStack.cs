// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    [DebuggerDisplay("Path:{JsonPath()} Current: ClassType.{Current.JsonClassInfo.ClassType}, {Current.JsonClassInfo.Type.Name}")]
    internal struct ReadStack
    {
        internal static readonly char[] SpecialCharacters = { '.', ' ', '\'', '/', '"', '[', ']', '(', ')', '\t', '\n', '\r', '\f', '\b', '\\', '\u0085', '\u2028', '\u2029' };

        /// <summary>
        /// The number of stack frames when the continuation started.
        /// </summary>
        private int _continuationCount;

        /// <summary>
        /// The number of stack frames including Current. _previous will contain _count-1 higher frames.
        /// </summary>
        private int _count;

        private List<ReadStackFrame> _previous;

        // State cache when deserializing objects with parameterized constructors.
        private List<ArgumentState>? _ctorArgStateCache;

        /// <summary>
        /// Bytes consumed in the current loop.
        /// </summary>
        public long BytesConsumed;

        // A field is used instead of a property to avoid value semantics.
        public ReadStackFrame Current;

        public bool IsContinuation => _continuationCount != 0;
        public bool IsLastContinuation => _continuationCount == _count;

        /// <summary>
        /// Internal flag to let us know that we need to read ahead in the inner read loop.
        /// </summary>
        public bool ReadAhead;

        // The bag of preservable references.
        public DefaultReferenceResolver ReferenceResolver;

        /// <summary>
        /// Whether we need to read ahead in the inner read loop.
        /// </summary>
        public bool SupportContinuation;

        private void AddCurrent()
        {
            if (_previous == null)
            {
                _previous = new List<ReadStackFrame>();
            }

            if (_count > _previous.Count)
            {
                // Need to allocate a new array element.
                _previous.Add(Current);
            }
            else
            {
                // Use a previously allocated slot.
                _previous[_count - 1] = Current;
            }

            _count++;
        }

        public void Initialize(Type type, JsonSerializerOptions options, bool supportContinuation)
        {
            JsonClassInfo jsonClassInfo = options.GetOrAddClass(type);

            Current.JsonClassInfo = jsonClassInfo;

            // The initial JsonPropertyInfo will be used to obtain the converter.
            Current.JsonPropertyInfo = jsonClassInfo.PropertyInfoForClassInfo;

            if (options.ReferenceHandling.ShouldReadPreservedReferences())
            {
                ReferenceResolver = new DefaultReferenceResolver(writing: false);
            }

            SupportContinuation = supportContinuation;
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
                    JsonClassInfo jsonClassInfo;
                    if (Current.JsonClassInfo.ClassType == ClassType.Object)
                    {
                        if (Current.JsonPropertyInfo != null)
                        {
                            jsonClassInfo = Current.JsonPropertyInfo.RuntimeClassInfo;
                        }
                        else
                        {
                            jsonClassInfo = Current.CtorArgumentState!.JsonParameterInfo!.RuntimeClassInfo;
                        }
                    }
                    else if ((Current.JsonClassInfo.ClassType & (ClassType.Value | ClassType.NewValue)) != 0)
                    {
                        // Although ClassType.Value doesn't push, a custom custom converter may re-enter serialization.
                        jsonClassInfo = Current.JsonPropertyInfo!.RuntimeClassInfo;
                    }
                    else
                    {
                        jsonClassInfo = Current.JsonClassInfo.ElementClassInfo!;
                    }

                    AddCurrent();
                    Current.Reset();

                    Current.JsonClassInfo = jsonClassInfo;
                    Current.JsonPropertyInfo = jsonClassInfo.PropertyInfoForClassInfo;
                }
            }
            else if (_continuationCount == 1)
            {
                // No need for a push since there is only one stack frame.
                Debug.Assert(_count == 1);
                _continuationCount = 0;
            }
            else
            {
                // A continuation; adjust the index.
                Current = _previous[_count - 1];

                // Check if we are done.
                if (_count == _continuationCount)
                {
                    _continuationCount = 0;
                }
                else
                {
                    _count++;
                }
            }

            SetConstrutorArgumentState();
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
                        // No need for a continuation since there is only one stack frame.
                        _continuationCount = 1;
                    }
                    else
                    {
                        AddCurrent();
                        _count--;
                        _continuationCount = _count;
                        _count--;
                        Current = _previous[_count - 1];
                    }

                    return;
                }

                if (_continuationCount == 1)
                {
                    // No need for a pop since there is only one stack frame.
                    Debug.Assert(_count == 1);
                    return;
                }

                // Update the list entry to the current value.
                _previous[_count - 1] = Current;

                Debug.Assert(_count > 0);
            }
            else
            {
                Debug.Assert(_continuationCount == 0);
            }

            if (_count > 1)
            {
                Current = _previous[--_count -1];
            }

            SetConstrutorArgumentState();
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
                AppendStackFrame(sb, _previous[i]);
            }

            if (_continuationCount == 0)
            {
                AppendStackFrame(sb, Current);
            }

            return sb.ToString();

            static void AppendStackFrame(StringBuilder sb, in ReadStackFrame frame)
            {
                // Append the property name.
                string? propertyName = GetPropertyName(frame);
                AppendPropertyName(sb, propertyName);

                if (frame.JsonClassInfo != null && frame.IsProcessingEnumerable())
                {
                    IEnumerable? enumerable = (IEnumerable?)frame.ReturnValue;
                    if (enumerable == null)
                    {
                        return;
                    }

                    // Once all elements are read, the exception is not within the array.
                    if (frame.ObjectState < StackFrameObjectState.ReadElements)
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

            static string? GetPropertyName(in ReadStackFrame frame)
            {
                string? propertyName = null;

                // Attempt to get the JSON property name from the frame.
                byte[]? utf8PropertyName = frame.JsonPropertyName;
                if (utf8PropertyName == null)
                {
                    // Attempt to get the JSON property name from the JsonPropertyInfo or JsonParameterInfo.
                    utf8PropertyName = frame.JsonPropertyInfo?.NameAsUtf8Bytes ??
                        frame.CtorArgumentState?.JsonParameterInfo?.NameAsUtf8Bytes;

                    if (utf8PropertyName == null)
                    {
                        // Attempt to get the JSON property name set manually for dictionary
                        // keys and serializer re-entry cases where a property is specified.
                        propertyName = frame.JsonPropertyNameAsString;
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
        private void SetConstrutorArgumentState()
        {
            if (Current.JsonClassInfo.ParameterCount > 0)
            {
                // A zero index indicates a new stack frame.
                if (Current.CtorArgumentStateIndex == 0)
                {
                    if (_ctorArgStateCache == null)
                    {
                        _ctorArgStateCache = new List<ArgumentState>();
                    }

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
