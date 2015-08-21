// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Composition.Hosting.Core;
using System.Composition.Hosting.Util;
using System.Composition.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Composition.Hosting.Properties;

namespace System.Composition.Hosting.Providers.ImportMany
{
    internal class ImportManyExportDescriptorProvider : ExportDescriptorProvider
    {
        private static readonly MethodInfo s_getImportManyDefinitionMethod = typeof(ImportManyExportDescriptorProvider).GetTypeInfo().GetDeclaredMethod("GetImportManyDescriptor");
        private static readonly Type[] s_supportedContractTypes = new[] { typeof(IList<>), typeof(ICollection<>), typeof(IEnumerable<>) };

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor definitionAccessor)
        {
            if (!(contract.ContractType.IsArray ||
                  contract.ContractType.IsConstructedGenericType && s_supportedContractTypes.Contains(contract.ContractType.GetGenericTypeDefinition())))
                return NoExportDescriptors;

            bool isImportMany;
            CompositionContract unwrapped;
            if (!contract.TryUnwrapMetadataConstraint(Constants.ImportManyImportMetadataConstraintName, out isImportMany, out unwrapped))
                return NoExportDescriptors;

            var elementType = contract.ContractType.IsArray ?
                contract.ContractType.GetElementType() :
                contract.ContractType.GenericTypeArguments[0];

            var elementContract = unwrapped.ChangeType(elementType);

            var gimd = s_getImportManyDefinitionMethod.MakeGenericMethod(elementType);
            var gimdm = gimd.CreateStaticDelegate<Func<CompositionContract, CompositionContract, DependencyAccessor, object>>();
            return new[] { (ExportDescriptorPromise)gimdm(contract, elementContract, definitionAccessor) };
        }

        private static ExportDescriptorPromise GetImportManyDescriptor<TElement>(CompositionContract importManyContract, CompositionContract elementContract, DependencyAccessor definitionAccessor)
        {
            return new ExportDescriptorPromise(
                importManyContract,
                typeof(TElement[]).Name,
                false,
                () => definitionAccessor.ResolveDependencies("item", elementContract, true),
                d =>
                {
                    var dependentDescriptors = d
                        .Select(el => el.Target.GetDescriptor())
                        .ToArray();

                    return ExportDescriptor.Create((c, o) => dependentDescriptors.Select(e => (TElement)e.Activator(c, o)).ToArray(), NoMetadata);
                });
        }
    }
}
