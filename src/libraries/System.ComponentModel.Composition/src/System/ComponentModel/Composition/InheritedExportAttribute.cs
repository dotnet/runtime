// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     Specifies that a type or interface that provides a particular export.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public class InheritedExportAttribute : ExportAttribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
        ///     type marked with this attribute under the default contract name.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on the type itself, 
        ///         that is marked with this attribute. 
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        public InheritedExportAttribute()
            : this((string)null, (Type)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
        ///     type marked with this attribute under a contract name derived from the specified type.
        /// </summary>
        /// <param name="contractType">
        ///     A <see cref="Type"/> of which to derive the contract name to export the type  
        ///     marked with this attribute, under; or <see langword="null"/> to use the 
        ///     default contract name.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         The contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on 
        ///         <paramref name="contractType"/>.
        ///     </para>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on the type of the 
        ///         itself, that is marked with this attribute. 
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        public InheritedExportAttribute(Type contractType)
            : this((string)null, contractType)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
        ///     type or member marked with this attribute under the specified contract name.
        /// </summary>
        /// <param name="contractName">
        ///      A <see cref="String"/> containing the contract name to export the type 
        ///      marked with this attribute, under; or <see langword="null"/> or an empty string 
        ///      ("") to use the default contract name.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on 
        ///         the type itself that this is marked with this attribute. 
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        public InheritedExportAttribute(string contractName)
            : this(contractName, (Type)null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExportAttribute"/> class, exporting the
        ///     type or member marked with this attribute under the specified contract name.
        /// </summary>
        /// <param name="contractName">
        ///      A <see cref="String"/> containing the contract name to export the type 
        ///      marked with this attribute, under; or <see langword="null"/> or an empty string 
        ///      ("") to use the default contract name.
        /// </param>
        /// <param name="contractType">
        ///     A <see cref="Type"/> of which to derive the contract name to export the type  
        ///     marked with this attribute, under; or <see langword="null"/> to use the 
        ///     default contract name.
        /// </param>
        /// <remarks>
        ///     <para>
        ///         The default contract name is the result of calling 
        ///         <see cref="AttributedModelServices.GetContractName(Type)"/> on 
        ///         the type itself that this is marked with this attribute. 
        ///     </para>
        ///     <para>
        ///         The contract name is compared using a case-sensitive, non-linguistic comparison 
        ///         using <see cref="StringComparer.Ordinal"/>.
        ///     </para>
        /// </remarks>
        public InheritedExportAttribute(string contractName, Type contractType)
            : base(contractName, contractType)
        {
        }
    }
}
