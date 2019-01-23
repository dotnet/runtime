// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    public partial struct JsonElement
    {
        public struct ObjectEnumerator : IEnumerable<JsonProperty>, IEnumerator<JsonProperty>
        {
            private readonly JsonElement _target;
            private int _curIdx;
            private readonly int _endIdx;

            internal ObjectEnumerator(JsonElement target)
            {
                Debug.Assert(target.TokenType == JsonTokenType.StartObject);

                _target = target;
                _curIdx = -1;
                _endIdx = _target._parent.GetEndIndex(_target._idx, includeEndElement: false);
            }

            public JsonProperty Current =>
                _curIdx < 0 ?
                    default :
                    new JsonProperty(new JsonElement(_target._parent, _curIdx));

            public ObjectEnumerator GetEnumerator()
            {
                ObjectEnumerator ator = this;
                ator._curIdx = -1;
                return ator;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator<JsonProperty> IEnumerable<JsonProperty>.GetEnumerator() => GetEnumerator();

            public void Dispose()
            {
                _curIdx = _endIdx;
            }

            public void Reset()
            {
                _curIdx = -1;
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (_curIdx >= _endIdx)
                {
                    return false;
                }

                if (_curIdx < 0)
                {
                    _curIdx = _target._idx + JsonDocument.DbRow.Size;
                }
                else
                {
                    _curIdx = _target._parent.GetEndIndex(_curIdx, includeEndElement: true);
                }

                // _curIdx is now pointing at a property name, move one more to get the value
                _curIdx += JsonDocument.DbRow.Size;

                return _curIdx < _endIdx;
            }
        }
    }
}
