// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.AttributedModel;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition
{
    public static class AttributedModelServices
    {
        [SuppressMessage("Microsoft.Design", "CA1004")]
        public static TMetadataView GetMetadataView<TMetadataView>(IDictionary<string, object> metadata)
        {
            Requires.NotNull(metadata, nameof(metadata));
            Contract.Ensures(Contract.Result<TMetadataView>() != null);

            return MetadataViewProvider.GetMetadataView<TMetadataView>(metadata);
        }

        public static ComposablePart CreatePart(object attributedPart)
        {
            Requires.NotNull(attributedPart, nameof(attributedPart));
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            return AttributedModelDiscovery.CreatePart(attributedPart);
        }

        public static ComposablePart CreatePart(object attributedPart, ReflectionContext reflectionContext)
        {
            Requires.NotNull(attributedPart, "attributedPart");
            Requires.NotNull(reflectionContext, "reflectionContext");
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            return AttributedModelDiscovery.CreatePart(attributedPart, reflectionContext);
        }

        public static ComposablePart CreatePart(ComposablePartDefinition partDefinition, object attributedPart)
        {
            Requires.NotNull(partDefinition, nameof(partDefinition));
            Requires.NotNull(attributedPart, nameof(attributedPart));
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            var reflectionComposablePartDefinition = partDefinition as ReflectionComposablePartDefinition;
            if(reflectionComposablePartDefinition == null)
            {
                throw ExceptionBuilder.CreateReflectionModelInvalidPartDefinition("partDefinition", partDefinition.GetType());
            }

            return AttributedModelDiscovery.CreatePart(reflectionComposablePartDefinition, attributedPart);
        }

        public static ComposablePartDefinition CreatePartDefinition(Type type, ICompositionElement origin)
        {
            Requires.NotNull(type, nameof(type));
            Contract.Ensures(Contract.Result<ComposablePartDefinition>() != null);

            return AttributedModelServices.CreatePartDefinition(type, origin, false);
        }

        public static ComposablePartDefinition CreatePartDefinition(Type type, ICompositionElement origin, bool ensureIsDiscoverable)
        {
            Requires.NotNull(type, nameof(type));
            if (ensureIsDiscoverable)
            {
                return AttributedModelDiscovery.CreatePartDefinitionIfDiscoverable(type, origin);
            }
            else
            {
                return AttributedModelDiscovery.CreatePartDefinition(type, null, false, origin);
            }
        }

        public static string GetTypeIdentity(Type type)
        {
            Requires.NotNull(type, nameof(type));
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            return ContractNameServices.GetTypeIdentity(type);
        }

        public static string GetTypeIdentity(MethodInfo method)
        {
            Requires.NotNull(method, nameof(method));
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            return ContractNameServices.GetTypeIdentityFromMethod(method);
        }

        public static string GetContractName(Type type)
        {
            Requires.NotNull(type, nameof(type));
            Contract.Ensures(!string.IsNullOrEmpty(Contract.Result<string>()));

            return AttributedModelServices.GetTypeIdentity(type);
        }

        public static ComposablePart AddExportedValue<T>(this CompositionBatch batch, T exportedValue)
        {
            Requires.NotNull(batch, nameof(batch));
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            string contractName = AttributedModelServices.GetContractName(typeof(T));

            return batch.AddExportedValue<T>(contractName, exportedValue);
        }

        public static void ComposeExportedValue<T>(this CompositionContainer container, T exportedValue)
        {
            Requires.NotNull(container, nameof(container));

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue<T>(exportedValue);
            container.Compose(batch);
        }

        public static ComposablePart AddExportedValue<T>(this CompositionBatch batch, string contractName, T exportedValue)
        {
            Requires.NotNull(batch, nameof(batch));
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            string typeIdentity = AttributedModelServices.GetTypeIdentity(typeof(T));

            IDictionary<string, object> metadata = new Dictionary<string, object>();
            metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, typeIdentity);

            return batch.AddExport(new Export(contractName, metadata, () => exportedValue));
        }

        public static void ComposeExportedValue<T>(this CompositionContainer container, string contractName, T exportedValue)
        {
            Requires.NotNull(container, nameof(container));

            CompositionBatch batch = new CompositionBatch();
            batch.AddExportedValue<T>(contractName, exportedValue);
            container.Compose(batch);
        }

        public static ComposablePart AddPart(this CompositionBatch batch, object attributedPart)
        {
            Requires.NotNull(batch, nameof(batch));
            Requires.NotNull(attributedPart, nameof(attributedPart));
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            ComposablePart part = AttributedModelServices.CreatePart(attributedPart);

            batch.AddPart(part);

            return part;
        }

        public static void ComposeParts(this CompositionContainer container, params object[] attributedParts)
        {
            Requires.NotNull(container, nameof(container));
            Requires.NotNullOrNullElements(attributedParts, "attributedParts");

            CompositionBatch batch = new CompositionBatch(
                attributedParts.Select(attributedPart => AttributedModelServices.CreatePart(attributedPart)).ToArray(),
                Enumerable.Empty<ComposablePart>());

            container.Compose(batch);
        }

        /// <summary>
        ///     Satisfies the imports of the specified attributed object exactly once and they will not
        ///     ever be recomposed.
        /// </summary>
        /// <param name="part">
        ///     The attributed object to set the imports.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="compositionService"/> or <paramref name="attributedPart"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will
        ///     contain a collection of errors that occurred.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="ICompositionService"/> has been disposed of.
        /// </exception>
        public static ComposablePart SatisfyImportsOnce(this ICompositionService compositionService, object attributedPart)
        {
            Requires.NotNull(compositionService, nameof(compositionService));
            Requires.NotNull(attributedPart, nameof(attributedPart));
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            ComposablePart part = AttributedModelServices.CreatePart(attributedPart);
            compositionService.SatisfyImportsOnce(part);

            return part;
        }
        
        /// <summary>
        ///     Satisfies the imports of the specified attributed object exactly once and they will not
        ///     ever be recomposed.
        /// </summary>
        /// <param name="part">
        ///     The attributed object to set the imports.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="compositionService"/> or <paramref name="attributedPart"/>  or <paramref name="reflectionContext"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="CompositionException">
        ///     An error occurred during composition. <see cref="CompositionException.Errors"/> will
        ///     contain a collection of errors that occurred.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///     The <see cref="ICompositionService"/> has been disposed of.
        /// </exception>
        public static ComposablePart SatisfyImportsOnce(this ICompositionService compositionService, object attributedPart, ReflectionContext reflectionContext)
        {
            Requires.NotNull(compositionService, "compositionService");
            Requires.NotNull(attributedPart, "attributedPart");
            Requires.NotNull(reflectionContext, "reflectionContext");
            Contract.Ensures(Contract.Result<ComposablePart>() != null);

            ComposablePart part = AttributedModelServices.CreatePart(attributedPart, reflectionContext);
            compositionService.SatisfyImportsOnce(part);

            return part;
        }

        /// <summary>
        /// Determines whether the specified part exports the specified contract.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <returns>
        /// 	<c>true</c> if the specified part exports the specified contract; otherwise, <c>false</c>.
        /// </returns>
        public static bool Exports(this ComposablePartDefinition part, Type contractType)
        {
            Requires.NotNull(part, nameof(part));
            Requires.NotNull(contractType, nameof(contractType));

            return part.Exports(AttributedModelServices.GetContractName(contractType));
        }

        /// <summary>
        /// Determines whether the specified part exports the specified contract.
        /// </summary>
        /// <typeparam name="T">Type of the contract.</typeparam>
        /// <param name="part">The part.</param>
        /// <returns>
        /// 	<c>true</c> if the specified part exports the specified contract; otherwise, <c>false</c>.
        /// </returns>
        public static bool Exports<T>(this ComposablePartDefinition part)
        {
            Requires.NotNull(part, nameof(part));

            return part.Exports(typeof(T));
        }

        /// <summary>
        /// Determines whether the specified part imports the specified contract.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <returns>
        /// 	<c>true</c> if the specified part imports the specified contract; otherwise, <c>false</c>.
        /// </returns>
        public static bool Imports(this ComposablePartDefinition part, Type contractType)
        {
            Requires.NotNull(part, nameof(part));
            Requires.NotNull(contractType, nameof(contractType));

            return part.Imports(AttributedModelServices.GetContractName(contractType));
        }

        /// <summary>
        /// Determines whether the specified part imports the specified contract.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <typeparam name="T">Type of the contract.</typeparam>
        /// <returns>
        /// 	<c>true</c> if the specified part imports the specified contract; otherwise, <c>false</c>.
        /// </returns>
        public static bool Imports<T>(this ComposablePartDefinition part)
        {
            Requires.NotNull(part, nameof(part));

            return part.Imports(typeof(T));
        }

        /// <summary>
        /// Determines whether the specified part imports the specified contract with the given cardinality.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="contractType">Type of the contract.</param>
        /// <param name="importCardinality">The import cardinality.</param>
        /// <returns>
        /// 	<c>true</c> if the specified part imports the specified contract with the given cardinality; otherwise, <c>false</c>.
        /// </returns>
        public static bool Imports(this ComposablePartDefinition part, Type contractType, ImportCardinality importCardinality)
        {
            Requires.NotNull(part, nameof(part));
            Requires.NotNull(contractType, nameof(contractType));

            return part.Imports(AttributedModelServices.GetContractName(contractType), importCardinality);
        }

        /// <summary>
        /// Determines whether the specified part imports the specified contract with the given cardinality.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <typeparam name="T">Type of the contract.</typeparam>
        /// <param name="importCardinality">The import cardinality.</param>
        /// <returns>
        /// 	<c>true</c> if the specified part imports the specified contract with the given cardinality; otherwise, <c>false</c>.
        /// </returns>
        public static bool Imports<T>(this ComposablePartDefinition part, ImportCardinality importCardinality)
        {
            Requires.NotNull(part, nameof(part));

            return part.Imports(typeof(T), importCardinality);
        }
    }
}
