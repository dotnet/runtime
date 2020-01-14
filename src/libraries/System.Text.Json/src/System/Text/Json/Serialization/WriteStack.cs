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

        // The bag of preservable references. It needs to be kept in the state and never in JsonSerializerOptions.
        public DefaultReferenceResolver ReferenceResolver;

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

        public void Push(JsonClassInfo nextClassInfo, object? nextValue)
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

        public void Pop()
        {
            Debug.Assert(_index > 0);
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
            string? propertyName = frame.JsonPropertyInfo?.PropertyInfo?.Name;
            AppendPropertyName(sb, propertyName);
        }

        private void AppendPropertyName(StringBuilder sb, string? propertyName)
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

        internal MetadataPropertyName GetResolvedReferenceHandling(object value, out string? referenceId)
        {
            // Avoid emitting metadata to value types.
            Type currentType = Current.JsonPropertyInfo?.DeclaredPropertyType ?? Current.JsonClassInfo!.Type;
            if (currentType.IsValueType)
            {
                referenceId = default;
                return MetadataPropertyName.NoMetadata;
            }

            if (ReferenceAlreadyExists(value, out referenceId))
            {
                return MetadataPropertyName.Ref;
            }

            return MetadataPropertyName.Id;
        }

        private bool ReferenceAlreadyExists(object value, out string id)
        {
            id = ReferenceResolver.GetOrAddReference(value, out bool alreadyExists);
            return alreadyExists;
        }
    }
}
