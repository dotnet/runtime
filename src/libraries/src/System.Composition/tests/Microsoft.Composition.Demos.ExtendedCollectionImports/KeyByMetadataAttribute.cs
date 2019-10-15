// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.Composition.Demos.ExtendedCollectionImports
{
    /// <summary>
    /// Used in conjunction with an import of type <see cref="IDictionary{TKey,TValue}"/>,
    /// specifies the metadata item associated with each value that will be used as the
    /// key within the dictionary.
    /// </summary>
    /// <example>
    /// The exporters provide a metadata item, here "HandledState":
    /// <code>
    /// [Export(typeof(ModemStateImplementation)),
    ///  ExportMetadata("HandledState", ModemState.On)]
    /// public class OnState : ModemStateImplementation
    /// {
    /// </code>
    /// The importer requests that its imports are keyed according to the same metadata item.
    /// <code>
    /// [ImportingConstructor]
    /// public Modem(
    ///     [KeyByMetadata("HandledState")]
    ///     IDictionary&lt;ModemState, Lazy&lt;ModemStateImplementation&gt;&gt; states)
    /// {
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    [MetadataAttribute]
    public sealed class KeyByMetadataAttribute : Attribute
    {
        private readonly string _metadataKey;

        private const string KeyByMetadataImportMetadataConstraintName = "KeyMetadataName";

        /// <summary>
        /// Construct a <see cref="KeyByMetadataAttribute"/> for the specified metadata name.
        /// </summary>
        /// <param name="keyMetadataName">The name of the metadata item to use as the key of the dictionary.</param>
        public KeyByMetadataAttribute(string keyMetadataName)
        {
            _metadataKey = keyMetadataName;
        }

        /// <summary>
        /// The name of the metadata item to use as the key of the dictionary.
        /// </summary>
        public string KeyMetadataName { get { return _metadataKey; } }
    }
}
