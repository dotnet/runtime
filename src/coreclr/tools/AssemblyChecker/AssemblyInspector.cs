// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace AssemblyChecker
{
    internal static class AssemblyInspector
    {
        private static readonly RuntimeAssemblyResolver s_resolver = new();

        internal static bool IsDebug(string path)
        {
            string assemblyPath = Path.GetFullPath(path);
            using MetadataLoadContext loadContext = new(s_resolver);
            Assembly assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
            foreach (CustomAttributeData attribute in assembly.GetCustomAttributesData())
            {
                if (attribute.AttributeType.FullName != typeof(DebuggableAttribute).FullName)
                {
                    continue;
                }

                IList<CustomAttributeTypedArgument> arguments = attribute.ConstructorArguments;
                if (arguments.Count == 1)
                {
                    if (arguments[0].Value is not int modes)
                    {
                        throw new BadImageFormatException();
                    }

                    if (((DebuggableAttribute.DebuggingModes)modes & DebuggableAttribute.DebuggingModes.DisableOptimizations) != 0)
                    {
                        return true;
                    }
                }
                else if (arguments.Count == 2)
                {
                    if (arguments[1].Value is not bool optimizationsDisabled)
                    {
                        throw new BadImageFormatException();
                    }

                    if (optimizationsDisabled)
                    {
                        return true;
                    }
                }
                else
                {
                    throw new BadImageFormatException();
                }
            }

            return false;
        }

        private sealed class RuntimeAssemblyResolver : MetadataAssemblyResolver
        {
            private readonly string _runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)
                ?? throw new InvalidOperationException("The runtime assembly directory is not available.");

            public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
            {
                if (assemblyName.Name is not string name)
                {
                    return null;
                }

                string assemblyPath = Path.Combine(_runtimeDirectory, name + ".dll");
                return File.Exists(assemblyPath) ? context.LoadFromAssemblyPath(assemblyPath) : null;
            }
        }
    }
}
