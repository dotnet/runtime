// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for all collections. Collections are assumed to implement <cref>System.Collections.IEnumerable</cref>.
    /// </summary>
    internal abstract class JsonCollectionConverter<TCollection, TElement> : JsonResumableConverter<TCollection>
    {
        internal sealed override ClassType ClassType => ClassType.Enumerable;
        internal override Type ElementType => typeof(TElement);
    }
}
