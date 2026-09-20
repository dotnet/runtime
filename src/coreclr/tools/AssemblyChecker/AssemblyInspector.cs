// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AssemblyChecker
{
    internal static class AssemblyInspector
    {
        internal static bool IsDebug(string path)
        {
            string assemblyPath = Path.GetFullPath(path);
            string? assemblyDirectory = Path.GetDirectoryName(assemblyPath);
            Debug.Assert(assemblyDirectory is not null);

            using MetadataLoadContext loadContext = new(new AssemblyResolver(assemblyDirectory));
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

        private sealed class AssemblyResolver(string assemblyDirectory) : MetadataAssemblyResolver
        {
            private readonly string _assemblyDirectory = assemblyDirectory;
            private readonly string _runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();

            public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
            {
                if (assemblyName.Name is not string name)
                {
                    return null;
                }

                string fileName = name + ".dll";
                string assemblyPath = Path.Combine(_assemblyDirectory, fileName);
                if (!File.Exists(assemblyPath))
                {
                    assemblyPath = Path.Combine(_runtimeDirectory, fileName);
                }

                return File.Exists(assemblyPath) ? context.LoadFromAssemblyPath(assemblyPath) : null;
            }
        }
    }
}
