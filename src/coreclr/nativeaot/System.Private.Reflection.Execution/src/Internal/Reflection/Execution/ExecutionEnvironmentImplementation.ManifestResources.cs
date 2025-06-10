// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Internal.NativeFormat;
using Internal.Reflection.Core.Execution;
using Internal.Runtime;
using Internal.Runtime.TypeLoader;

namespace Internal.Reflection.Execution
{
    //==========================================================================================================
    // These ExecutionEnvironment entrypoints implement support for manifest resource streams on the Assembly class.
    //==========================================================================================================
    internal sealed partial class ExecutionEnvironmentImplementation : ExecutionEnvironment
    {
        public sealed override ManifestResourceInfo GetManifestResourceInfo(Assembly assembly, string resourceName)
        {
            ArgumentNullException.ThrowIfNull(resourceName);

            foreach (ResourceInfo resourceInfo in ExtractResources(assembly))
            {
                if (resourceInfo.Name == resourceName)
                {
                    return new ManifestResourceInfo(null, null, ResourceLocation.Embedded | ResourceLocation.ContainedInManifestFile);
                }
            }

            return null;
        }

        public sealed override string[] GetManifestResourceNames(Assembly assembly)
        {
            ArrayBuilder<string> arrayBuilder = default;

            foreach (ResourceInfo resourceInfo in ExtractResources(assembly))
            {
                arrayBuilder.Add(resourceInfo.Name);
            }

            return arrayBuilder.ToArray();
        }

        public sealed override Stream GetManifestResourceStream(Assembly assembly, string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            foreach (ResourceInfo resourceInfo in ExtractResources(assembly))
            {
                if (resourceInfo.Name == name)
                {
                    return ReadResourceFromBlob(resourceInfo);
                }
            }

            return null;
        }

        private static unsafe UnmanagedMemoryStream ReadResourceFromBlob(ResourceInfo resourceInfo)
        {
            byte* pBlob;
            uint cbBlob;

            if (!resourceInfo.Module.TryFindBlob((int)ReflectionMapBlob.BlobIdResourceData, out pBlob, out cbBlob))
            {
                throw new BadImageFormatException();
            }

            // resourceInfo is read from the executable image, so check it only in debug builds
            Debug.Assert(resourceInfo.Index >= 0 && resourceInfo.Length >= 0 && (uint)(resourceInfo.Index + resourceInfo.Length) <= cbBlob);
            return new UnmanagedMemoryStream(pBlob + resourceInfo.Index, resourceInfo.Length);
        }

        private static IEnumerable<ResourceInfo> ExtractResources(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            string assemblyName = assembly.GetName().FullName;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                NativeReader reader;
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.BlobIdResourceIndex, out reader))
                {
                    continue;
                }
                NativeParser indexParser = new NativeParser(reader, 0);
                NativeHashtable indexHashTable = new NativeHashtable(indexParser);

                var entryEnumerator = indexHashTable.EnumerateAllEntries();
                NativeParser entryParser;
                while (!(entryParser = entryEnumerator.GetNext()).IsNull)
                {
                    string entryAssemblyName = entryParser.GetString();
                    string entryResourceName = entryParser.GetString();
                    int resourceOffset = (int)entryParser.GetUnsigned();
                    int resourceLength = (int)entryParser.GetUnsigned();

                    if (assemblyName == entryAssemblyName)
                    {
                        yield return new ResourceInfo(entryResourceName, resourceOffset, resourceLength, module);
                    }
                }
            }
        }

        private struct ResourceInfo
        {
            public ResourceInfo(string name, int index, int length, NativeFormatModuleInfo module)
            {
                Name = name;
                Index = index;
                Length = length;
                Module = module;
            }

            public string Name { get; }
            public int Index { get; }
            public int Length { get; }
            public NativeFormatModuleInfo Module { get; }
        }
    }
}
