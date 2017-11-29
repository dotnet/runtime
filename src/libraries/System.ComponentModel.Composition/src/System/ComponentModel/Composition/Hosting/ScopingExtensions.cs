// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{
    public static class ScopingExtensions
    {
        /// <summary>
        /// Determines whether the specified part exports the specified contract.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="contractName">Name of the contract.</param>
        /// <returns>
        /// 	<c>true</c> if the specified part exports the specified contract; otherwise, <c>false</c>.
        /// </returns>
        public static bool Exports(this ComposablePartDefinition part, string contractName)
        {
            Requires.NotNull(part, nameof(part));
            Requires.NotNull(contractName, nameof(contractName));

            foreach (ExportDefinition export in part.ExportDefinitions)
            {
                if (StringComparers.ContractName.Equals(contractName, export.ContractName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified part imports the specified contract.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="contractName">Name of the contract.</param>
        /// <returns>
        /// 	<c>true</c> if the specified part imports the specified contract; otherwise, <c>false</c>.
        /// </returns>
        public static bool Imports(this ComposablePartDefinition part, string contractName)
        {
            Requires.NotNull(part, nameof(part));
            Requires.NotNull(contractName, nameof(contractName));

            foreach (ImportDefinition import in part.ImportDefinitions)
            {
                if (StringComparers.ContractName.Equals(contractName, import.ContractName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified part imports the specified contract with the given cardinality.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="contractName">Name of the contract.</param>
        /// <param name="importCardinality">The import cardinality.</param>
        /// <returns>
        /// 	<c>true</c> if the specified part imports the specified contract with the given cardinality; otherwise, <c>false</c>.
        /// </returns>
        public static bool Imports(this ComposablePartDefinition part, string contractName, ImportCardinality importCardinality)
        {
            Requires.NotNull(part, nameof(part));
            Requires.NotNull(contractName, nameof(contractName));

            foreach (ImportDefinition import in part.ImportDefinitions)
            {
                if (StringComparers.ContractName.Equals(contractName, import.ContractName) && (import.Cardinality == importCardinality))
                {
                    return true;
                }
            }

            return false;
        }

/// <summary>
        /// Determines whether the part contains a metadata entry with the specified key.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        /// 	<c>true</c> if the part contains a metadata entry with the specified key; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsPartMetadataWithKey(this ComposablePartDefinition part, string key)
        {
            Requires.NotNull(part, nameof(part));
            Requires.NotNull(key, nameof(key));

            return part.Metadata.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the specified part contains a metadata entry with the specified key and value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="part">The part.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 	<c>true</c> the specified part contains a metadata entry with the specified key and value; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsPartMetadata<T>(this ComposablePartDefinition part, string key, T value)
        {
            Requires.NotNull(part, nameof(part));
            Requires.NotNull(key, nameof(key));

            object untypedValue = null;
            if (part.Metadata.TryGetValue(key, out untypedValue))
            {
                if (value == null)
                {
                    return (untypedValue == null);
                }
                else
                {
                    return value.Equals(untypedValue);
                }
            }

            return false;
        }

        /// <summary>
        /// Filters the specified catalog.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public static FilteredCatalog Filter(this ComposablePartCatalog catalog, Func<ComposablePartDefinition, bool> filter)
        {
            Requires.NotNull(catalog, nameof(catalog));
            Requires.NotNull(filter, nameof(filter));

            return new FilteredCatalog(catalog, filter);
        }
    }
}
