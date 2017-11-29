// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     Specifies that a property, field, or parameter imports a particular set of exports.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
                    AllowMultiple = false, Inherited = false)]
    public class ImportManyAttribute : Attribute, IAttributedImport
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportManyAttribute"/> class, importing the 
        ///     set of exports with the default contract name.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on the element\item type of 
        ///         theproperty, field, or parameter type that this is marked with this attribute.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        public ImportManyAttribute()
            : this((string)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportManyAttribute"/> class, importing the
        ///     set of exports with the contract name derived from the specified type.
        /// </summary>
        /// <param name="contractType">
        ///     A <see cref="Type"/> of which to derive the contract name of the exports to import, or 
        ///     <see langword="null"/> to use the default contract name.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on 
        ///         <paramref name="contractType"/>.
        ///     </para>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on the property, field, 
        ///         or parameter type that is marked with this attribute.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        public ImportManyAttribute(Type contractType)
            : this((string)null, contractType)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportManyAttribute"/> class, importing the
        ///     set of exports with the specified contract name.
        /// </summary>
        /// <param name="contractName">
        ///      A <see cref="String"/> containing the contract name of the exports to import, or 
        ///      <see langword="null"/> or an empty string ("") to use the default contract name.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on the property, field, 
        ///         or parameter type that is marked with this attribute.
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        public ImportManyAttribute(string contractName)
            : this(contractName, (Type)null)
        {
        }

        public ImportManyAttribute(string contractName, Type contractType)
        {
            ContractName = contractName;
            ContractType = contractType;
        }

        /// <summary>
        ///     Gets the contract name of the exports to import.
        /// </summary>
        /// <value>
        ///      A <see cref="String"/> containing the contract name of the exports to import. The 
        ///      default value is an empty string ("").
        /// </value>
        public string ContractName { get; private set; }

        /// <summary>
        ///     Get the contract type of the export to import.
        /// </summary>
        /// <value>
        ///     A <see cref="Type"/> of the export that this import is expecting. The default value is
        ///     <see langword="null"/> which means that the type will be obtained by looking at the type on
        ///     the member that this import is attached to. If the type is <see cref="object"/> then the
        ///     importer is delaring they can accept any exported type.
        /// </value>
        public Type ContractType { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the property or field will be recomposed
        ///     when exports that provide the same contract that this import expects, have changed
        ///     in the container. 
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if the property or field allows for recomposition when exports
        ///     that provide the same <see cref="ContractName"/> are added or removed from the 
        ///     <see cref="CompositionContainer"/>; otherwise, <see langword="false"/>. 
        ///     The default value is <see langword="false"/>.
        /// </value>
        public bool AllowRecomposition { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating that the importer requires a specific 
        ///     <see cref="CreationPolicy"/> for the exports used to satisfy this import. T
        /// </summary>
        /// <value>
        ///     <see cref="CreationPolicy.Any"/> - default value, used if the importer doesn't 
        ///         require a specific <see cref="CreationPolicy"/>.
        /// 
        ///     <see cref="CreationPolicy.Shared"/> - Requires that all exports used should be shared
        ///         by everyone in the container.
        /// 
        ///     <see cref="CreationPolicy.NonShared"/> - Requires that all exports used should be 
        ///         non-shared in a container and thus everyone gets their own instance.
        /// </value>
        public CreationPolicy RequiredCreationPolicy { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating that the importer indicating that the composition engine 
        ///     either should satisfy exports from the local or no local scope.
        /// </summary>
        /// <value>
        ///     <see cref="ImportSource.Any"/> - indicates that importer does not
        ///         require a specific satisfaction scope"/>.
        /// 
        ///     <see cref="ImportSource.Local"/> - indicates the importer requires satisfaction to be
        ///         from the current container.
        /// 
        ///     <see cref="ImportSource.NonLocal"/> - indicates the importer requires satisfaction to be
        ///         from one of the ancestor containers.
        /// </value>
        public ImportSource Source { get; set; }

        ImportCardinality IAttributedImport.Cardinality
        {
            get { return ImportCardinality.ZeroOrMore; }
        }
    }
}
