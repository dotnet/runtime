// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    [DebuggerDisplay("Current: ClassType.{Current.JsonClassInfo.ClassType}, {Current.JsonClassInfo.Type.Name}")]
    internal struct ReadStack
    {
        private static readonly char[] SpecialCharacters = { '.', ' ', '\'', '/', '"', '[', ']', '(', ')', '\t', '\n', '\r', '\f', '\b', '\\', '\u0085', '\u2028', '\u2029' };

        // A field is used instead of a property to avoid value semantics.
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

        // Return a JSONPath using simple dot-notation when possible. When special characters are present, bracket-notation is used:
        // $.x.y[0].z
        // $['PropertyName.With.Special.Chars']
        public string JsonPath
        {
            get
            {
                StringBuilder sb = new StringBuilder("$");

                for (int i = 0; i < _index; i++)
                {
                    AppendStackFrame(sb, _previous[i]);
                }

                AppendStackFrame(sb, Current);
                return sb.ToString();
            }
        }

        private void AppendStackFrame(StringBuilder sb, in ReadStackFrame frame)
        {
            // Append the property name.
            string propertyName = GetPropertyName(frame);
            AppendPropertyName(sb, propertyName);

            if (frame.JsonClassInfo != null)
            {
                if (frame.IsProcessingDictionary)
                {
                    // For dictionaries add the key.
                    AppendPropertyName(sb, frame.KeyName);
                }
                else if (frame.IsProcessingEnumerable)
                {
                    // For enumerables add the index.
                    IList list = frame.TempEnumerableValues;
                    if (list == null && frame.ReturnValue != null)
                    {
                        list = (IList)frame.JsonPropertyInfo?.GetValueAsObject(frame.ReturnValue);
                    }

                    if (list != null)
                    {
                        sb.Append(@"[");
                        sb.Append(list.Count);
                        sb.Append(@"]");
                    }
                }
            }
        }

        private void AppendPropertyName(StringBuilder sb, string propertyName)
        {
            if (propertyName != null)
            {
                JsonEncodedText encodedPropertyName = JsonEncodedText.Encode(propertyName);

                if (propertyName.IndexOfAny(SpecialCharacters) != -1)
                {
                    sb.Append(@"['");
                    sb.Append(encodedPropertyName);
                    sb.Append(@"']");
                }
                else
                {
                    sb.Append('.');
                    sb.Append(encodedPropertyName);
                }
            }
        }

        private string GetPropertyName(in ReadStackFrame frame)
        {
            // Attempt to get the JSON property name from the frame.
            byte[] utf8PropertyName = frame.JsonPropertyName;
            if (utf8PropertyName == null)
            {
                // Attempt to get the JSON property name from the JsonPropertyInfo.
                utf8PropertyName = frame.JsonPropertyInfo?.JsonPropertyName;
            }

            string propertyName;
            if (utf8PropertyName != null)
            {
                // Attempt to get the JSON property name from the dictionary key.
                propertyName = JsonHelpers.Utf8GetString(utf8PropertyName);
            }
            else
            {
                propertyName = null;
            }

            return propertyName;
        }
    }
}
