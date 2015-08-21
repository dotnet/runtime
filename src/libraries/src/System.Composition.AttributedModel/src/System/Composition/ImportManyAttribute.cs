// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace System.Composition
{
    /// <summary>
    ///     Specifies that a property, field, or parameter imports a particular set of exports.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter,
                    AllowMultiple = false, Inherited = false)]
    public class ImportManyAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportManyAttribute"/> class, importing the 
        ///     set of exports without a contract name.
        /// </summary>
        public ImportManyAttribute()
            : this((string)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportManyAttribute"/> class, importing the
        ///     set of exports with the specified contract name.
        /// </summary>
        /// <param name="contractName">
        ///      A <see cref="String"/> containing the contract name of the exports to import, or 
        ///      <see langword="null"/>.
        /// </param>
        public ImportManyAttribute(string contractName)
        {
            ContractName = contractName;
        }

        /// <summary>
        ///     Gets the contract name of the exports to import.
        /// </summary>
        /// <value>
        ///      A <see cref="String"/> containing the contract name of the exports to import. The 
        ///      default value is null.
        /// </value>
        public string ContractName { get; private set; }
    }
}
