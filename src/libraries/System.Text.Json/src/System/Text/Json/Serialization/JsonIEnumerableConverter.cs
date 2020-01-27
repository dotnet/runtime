// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for IEnumerable-based collections.
    /// </summary>
    internal abstract class JsonIEnumerableConverter<TCollection, TElement> : JsonResumableConverter<TCollection>
    {
        private Type _elementType = typeof(TElement);

        internal override bool CanHaveValuesMetadata => true;
        internal override ClassType ClassType => ClassType.Enumerable;
        internal override Type ElementType => _elementType;
    }
}
