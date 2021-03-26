// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Composition.Hosting.Util;
using System.Collections.Generic;
using System.Composition.Hosting.Providers.Metadata;

namespace System.Composition.Hosting.Providers.Lazy
{
    internal sealed class LazyWithMetadataExportDescriptorProvider : ExportDescriptorProvider
    {
        private static readonly MethodInfo s_getLazyDefinitionsMethod = typeof(LazyWithMetadataExportDescriptorProvider).GetTypeInfo().GetDeclaredMethod("GetLazyDefinitions");

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract exportKey, DependencyAccessor definitionAccessor)
        {
            if (!exportKey.ContractType.IsConstructedGenericType || exportKey.ContractType.GetGenericTypeDefinition() != typeof(Lazy<,>))
                return NoExportDescriptors;

            var ga = exportKey.ContractType.GenericTypeArguments;
            var gld = s_getLazyDefinitionsMethod.MakeGenericMethod(ga[0], ga[1]);
            var gldm = gld.CreateStaticDelegate<Func<CompositionContract, DependencyAccessor, object>>();
            return (ExportDescriptorPromise[])gldm(exportKey, definitionAccessor);
        }

        private static ExportDescriptorPromise[] GetLazyDefinitions<TValue, TMetadata>(CompositionContract lazyContract, DependencyAccessor definitionAccessor)
        {
            var metadataProvider = MetadataViewProvider.GetMetadataViewProvider<TMetadata>();

            return definitionAccessor.ResolveDependencies("value", lazyContract.ChangeType(typeof(TValue)), false)
                .Select(d => new ExportDescriptorPromise(
                    lazyContract,
                    Formatters.Format(typeof(Lazy<TValue, TMetadata>)),
                    false,
                    () => new[] { d },
                    _ =>
                    {
                        var dsc = d.Target.GetDescriptor();
                        var da = dsc.Activator;
                        return ExportDescriptor.Create((c, o) =>
                        {
                            return new Lazy<TValue, TMetadata>(() => (TValue)CompositionOperation.Run(c, da), metadataProvider(dsc.Metadata));
                        }, dsc.Metadata);
                    }))
                .ToArray();
        }
    }
}
