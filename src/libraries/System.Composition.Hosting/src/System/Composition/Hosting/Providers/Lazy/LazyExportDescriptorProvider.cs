// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Composition.Hosting.Core;
using System.Collections.Generic;
using System.Composition.Hosting.Util;

namespace System.Composition.Hosting.Providers.Lazy
{
    internal sealed class LazyExportDescriptorProvider : ExportDescriptorProvider
    {
        private static readonly MethodInfo s_getLazyDefinitionsMethod = typeof(LazyExportDescriptorProvider)
            .GetTypeInfo().GetDeclaredMethod("GetLazyDefinitions");

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract exportKey, DependencyAccessor definitionAccessor)
        {
            if (!exportKey.ContractType.IsConstructedGenericType || exportKey.ContractType.GetGenericTypeDefinition() != typeof(Lazy<>))
                return NoExportDescriptors;

            var gld = s_getLazyDefinitionsMethod.MakeGenericMethod(exportKey.ContractType.GenericTypeArguments[0]);
            var gldm = gld.CreateStaticDelegate<Func<CompositionContract, DependencyAccessor, object>>();
            return (ExportDescriptorPromise[])gldm(exportKey, definitionAccessor);
        }

        private static ExportDescriptorPromise[] GetLazyDefinitions<TValue>(CompositionContract lazyContract, DependencyAccessor definitionAccessor)
        {
            return definitionAccessor.ResolveDependencies("value", lazyContract.ChangeType(typeof(TValue)), false)
                .Select(d => new ExportDescriptorPromise(
                    lazyContract,
                    Formatters.Format(typeof(Lazy<TValue>)),
                    false,
                    () => new[] { d },
                    _ =>
                    {
                        var dsc = d.Target.GetDescriptor();
                        var da = dsc.Activator;
                        return ExportDescriptor.Create((c, o) => new Lazy<TValue>(() => (TValue)CompositionOperation.Run(c, da)), dsc.Metadata);
                    }))
                .ToArray();
        }
    }
}
