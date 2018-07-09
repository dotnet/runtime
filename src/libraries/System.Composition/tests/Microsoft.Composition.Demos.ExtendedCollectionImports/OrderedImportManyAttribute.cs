// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.Composition.Demos.ExtendedCollectionImports
{
    /// <summary>
    /// Used in conjunction with an ImportMany import,
    /// specifies the metadata item associated with each value that will be used to
    /// order the result.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    [MetadataAttribute]
    public sealed class OrderedImportManyAttribute : ImportAttribute
    {
        private readonly string _metadataKey;

        private const string OrderByMetadataImportMetadataConstraintName = "OrderMetadataName";

        /// <summary>
        /// Construct a <see cref="OrderByMetadataAttribute"/> for the specified metadata name.
        /// </summary>
        /// <param name="orderMetadataName">The name of the metadata item to use to order the collection.</param>
        public OrderedImportManyAttribute(string orderMetadataName)
        {
            _metadataKey = orderMetadataName;
        }

        /// <summary>
        /// The name of the metadata item to use as the key of the dictionary.
        /// </summary>
        public string OrderMetadataName { get { return _metadataKey; } }
    }
}
