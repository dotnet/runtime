// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    internal struct ReadStack
    {
        // A fields is used instead of a property to avoid value semantics.
        public ReadStackFrame Current;

        private List<ReadStackFrame> _previous;
        public int _index;

        public void Push()
        {
            if (_previous == null)
            {
                _previous = new List<ReadStackFrame>();
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

        public void Pop()
        {
            Debug.Assert(_index > 0);
            Current = _previous[--_index];
        }

        public bool IsLastFrame => _index == 0;

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

        private string GetPropertyName(in ReadStackFrame frame)
        {
            if (frame.JsonPropertyInfo != null && frame.JsonClassInfo.ClassType == ClassType.Object)
            {
                return $".{frame.JsonPropertyInfo.PropertyInfo.Name}";
            }

            return string.Empty;
        }
    }
}
