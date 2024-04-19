// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Composition.Hosting.Providers.Metadata;
using System.Composition.Hosting.Util;
using System.Linq;
using System.Reflection;

namespace System.Composition.Hosting.Providers.ExportFactory
{
    internal sealed class ExportFactoryWithMetadataExportDescriptorProvider : ExportDescriptorProvider
    {
        private static readonly MethodInfo s_getLazyDefinitionsMethod =
            typeof(ExportFactoryWithMetadataExportDescriptorProvider).GetTypeInfo().GetDeclaredMethod("GetExportFactoryDescriptors");

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor definitionAccessor)
        {
            if (!contract.ContractType.IsConstructedGenericType || contract.ContractType.GetGenericTypeDefinition() != typeof(ExportFactory<,>))
                return NoExportDescriptors;

            var ga = contract.ContractType.GenericTypeArguments;
            var gld = s_getLazyDefinitionsMethod.MakeGenericMethod(ga[0], ga[1]);
            var gldm = gld.CreateStaticDelegate<Func<CompositionContract, DependencyAccessor, object>>();
            return (ExportDescriptorPromise[])gldm(contract, definitionAccessor);
        }

        private static ExportDescriptorPromise[] GetExportFactoryDescriptors<TProduct, TMetadata>(CompositionContract exportFactoryContract, DependencyAccessor definitionAccessor)
        {
            var productContract = exportFactoryContract.ChangeType(typeof(TProduct));
            var boundaries = Array.Empty<string>();

            IEnumerable<string> specifiedBoundaries;
            CompositionContract unwrapped;
            if (exportFactoryContract.TryUnwrapMetadataConstraint(Constants.SharingBoundaryImportMetadataConstraintName, out specifiedBoundaries, out unwrapped))
            {
                productContract = unwrapped.ChangeType(typeof(TProduct));
                boundaries = (specifiedBoundaries ?? Array.Empty<string>()).ToArray();
            }

            var metadataProvider = MetadataViewProvider.GetMetadataViewProvider<TMetadata>();

            return definitionAccessor.ResolveDependencies("product", productContract, false)
                .Select(d => new ExportDescriptorPromise(
                    exportFactoryContract,
                    typeof(ExportFactory<TProduct, TMetadata>).Name,
                    false,
                    () => new[] { d },
                    _ =>
                    {
                        var dsc = d.Target.GetDescriptor();
                        return ExportDescriptor.Create((c, o) =>
                        {
                            return new ExportFactory<TProduct, TMetadata>(() =>
                            {
                                var lifetimeContext = new LifetimeContext(c, boundaries);
                                return Tuple.Create<TProduct, Action>((TProduct)CompositionOperation.Run(lifetimeContext, dsc.Activator), lifetimeContext.Dispose);
                            },
                            metadataProvider(dsc.Metadata));
                        },
                        dsc.Metadata);
                    }))
                .ToArray();
        }
    }
}
