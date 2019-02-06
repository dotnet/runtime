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
        public struct ArrayEnumerator : IEnumerable<JsonElement>, IEnumerator<JsonElement>
        {
            private readonly JsonElement _target;
            private int _curIdx;
            private readonly int _endIdx;

            internal ArrayEnumerator(JsonElement target)
            {
                Debug.Assert(target.TokenType == JsonTokenType.StartArray);

                _target = target;
                _curIdx = -1;
                _endIdx = _target._parent.GetEndIndex(_target._idx, includeEndElement: false);
            }

            public JsonElement Current =>
                _curIdx < 0 ? default : new JsonElement(_target._parent, _curIdx);

            public ArrayEnumerator GetEnumerator()
            {
                ArrayEnumerator ator = this;
                ator._curIdx = -1;
                return ator;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IEnumerator<JsonElement> IEnumerable<JsonElement>.GetEnumerator() => GetEnumerator();

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

                return _curIdx < _endIdx;
            }
        }
    }
}
