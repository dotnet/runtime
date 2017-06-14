// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Composition
{
    /// <summary>
    ///     Specifies that a type, property, field, or method provides a particular export.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property,
                    AllowMultiple = true, Inherited = false)]
    public class ExportAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
        ///     type or member marked with this attribute under the default contract name.
        /// </summary>
        public ExportAttribute() : this(null, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
        ///     type or member marked with this attribute under a contract name derived from the 
        ///     specified type.
        /// </summary>
        /// <param name="contractType">
        ///     A <see cref="Type"/> of which to derive the contract name to export the type or 
        ///     member marked with this attribute, under; or <see langword="null"/> to use the 
        ///     default contract name.
        /// </param>
        public ExportAttribute(Type contractType) : this(null, contractType)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
        ///     type or member marked with this attribute under the specified contract name.
        /// </summary>
        /// <param name="contractName">
        ///      A <see cref="string"/> containing the contract name to export the type or member 
        ///      marked with this attribute, under; or <see langword="null"/> or an empty string 
        ///      ("") to use the default contract name.
        /// </param>
        public ExportAttribute(string contractName) : this(contractName, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
        ///     type or member marked with this attribute under the specified contract name.
        /// </summary>
        /// <param name="contractName">
        ///      A <see cref="string"/> containing the contract name to export the type or member 
        ///      marked with this attribute, under; or <see langword="null"/> or an empty string 
        ///      ("") to use the default contract name.
        /// </param>
        /// <param name="contractType">
        ///     A <see cref="Type"/> of which to derive the contract name to export the type or 
        ///     member marked with this attribute, under; or <see langword="null"/> to use the 
        ///     default contract name.
        /// </param>
        public ExportAttribute(string contractName, Type contractType)
        {
            ContractName = contractName;
            ContractType = contractType;
        }

        /// <summary>
        ///     Gets the contract name to export the type or member under.
        /// </summary>
        /// <value>
        ///      A <see cref="string"/> containing the contract name to export the type or member 
        ///      marked with this attribute, under. The default value is an empty string ("").
        /// </value>
        public string ContractName { get;  }

        /// <summary>
        ///     Get the contract type that is exported by the member that this attribute is attached to.
        /// </summary>
        /// <value>
        ///     A <see cref="Type"/> of the export that is be provided. The default value is
        ///     <see langword="null"/> which means that the type will be obtained by looking at the type on
        ///     the member that this export is attached to. 
        /// </value>
        public Type ContractType { get; }
    }
}
