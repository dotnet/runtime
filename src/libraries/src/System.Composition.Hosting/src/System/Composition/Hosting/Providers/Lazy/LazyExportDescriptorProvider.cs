// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reflection;
using System.Composition.Hosting.Core;
using System.Composition.Runtime;
using System.Collections.Generic;
using System.Composition.Hosting.Util;
using System.Composition.Hosting.Properties;

namespace System.Composition.Hosting.Providers.Lazy
{
    internal class LazyExportDescriptorProvider : ExportDescriptorProvider
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
