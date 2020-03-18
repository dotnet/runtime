// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// The default ReferenceResolver implementation to handle duplicate object references.
    /// </summary>
    /// <remarks>
    /// It is currently a struct to save one unnecessary allcation while (de)serializing.
    /// If we choose to expose the ReferenceResolver in a future, we may need to create an abstract class/interface and change this type to become a class that inherits from that abstract class/interface.
    /// </remarks>
    internal struct DefaultReferenceResolver
    {
        private uint _referenceCount;
        private readonly Dictionary<string, object>? _referenceIdToObjectMap;
        private readonly Dictionary<object, string>? _objectToReferenceIdMap;

        public DefaultReferenceResolver(bool writing)
        {
            _referenceCount = default;

            if (writing)
            {
                // Comparer used here to always do a Reference Equality comparison on serialization which is where we use the objects as the TKey in our dictionary.
                _objectToReferenceIdMap = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
                _referenceIdToObjectMap = null;
            }
            else
            {
                _referenceIdToObjectMap = new Dictionary<string, object>();
                _objectToReferenceIdMap = null;
            }
        }

        /// <summary>
        /// Adds an entry to the bag of references using the specified id and value.
        /// This method gets called when an $id metadata property from a JSON object is read.
        /// </summary>
        /// <param name="referenceId">The identifier of the respective JSON object or array.</param>
        /// <param name="value">The value of the respective CLR reference type object that results from parsing the JSON object.</param>
        /// <returns>True if the value was successfully added, false otherwise.</returns>
        public bool AddReferenceOnDeserialize(string referenceId, object value)
        {
            return JsonHelpers.TryAdd(_referenceIdToObjectMap!, referenceId, value);
        }

        /// <summary>
        /// Gets the reference id of the specified value if exists; otherwise a new id is assigned.
        /// This method gets called before a CLR object is written so we can decide whether to write $id and the rest of its properties or $ref and step into the next object.
        /// The first $id value will be 1.
        /// </summary>
        /// <param name="value">The value of the CLR reference type object to get or add an id for.</param>
        /// <param name="referenceId">The id realated to the object.</param>
        /// <returns></returns>
        public bool TryGetOrAddReferenceOnSerialize(object value, out string referenceId)
        {
            bool result = _objectToReferenceIdMap!.TryGetValue(value, out referenceId!);
            if (!result)
            {
                _referenceCount++;
                referenceId = _referenceCount.ToString();
                _objectToReferenceIdMap.Add(value, referenceId);
            }
            return result;
        }

        /// <summary>
        /// Resolves the CLR reference type object related to the specified reference id.
        /// This method gets called when $ref metadata property is read.
        /// </summary>
        /// <param name="referenceId">The id related to the returned object.</param>
        /// <returns></returns>
        public object ResolveReferenceOnDeserialize(string referenceId)
        {
            if (!_referenceIdToObjectMap!.TryGetValue(referenceId, out object? value))
            {
                ThrowHelper.ThrowJsonException_MetadataReferenceNotFound(referenceId);
            }

            return value;
        }
    }
}
