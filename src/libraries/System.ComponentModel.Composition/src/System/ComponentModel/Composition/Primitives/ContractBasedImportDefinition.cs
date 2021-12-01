// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Primitives
{
    /// <summary>
    ///     Represents a contract name and metadata-based import
    ///     required by a <see cref="ComposablePart"/> object.
    /// </summary>
    public class ContractBasedImportDefinition : ImportDefinition
    {
        // Unlike contract name, both metadata and required metadata has a sensible default; set it to an empty
        // enumerable, so that derived definitions only need to override ContractName by default.
        private readonly IEnumerable<KeyValuePair<string, Type>> _requiredMetadata = Enumerable.Empty<KeyValuePair<string, Type>>();
        private Expression<Func<ExportDefinition, bool>>? _constraint;
        private readonly CreationPolicy _requiredCreationPolicy = CreationPolicy.Any;
        private readonly string? _requiredTypeIdentity;
        private bool _isRequiredMetadataValidated;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContractBasedImportDefinition"/> class.
        /// </summary>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Derived types calling this constructor can optionally override the
        ///         <see cref="ImportDefinition.ContractName"/>, <see cref="RequiredTypeIdentity"/>,
        ///         <see cref="RequiredMetadata"/>, <see cref="ImportDefinition.Cardinality"/>,
        ///         <see cref="ImportDefinition.IsPrerequisite"/>, <see cref="ImportDefinition.IsRecomposable"/>
        ///         and <see cref="RequiredCreationPolicy"/> properties.
        ///     </note>
        /// </remarks>
        protected ContractBasedImportDefinition()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContractBasedImportDefinition"/> class
        ///     with the specified contract name, required metadataq, cardinality, value indicating
        ///     if the import definition is recomposable and a value indicating if the import definition
        ///     is a prerequisite.
        /// </summary>
        /// <param name="contractName">
        ///     A <see cref="string"/> containing the contract name of the
        ///     <see cref="Export"/> required by the <see cref="ContractBasedImportDefinition"/>.
        /// </param>
        /// <param name="requiredTypeIdentity">
        ///     The type identity of the export type expected. Use <see cref="AttributedModelServices.GetTypeIdentity(Type)"/>
        ///     to generate a type identity for a given type. If no specific type is required pass <see langword="null"/>.
        /// </param>
        /// <param name="requiredMetadata">
        ///     An <see cref="IEnumerable{T}"/> of <see cref="string"/> objects containing
        ///     the metadata names of the <see cref="Export"/> required by the
        ///     <see cref="ContractBasedImportDefinition"/>; or <see langword="null"/> to
        ///     set the <see cref="RequiredMetadata"/> property to an empty <see cref="IEnumerable{T}"/>.
        /// </param>
        /// <param name="cardinality">
        ///     One of the <see cref="ImportCardinality"/> values indicating the
        ///     cardinality of the <see cref="Export"/> objects required by the
        ///     <see cref="ContractBasedImportDefinition"/>.
        /// </param>
        /// <param name="isRecomposable">
        ///     <see langword="true"/> if the <see cref="ContractBasedImportDefinition"/> can be satisfied
        ///     multiple times throughout the lifetime of a <see cref="ComposablePart"/>, otherwise,
        ///     <see langword="false"/>.
        /// </param>
        /// <param name="isPrerequisite">
        ///     <see langword="true"/> if the <see cref="ContractBasedImportDefinition"/> is required to be
        ///     satisfied before a <see cref="ComposablePart"/> can start producing exported
        ///     objects; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="requiredCreationPolicy">
        ///     A value indicating that the importer requires a specific <see cref="CreationPolicy"/> for
        ///     the exports used to satisfy this import. If no specific <see cref="CreationPolicy"/> is needed
        ///     pass the default <see cref="CreationPolicy.Any"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="contractName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contractName"/> is an empty string ("").
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="requiredMetadata"/> contains an element that is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="cardinality"/> is not one of the <see cref="ImportCardinality"/>
        ///     values.
        /// </exception>
        public ContractBasedImportDefinition(string contractName, string? requiredTypeIdentity, IEnumerable<KeyValuePair<string, Type>>? requiredMetadata,
            ImportCardinality cardinality, bool isRecomposable, bool isPrerequisite, CreationPolicy requiredCreationPolicy)
            : this(contractName, requiredTypeIdentity, requiredMetadata, cardinality, isRecomposable, isPrerequisite, requiredCreationPolicy, MetadataServices.EmptyMetadata)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContractBasedImportDefinition"/> class
        ///     with the specified contract name, required metadataq, cardinality, value indicating
        ///     if the import definition is recomposable and a value indicating if the import definition
        ///     is a prerequisite.
        /// </summary>
        /// <param name="contractName">
        ///     A <see cref="string"/> containing the contract name of the
        ///     <see cref="Export"/> required by the <see cref="ContractBasedImportDefinition"/>.
        /// </param>
        /// <param name="requiredTypeIdentity">
        ///     The type identity of the export type expected. Use <see cref="AttributedModelServices.GetTypeIdentity(Type)"/>
        ///     to generate a type identity for a given type. If no specific type is required pass <see langword="null"/>.
        /// </param>
        /// <param name="requiredMetadata">
        ///     An <see cref="IEnumerable{T}"/> of <see cref="string"/> objects containing
        ///     the metadata names of the <see cref="Export"/> required by the
        ///     <see cref="ContractBasedImportDefinition"/>; or <see langword="null"/> to
        ///     set the <see cref="RequiredMetadata"/> property to an empty <see cref="IEnumerable{T}"/>.
        /// </param>
        /// <param name="cardinality">
        ///     One of the <see cref="ImportCardinality"/> values indicating the
        ///     cardinality of the <see cref="Export"/> objects required by the
        ///     <see cref="ContractBasedImportDefinition"/>.
        /// </param>
        /// <param name="isRecomposable">
        ///     <see langword="true"/> if the <see cref="ContractBasedImportDefinition"/> can be satisfied
        ///     multiple times throughout the lifetime of a <see cref="ComposablePart"/>, otherwise,
        ///     <see langword="false"/>.
        /// </param>
        /// <param name="isPrerequisite">
        ///     <see langword="true"/> if the <see cref="ContractBasedImportDefinition"/> is required to be
        ///     satisfied before a <see cref="ComposablePart"/> can start producing exported
        ///     objects; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="requiredCreationPolicy">
        ///     A value indicating that the importer requires a specific <see cref="CreationPolicy"/> for
        ///     the exports used to satisfy this import. If no specific <see cref="CreationPolicy"/> is needed
        ///     pass the default <see cref="CreationPolicy.Any"/>.
        /// </param>
        /// <param name="metadata">The metadata associated with this import.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="contractName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="contractName"/> is an empty string ("").
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="requiredMetadata"/> contains an element that is <see langword="null"/>.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <paramref name="cardinality"/> is not one of the <see cref="ImportCardinality"/>
        ///     values.
        /// </exception>
        public ContractBasedImportDefinition(string contractName, string? requiredTypeIdentity, IEnumerable<KeyValuePair<string, Type>>? requiredMetadata,
            ImportCardinality cardinality, bool isRecomposable, bool isPrerequisite, CreationPolicy requiredCreationPolicy, IDictionary<string, object?> metadata)
            : base(contractName, cardinality, isRecomposable, isPrerequisite, metadata)
        {
            Requires.NotNullOrEmpty(contractName, nameof(contractName));

            _requiredTypeIdentity = requiredTypeIdentity;

            if (requiredMetadata != null)
            {
                _requiredMetadata = requiredMetadata;
            }

            _requiredCreationPolicy = requiredCreationPolicy;
        }

        /// <summary>
        ///     The type identity of the export type expected.
        /// </summary>
        /// <value>
        ///     A <see cref="string"/> that is generated by <see cref="AttributedModelServices.GetTypeIdentity(Type)"/>
        ///     on the type that this import expects. If the value is <see langword="null"/> then this import
        ///     doesn't expect a particular type.
        /// </value>
        public virtual string? RequiredTypeIdentity
        {
            get { return _requiredTypeIdentity; }
        }

        /// <summary>
        ///     Gets the metadata names of the export required by the import definition.
        /// </summary>
        /// <value>
        ///     An <see cref="IEnumerable{T}"/> of pairs of metadata keys and types of the <see cref="Export"/> required by the
        ///     <see cref="ContractBasedImportDefinition"/>. The default is an empty
        ///     <see cref="IEnumerable{T}"/>.
        /// </value>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overriders of this property should never return <see langword="null"/>
        ///         or return an <see cref="IEnumerable{T}"/> that contains an element that is
        ///         <see langword="null"/>. If the definition does not contain required metadata,
        ///         return an empty <see cref="IEnumerable{T}"/> instead.
        ///     </note>
        /// </remarks>
        public virtual IEnumerable<KeyValuePair<string, Type>> RequiredMetadata
        {
            get
            {
                // NOTE : unlike other arguments, we validate this one as late as possible, because its validation may lead to type loading
                ValidateRequiredMetadata();

                Debug.Assert(_requiredMetadata != null);
                return _requiredMetadata;
            }
        }

        private void ValidateRequiredMetadata()
        {
            if (!_isRequiredMetadataValidated)
            {
                foreach (KeyValuePair<string, Type> metadataItem in _requiredMetadata)
                {
                    if ((metadataItem.Key == null) || (metadataItem.Value == null))
                    {
                        throw new InvalidOperationException(
                            SR.Format(SR.Argument_NullElement, "requiredMetadata"));
                    }
                }
                _isRequiredMetadataValidated = true;
            }
        }

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
        public virtual CreationPolicy RequiredCreationPolicy
        {
            get { return _requiredCreationPolicy; }
        }

        /// <summary>
        ///     Gets an expression that defines conditions that must be matched for the import
        ///     described by the import definition to be satisfied.
        /// </summary>
        /// <returns>
        ///     A <see cref="Expression{TDelegate}"/> containing a <see cref="Func{T, TResult}"/>
        ///     that defines the conditions that must be matched for the
        ///     <see cref="ImportDefinition"/> to be satisfied by an <see cref="Export"/>.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         This property returns an expression that defines conditions based on the
        ///         <see cref="ImportDefinition.ContractName"/>, <see cref="RequiredTypeIdentity"/>,
        ///         <see cref="RequiredMetadata"/>, and <see cref="RequiredCreationPolicy"/>
        ///         properties.
        ///     </para>
        /// </remarks>
        public override Expression<Func<ExportDefinition, bool>> Constraint
        {
            get
            {
                if (_constraint == null)
                {
                    _constraint = ConstraintServices.CreateConstraint(ContractName, RequiredTypeIdentity, RequiredMetadata, RequiredCreationPolicy);
                }

                return _constraint;
            }
        }

        /// <summary>
        ///     Executes an optimized version of the contraint given by the <see cref="Constraint"/> property
        /// </summary>
        /// <param name="exportDefinition">
        ///     A definition for a <see cref="Export"/> used to determine if it satisfies the
        ///     requirements for this <see cref="ImportDefinition"/>.
        /// </param>
        /// <returns>
        ///     <see langword="True"/> if the <see cref="Export"/> satisfies the requirements for
        ///     this <see cref="ImportDefinition"/>, otherwise returns <see langword="False"/>.
        /// </returns>
        /// <remarks>
        ///     <note type="inheritinfo">
        ///         Overrides of this method can provide a more optimized execution of the
        ///         <see cref="Constraint"/> property but the result should remain consistent.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="exportDefinition"/> is <see langword="null"/>.
        /// </exception>
        public override bool IsConstraintSatisfiedBy(ExportDefinition exportDefinition)
        {
            Requires.NotNull(exportDefinition, nameof(exportDefinition));

            if (!StringComparers.ContractName.Equals(ContractName, exportDefinition.ContractName))
            {
                return false;
            }

            return MatchRequiredMetadata(exportDefinition);
        }

        private bool MatchRequiredMetadata(ExportDefinition definition)
        {
            if (!string.IsNullOrEmpty(RequiredTypeIdentity))
            {
                string? exportTypeIdentity = definition.Metadata.GetValue<string>(CompositionConstants.ExportTypeIdentityMetadataName);

                if (!StringComparers.ContractName.Equals(RequiredTypeIdentity, exportTypeIdentity))
                {
                    return false;
                }
            }

            foreach (KeyValuePair<string, Type> metadataItem in RequiredMetadata)
            {
                string metadataKey = metadataItem.Key;
                Type metadataValueType = metadataItem.Value;

                if (!definition.Metadata.TryGetValue(metadataKey, out object? metadataValue))
                {
                    return false;
                }

                if (metadataValue != null)
                {
                    // the metadata value is not null, we can rely on IsInstanceOfType to do the right thing
                    if (!metadataValueType.IsInstanceOfType(metadataValue))
                    {
                        return false;
                    }
                }
                else
                {
                    // this is an unfortunate special case - typeof(object).IsInstanceofType(null) == false
                    // basically nulls are not considered valid values for anything
                    // We want them to match anything that is a reference type
                    if (metadataValueType.IsValueType)
                    {
                        // this is a pretty expensive check, but we only invoke it when metadata values are null, which is very rare
                        return false;
                    }
                }
            }

            if (RequiredCreationPolicy == CreationPolicy.Any)
            {
                return true;
            }

            CreationPolicy exportPolicy = definition.Metadata.GetValue<CreationPolicy>(CompositionConstants.PartCreationPolicyMetadataName);
            return exportPolicy == CreationPolicy.Any ||
                   exportPolicy == RequiredCreationPolicy;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("\n\tContractName\t").Append(ContractName);
            sb.Append("\n\tRequiredTypeIdentity\t").Append(RequiredTypeIdentity);
            if (_requiredCreationPolicy != CreationPolicy.Any)
            {
                sb.Append("\n\tRequiredCreationPolicy\t").Append(RequiredCreationPolicy);
            }

            if (_requiredMetadata.Any())
            {
                sb.Append("\n\tRequiredMetadata");
                foreach (KeyValuePair<string, Type> metadataItem in _requiredMetadata)
                {
                    sb.Append("\n\t\t").Append(metadataItem.Key).Append("\t(").Append(metadataItem.Value).Append(')');
                }
            }
            return sb.ToString();
        }
    }
}
