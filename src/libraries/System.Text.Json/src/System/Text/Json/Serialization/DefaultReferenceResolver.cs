using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed class DefaultReferenceResolver : ReferenceResolver
    {
        private uint _referenceCount;
        private BidirectionalDictionary<string, object> _preservedReferences;

        public override void AddReference(string key, object value)
        {
            if (_preservedReferences == null)
            {
                _preservedReferences = new BidirectionalDictionary<string, object>();
            }
            else if (_preservedReferences.TryGetByKey(key, out _))
            {
                throw new JsonException($"Duplicated $id \"{key}\" found while preserving reference.");
            }

            _preservedReferences.Add(key, value);
        }

        public override string GetReference(object value)
        {
            string key;

            if (_preservedReferences == null)
            {
                _preservedReferences = new BidirectionalDictionary<string, object>();
                key = (++_referenceCount).ToString();
                _preservedReferences.Add(key, value);
            }
            else if (!_preservedReferences.TryGetByValue(value, out key))
            {
                key = (++_referenceCount).ToString();
                _preservedReferences.Add(key, value);
            }

            return key;
        }

        public override bool IsReferenced(object value)
        {
            return _preservedReferences != null && _preservedReferences.TryGetByValue(value, out _);
        }

        public override object ResolveReference(string key)
        {
            object value = null;
            _preservedReferences?.TryGetByKey(key, out value);

            if (value == null)
            {
                throw new JsonException("Reference not found.");
            }

            return value;
        }
    }
}
