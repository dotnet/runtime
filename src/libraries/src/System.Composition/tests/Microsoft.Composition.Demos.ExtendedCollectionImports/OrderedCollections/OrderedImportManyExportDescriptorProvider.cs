// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Composition.Hosting.Core;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Composition.Demos.ExtendedCollectionImports.Util;
using System.Composition.Hosting;

namespace Microsoft.Composition.Demos.ExtendedCollectionImports.OrderedCollections
{
    public class OrderedImportManyExportDescriptorProvider : ExportDescriptorProvider
    {
        /// <summary>
        /// Identifies the metadata used to order a "many" import.
        /// </summary>
        private const string OrderByMetadataImportMetadataConstraintName = "OrderMetadataName";

        private static readonly MethodInfo s_getImportManyDefinitionMethod = typeof(OrderedImportManyExportDescriptorProvider).GetTypeInfo().GetDeclaredMethod("GetImportManyDescriptor");
        private static readonly Type[] s_supportedContractTypes = new[] { typeof(IList<>), typeof(ICollection<>), typeof(IEnumerable<>) };

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor definitionAccessor)
        {
            if (!(contract.ContractType.IsArray ||
                  contract.ContractType.IsConstructedGenericType && s_supportedContractTypes.Contains(contract.ContractType.GetGenericTypeDefinition())))
                return NoExportDescriptors;

            string keyToOrderBy;
            CompositionContract orderUnwrappedContract;
            if (!contract.TryUnwrapMetadataConstraint(OrderByMetadataImportMetadataConstraintName, out keyToOrderBy, out orderUnwrappedContract))
                return NoExportDescriptors;

            var elementType = contract.ContractType.IsArray ?
                contract.ContractType.GetElementType() :
                contract.ContractType.GenericTypeArguments[0];

            var elementContract = orderUnwrappedContract.ChangeType(elementType);

            var gimd = s_getImportManyDefinitionMethod.MakeGenericMethod(elementType);

            return new[] { (ExportDescriptorPromise)gimd.Invoke(null, new object[] { contract, elementContract, definitionAccessor, keyToOrderBy }) };
        }

        private static ExportDescriptorPromise GetImportManyDescriptor<TElement>(CompositionContract importManyContract, CompositionContract elementContract, DependencyAccessor definitionAccessor, string keyToOrderBy)
        {
            return new ExportDescriptorPromise(
                importManyContract,
                typeof(TElement[]).Name,
                false,
                () => definitionAccessor.ResolveDependencies("item", elementContract, true),
                d =>
                {
                    var dependentDescriptors = (keyToOrderBy != null) ?
                        OrderDependentDescriptors(d, keyToOrderBy) :
                        d.Select(el => el.Target.GetDescriptor()).ToArray();

                    return ExportDescriptor.Create((c, o) => dependentDescriptors.Select(e => (TElement)e.Activator(c, o)).ToArray(), NoMetadata);
                });
        }

        private static IEnumerable<ExportDescriptor> OrderDependentDescriptors(IEnumerable<CompositionDependency> dependentDescriptors, string keyToOrderBy)
        {
            var targets = dependentDescriptors.Select(d => d.Target).ToArray();
            var missing = targets.Where(t => !t.GetDescriptor().Metadata.ContainsKey(keyToOrderBy) ||
                                             t.GetDescriptor().Metadata[keyToOrderBy] == null).ToArray();
            if (missing.Length != 0)
            {
                var origins = Formatters.ReadableQuotedList(missing.Select(m => m.Origin));
                var message = string.Format("The metadata '{0}' cannot be used for ordering because it is missing from exports on part(s) {1}.", keyToOrderBy, origins);
                throw new CompositionFailedException(message);
            }

            return targets.Select(t => t.GetDescriptor()).OrderBy(d => d.Metadata[keyToOrderBy]).ToArray();
        }
    }
}
