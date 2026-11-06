// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        public static Task BindAssemblyExports(string? assemblyName)
        {
            ArgumentException.ThrowIfNullOrEmpty(assemblyName);

            Assembly assembly = Assembly.Load(new AssemblyName(assemblyName));
            if (TryBindMappedAssemblyExports(assembly, assemblyName))
            {
                return Task.CompletedTask;
            }

            Interop.Runtime.BindAssemblyExports(Marshal.StringToCoTaskMemUTF8(assemblyName));
            return Task.CompletedTask;
        }

        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        private static Type? GetExternalTypeMappingForAssembly<TTypeMapGroup>(Assembly assembly, string key)
        {
            Type? result = null;
            foreach (CustomAttributeData attribute in assembly.GetCustomAttributesData())
            {
                if (attribute.AttributeType != typeof(TypeMapAttribute<TTypeMapGroup>)
                    || (string)attribute.ConstructorArguments[0].Value! != key)
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Count != 2)
                    throw new PlatformNotSupportedException(SR.JSExportConditionalProviderNotSupported);

                if (result is not null)
                    throw new InvalidOperationException(SR.Format(SR.JSExportDuplicateProvider, key));

                result = (Type)attribute.ConstructorArguments[1].Value!;
            }

            return result;
        }
    }
}
