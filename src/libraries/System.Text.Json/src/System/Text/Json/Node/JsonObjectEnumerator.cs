using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    /// <summary>
    ///   Supports an iteration over a JSON object.
    /// </summary>
    public struct JsonObjectEnumerator : IEnumerator<KeyValuePair<string, JsonNode>>
    {
        private JsonObjectProperty _first;
        private JsonObjectProperty _current;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonObjectEnumerator"/> class supporting an interation over provided JSON object.
        /// </summary>
        /// <param name="jsonObject">JSON object to iterate over.</param>
        public JsonObjectEnumerator(JsonObject jsonObject)
        {
            _first = jsonObject._first;
            _current = null;
        }

        /// <summary>
        ///   Gets the property in the JSON object at the current position of the enumerator.
        /// </summary>
        public KeyValuePair<string, JsonNode> Current
        {
            get
            {
                if (_current == null)
                {
                    return default;
                }

                return new KeyValuePair<string, JsonNode>(_current.Name, _current.Value);
            }
        }

        /// <summary>
        ///    Gets the property in the JSON object at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        ///   Releases all resources used by the <see cref="JsonObjectEnumerator"/>.
        /// </summary>
        public void Dispose() => _current = null;

        /// <summary>
        ///   Advances the enumerator to the next property of the JSON object.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (_first == null)
            {
                return false;
            }

            if (_current == null)
            {
                _current = _first;
                return true;
            }

            if (_current.Next != null)
            {
                _current = _current.Next;
                return true;
            }

            return false;
        }

        /// <summary>
        ///   Sets the enumerator to its initial position, which is before the first element in the JSON object.
        /// </summary>
        void IEnumerator.Reset() => _current = null;
    }
}
