// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    [DebuggerDisplay("Path:{PropertyPath()} Current: ClassType.{Current.JsonClassInfo.ClassType}, {Current.JsonClassInfo.Type.Name}")]
    internal struct WriteStack
    {
        // Fields are used instead of properties to avoid value semantics.
        public WriteStackFrame Current;
        private List<WriteStackFrame> _previous;
        private int _index;

        private ReferenceResolver _referenceResolver;
        private HashSet<object> _referenceStack;

        public void Push()
        {
            if (_previous == null)
            {
                _previous = new List<WriteStackFrame>();
            }

            if (_index == _previous.Count)
            {
                // Need to allocate a new array element.
                _previous.Add(Current);
            }
            else
            {
                Debug.Assert(_index < _previous.Count);

                // Use a previously allocated slot.
                _previous[_index] = Current;
            }

            Current.Reset();
            _index++;
        }

        public void Push(JsonClassInfo nextClassInfo, object nextValue)
        {
            Push();
            Current.JsonClassInfo = nextClassInfo;
            Current.CurrentValue = nextValue;

            ClassType classType = nextClassInfo.ClassType;

            if (classType == ClassType.Enumerable || nextClassInfo.ClassType == ClassType.Dictionary)
            {
                Current.PopStackOnEndCollection = true;
                Current.JsonPropertyInfo = Current.JsonClassInfo.PolicyProperty;
            }
            else
            {
                Debug.Assert(nextClassInfo.ClassType == ClassType.Object || nextClassInfo.ClassType == ClassType.Unknown);
                Current.PopStackOnEndObject = true;
            }
        }

        public void Pop(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            Debug.Assert(_index > 0);

            options.PopReference(ref this, false);

            Current = _previous[--_index];
        }

        // Return a property path as a simple JSONPath using dot-notation when possible. When special characters are present, bracket-notation is used:
        // $.x.y.z
        // $['PropertyName.With.Special.Chars']
        public string PropertyPath()
        {
            StringBuilder sb = new StringBuilder("$");

            for (int i = 0; i < _index; i++)
            {
                AppendStackFrame(sb, _previous[i]);
            }

            AppendStackFrame(sb, Current);
            return sb.ToString();
        }

        private void AppendStackFrame(StringBuilder sb, in WriteStackFrame frame)
        {
            // Append the property name.
            string propertyName = frame.JsonPropertyInfo?.PropertyInfo?.Name;
            AppendPropertyName(sb, propertyName);
        }

        private void AppendPropertyName(StringBuilder sb, string propertyName)
        {
            if (propertyName != null)
            {
                if (propertyName.IndexOfAny(ReadStack.SpecialCharacters) != -1)
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

        public bool AddStackReference(object value)
        {
            if (_referenceStack == null)
            {
                _referenceStack = new HashSet<object>(ReferenceEqualsEqualityComparer<object>.Comparer);
            }

            return _referenceStack.Add(value);
        }

        public void PopStackReference(object value) => _referenceStack?.Remove(value);

        // true if reference already existed; otherwise, false;
        public bool GetPreservedReference(object value, out string id)
        {
            if (_referenceResolver == null)
            {
                _referenceResolver = new DefaultReferenceResolver();
            }

            bool isReference = _referenceResolver.IsReferenced(value);
            id = _referenceResolver.GetReference(value);

            return isReference;
        }
    }
}
