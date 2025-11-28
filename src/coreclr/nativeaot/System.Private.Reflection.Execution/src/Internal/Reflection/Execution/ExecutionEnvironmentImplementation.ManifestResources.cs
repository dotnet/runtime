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
            if (FindResourceWithName(assembly, resourceName).Module != null)
            {
                return new ManifestResourceInfo(null, null, ResourceLocation.Embedded | ResourceLocation.ContainedInManifestFile);
            }

            return null;
        }

        public sealed override string[] GetManifestResourceNames(Assembly assembly)
        {
            string assemblyName = assembly.GetName().FullName;
            int assemblyNameHash = TypeHashingAlgorithms.ComputeNameHashCode(assemblyName);
            ArrayBuilder<string> arrayBuilder = default;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.BlobIdResourceIndex, out NativeReader reader))
                {
                    continue;
                }
                NativeParser indexParser = new NativeParser(reader, 0);
                NativeHashtable indexHashTable = new NativeHashtable(indexParser);

                var lookup = indexHashTable.Lookup(assemblyNameHash);
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    if (entryParser.StringEquals(assemblyName))
                    {
                        entryParser.SkipString(); // assemblyName
                        arrayBuilder.Add(entryParser.GetString());
                    }
                    else
                    {
                        entryParser.SkipString(); // assemblyName
                        entryParser.SkipString(); // resourceName
                    }
                    entryParser.SkipInteger(); // offset
                    entryParser.SkipInteger(); // length
                }
            }

            return arrayBuilder.ToArray();
        }

        public sealed override Stream GetManifestResourceStream(Assembly assembly, string name)
        {
            ResourceInfo resourceInfo = FindResourceWithName(assembly, name);
            if (resourceInfo.Module != null)
            {
                return ReadResourceFromBlob(resourceInfo);
            }

            return null;
        }

        private static unsafe UnmanagedMemoryStream ReadResourceFromBlob(ResourceInfo resourceInfo)
        {
            if (!resourceInfo.Module.TryFindBlob((int)ReflectionMapBlob.BlobIdResourceData, out byte* pBlob, out uint cbBlob))
            {
                throw new BadImageFormatException();
            }

            // resourceInfo is read from the executable image, so check it only in debug builds
            Debug.Assert(resourceInfo.Index >= 0 && resourceInfo.Length >= 0 && (uint)(resourceInfo.Index + resourceInfo.Length) <= cbBlob);
            return new UnmanagedMemoryStream(pBlob + resourceInfo.Index, resourceInfo.Length);
        }

        private static ResourceInfo FindResourceWithName(Assembly assembly, string resourceName)
        {
            string assemblyName = assembly.GetName().FullName;
            int assemblyNameHash = TypeHashingAlgorithms.ComputeNameHashCode(assemblyName);

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.BlobIdResourceIndex, out NativeReader reader))
                {
                    continue;
                }
                NativeParser indexParser = new NativeParser(reader, 0);
                NativeHashtable indexHashTable = new NativeHashtable(indexParser);

                var lookup = indexHashTable.Lookup(assemblyNameHash);
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    if (entryParser.StringEquals(assemblyName))
                    {
                        entryParser.SkipString(); // assemblyName
                        if (entryParser.StringEquals(resourceName))
                        {
                            entryParser.SkipString(); // resourceName
                            int resourceOffset = (int)entryParser.GetUnsigned();
                            int resourceLength = (int)entryParser.GetUnsigned();
                            return new ResourceInfo(resourceOffset, resourceLength, module);
                        }
                    }
                    else
                    {
                        entryParser.SkipString(); // assemblyName
                    }
                    entryParser.SkipString(); // resourceName
                    entryParser.SkipInteger(); // offset
                    entryParser.SkipInteger(); // length
                }
            }

            return default;
        }

        private struct ResourceInfo
        {
            public ResourceInfo(int index, int length, NativeFormatModuleInfo module)
            {
                Index = index;
                Length = length;
                Module = module;
            }

            public int Index { get; }
            public int Length { get; }
            public NativeFormatModuleInfo Module { get; }
        }
    }
}
