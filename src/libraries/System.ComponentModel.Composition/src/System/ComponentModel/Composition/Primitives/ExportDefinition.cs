// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Primitives
{
    /// <summary>
    ///     Describes the contract that an <see cref="Export"/> object satisfies.
    /// </summary>
    public class ExportDefinition
    {
        // Unlike contract name, metadata has a sensible default; set it to an empty bag, 
        // so that derived definitions only need to override ContractName by default.
        private readonly IDictionary<string, object> _metadata = MetadataServices.EmptyMetadata;
        private readonly string _contractName;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportDefinition"/> class.
        /// </summary>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Derived types calling this constructor must override <see cref="ContractName"/>
        ///         and optionally, <see cref="Metadata"/>. By default, <see cref="Metadata"/>
        ///         returns an empty, read-only dictionary.
        ///     </note>
        /// </remarks>
        protected ExportDefinition()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportDefinition"/> class with 
        ///     the specified contract name and metadata.
        /// </summary>
        /// <param name="contractName">
        ///     A <see cref="String"/> containing the contract name of the 
        ///     <see cref="ExportDefinition"/>.
        /// </param>
        /// <param name="metadata">
        ///     An <see cref="IDictionary{TKey, TValue}"/> containing the metadata of the 
        ///     <see cref="ExportDefinition"/>; or <see langword="null"/> to set the 
        ///     <see cref="Metadata"/> property to an empty, read-only 
        ///     <see cref="IDictionary{TKey, TValue}"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="contractName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contractName"/> is an empty string ("").
        /// </exception>
        public ExportDefinition(string contractName, IDictionary<string, object> metadata)
        {
            Requires.NotNullOrEmpty(contractName, nameof(contractName));

            _contractName = contractName;

            if (metadata != null)
            {
                _metadata = metadata.AsReadOnly();
            }
        }

        /// <summary>
        ///     Gets the contract name of the export definition.
        /// </summary>
        /// <value>
        ///     A <see cref="String"/> containing the contract name of the 
        ///     <see cref="ExportDefinition"/>.
        /// </value>
        /// <exception cref="NotImplementedException">
        ///     The property was not overridden by a derived class.
        /// </exception>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this property should never return <see langword="null"/> 
        ///         or an empty string ("").
        ///     </note>
        /// </remarks>
        public virtual string ContractName
        {
            get 
            {
                if (_contractName != null)
                {
                    return _contractName;
                }

                throw ExceptionBuilder.CreateNotOverriddenByDerived("ContractName");
            }
        }

        /// <summary>
        ///     Gets the metadata of the export definition.
        /// </summary>
        /// <value>
        ///     An <see cref="IDictionary{TKey, TValue}"/> containing the metadata of the 
        ///     <see cref="ExportDefinition"/>. The default is an empty, read-only
        ///     <see cref="IDictionary{TKey, TValue}"/>.
        /// </value>
        /// <remarks>
        ///     <para>
        ///         <note type="inheritinfo">
        ///             Overriders of this property should return a read-only
        ///             <see cref="IDictionary{TKey, TValue}"/> object with a case-sensitive, 
        ///             non-linguistic comparer, such as <see cref="StringComparer.Ordinal"/>, 
        ///             and should never return <see langword="null"/>.
        ///             If the <see cref="ExportDefinition"/> does not contain metadata 
        ///             return an empty <see cref="IDictionary{TKey, TValue}"/> instead.
        ///         </note>
        ///     </para>
        /// </remarks>
        public virtual IDictionary<string, object> Metadata
        {
            get 
            {
                Debug.Assert(_metadata != null);

                return _metadata; 
            }
        }

        /// <summary>
        ///     Returns a string representation of the export definition.
        /// </summary>
        /// <returns>
        ///     A <see cref="String"/> containing the value of the <see cref="ContractName"/> property.
        /// </returns>
        public override string ToString()
        {
            return ContractName;
        }
    }
}
