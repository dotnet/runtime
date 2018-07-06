// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Composition.Hosting.Core;
using System.Collections.Generic;
using System.Composition.Hosting.Util;

namespace System.Composition.Hosting.Providers.ExportFactory
{
    internal class ExportFactoryExportDescriptorProvider : ExportDescriptorProvider
    {
        private static readonly MethodInfo s_getExportFactoryDefinitionsMethod = typeof(ExportFactoryExportDescriptorProvider).GetTypeInfo().GetDeclaredMethod("GetExportFactoryDescriptors");

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract exportKey, DependencyAccessor definitionAccessor)
        {
            if (!exportKey.ContractType.IsConstructedGenericType || exportKey.ContractType.GetGenericTypeDefinition() != typeof(ExportFactory<>))
                return NoExportDescriptors;

            var gld = s_getExportFactoryDefinitionsMethod.MakeGenericMethod(exportKey.ContractType.GenericTypeArguments[0]);
            var gldm = gld.CreateStaticDelegate<Func<CompositionContract, DependencyAccessor, object>>();
            return (ExportDescriptorPromise[])gldm(exportKey, definitionAccessor);
        }

        private static ExportDescriptorPromise[] GetExportFactoryDescriptors<TProduct>(CompositionContract exportFactoryContract, DependencyAccessor definitionAccessor)
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

            return definitionAccessor.ResolveDependencies("product", productContract, false)
                .Select(d => new ExportDescriptorPromise(
                    exportFactoryContract,
                    Formatters.Format(typeof(ExportFactory<TProduct>)),
                    false,
                    () => new[] { d },
                    _ =>
                    {
                        var dsc = d.Target.GetDescriptor();
                        var da = dsc.Activator;
                        return ExportDescriptor.Create((c, o) =>
                            {
                                return new ExportFactory<TProduct>(() =>
                                {
                                    var lifetimeContext = new LifetimeContext(c, boundaries);
                                    return Tuple.Create<TProduct, Action>((TProduct)CompositionOperation.Run(lifetimeContext, da), lifetimeContext.Dispose);
                                });
                            },
                            dsc.Metadata);
                    }))
                .ToArray();
        }
    }
}
