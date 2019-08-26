using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    /// <summary>
    ///   Supports an iteration over a JSON object.
    /// </summary>
    public struct JsonObjectEnumerator : IEnumerator<KeyValuePair<string, JsonNode>>
    {
        private Dictionary<string, JsonNode>.Enumerator _enumerator;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonObjectEnumerator"/> class supporting an interation over provided JSON object.
        /// </summary>
        /// <param name="jsonObject">JSON object to iterate over.</param>
        public JsonObjectEnumerator(JsonObject jsonObject) => _enumerator = jsonObject._dictionary.GetEnumerator();

        /// <summary>
        ///   Gets the property in the JSON object at the current position of the enumerator.
        /// </summary>
        public KeyValuePair<string, JsonNode> Current => _enumerator.Current;

        /// <summary>
        ///    Gets the property in the JSON object at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current => _enumerator.Current;

        /// <summary>
        ///   Releases all resources used by the <see cref="JsonObjectEnumerator"/>.
        /// </summary>
        public void Dispose() => _enumerator.Dispose();

        /// <summary>
        ///   Advances the enumerator to the next property of the JSON object.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext() => _enumerator.MoveNext();

        /// <summary>
        ///   Sets the enumerator to its initial position, which is before the first element in the JSON object.
        /// </summary>
        void IEnumerator.Reset() => ((IEnumerator)_enumerator).Reset();
    }
}
