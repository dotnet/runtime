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
        /// <summary>
        ///   An enumerable and enumerator for the properties of a JSON object.
        /// </summary>
        [DebuggerDisplay("{Current,nq}")]
        public struct ObjectEnumerator : IEnumerable<JsonProperty>, IEnumerator<JsonProperty>
        {
            private readonly JsonElement _target;
            private int _curIdx;
            private readonly int _endIdxOrVersion;
            private JsonObjectProperty _current;

            internal ObjectEnumerator(JsonElement target)
            {
                _target = target;
                _curIdx = -1;
                _current = null;

                if (target._parent is JsonDocument document)
                {
                    Debug.Assert(target.TokenType == JsonTokenType.StartObject);
                    _endIdxOrVersion = document.GetEndIndex(_target._idx, includeEndElement: false);
                }
                else
                {
                    var jsonObject = (JsonObject)target._parent;
                   _endIdxOrVersion = jsonObject._version;
                }
            }

            /// <inheritdoc />
            public JsonProperty Current
            {
                get
                {
                    if (_target._parent is JsonNode)
                    {
                        if (_current == null)
                        {
                            return default;
                        }

                        return new JsonProperty(_current.Value.AsJsonElement(), _current.Name);
                    }

                    if (_curIdx < 0)
                    {
                        return default;
                    }

                    var document = (JsonDocument)_target._parent;
                    return new JsonProperty(new JsonElement(document, _curIdx));
                }
            }

            /// <summary>
            ///   Returns an enumerator that iterates the properties of an object.
            /// </summary>
            /// <returns>
            ///   An <see cref="ObjectEnumerator"/> value that can be used to iterate
            ///   through the object.
            /// </returns>
            /// <remarks>
            ///   The enumerator will enumerate the properties in the order they are
            ///   declared, and when an object has multiple definitions of a single
            ///   property they will all individually be returned (each in the order
            ///   they appear in the content).
            /// </remarks>
            public ObjectEnumerator GetEnumerator()
            {
                ObjectEnumerator ator = this;
                ator._curIdx = -1;
                return ator;
            }

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <inheritdoc />
            IEnumerator<JsonProperty> IEnumerable<JsonProperty>.GetEnumerator() => GetEnumerator();

            /// <inheritdoc />
            public void Dispose()
            {
                _curIdx = _endIdxOrVersion;
                _current = null;
            }

            /// <inheritdoc />
            public void Reset()
            {
                _curIdx = -1;
                _current = null;
            }

            /// <inheritdoc />
            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public bool MoveNext()
            {
                if (_target._parent is JsonObject jsonObject)
                {
                    if (jsonObject._version != _endIdxOrVersion)
                    {
                        throw new InvalidOperationException(SR.ArrayModifiedDuringIteration);
                    }

                    if (_current == null)
                    {
                        _current = jsonObject._first;
                        return true;
                    }

                    if (_current.Next != null)
                    {
                        _current = _current.Next;
                        return true;
                    }

                    return false;
                }

                if (_curIdx >= _endIdxOrVersion)
                {
                    return false;
                }

                if (_curIdx < 0)
                {
                    _curIdx = _target._idx + JsonDocument.DbRow.Size;
                }
                else
                {
                    var document = (JsonDocument)_target._parent;
                    _curIdx = document.GetEndIndex(_curIdx, includeEndElement: true);
                }

                // _curIdx is now pointing at a property name, move one more to get the value
                _curIdx += JsonDocument.DbRow.Size;

                return _curIdx < _endIdxOrVersion;
            }
        }
    }
}
