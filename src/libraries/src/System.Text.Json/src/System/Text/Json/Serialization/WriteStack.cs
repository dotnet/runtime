// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    internal struct WriteStack
    {
        // Fields are used instead of properties to avoid value semantics.
        public WriteStackFrame Current;
        private List<WriteStackFrame> _previous;
        private int _index;

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
                Current.PopStackOnEnd = true;
                Current.JsonPropertyInfo = Current.JsonClassInfo.GetPolicyProperty();
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

        // Return a property path in the form of: [FullNameOfType].FirstProperty.SecondProperty.LastProperty
        public string PropertyPath
        {
            get
            {
                StringBuilder path = new StringBuilder();

                if (_previous == null || _index == 0)
                {
                    path.Append($"[{Current.JsonClassInfo.Type.FullName}]");
                }
                else
                {
                    path.Append($"[{_previous[0].JsonClassInfo.Type.FullName}]");

                    for (int i = 0; i < _index; i++)
                    {
                        path.Append(GetPropertyName(_previous[i]));
                    }
                }

                path.Append(GetPropertyName(Current));

                return path.ToString();
            }
        }

        private string GetPropertyName(in WriteStackFrame frame)
        {
            if (frame.JsonPropertyInfo != null && frame.JsonClassInfo.ClassType == ClassType.Object)
            {
                return $".{frame.JsonPropertyInfo.PropertyInfo.Name}";
            }

            return string.Empty;
        }
    }
}
