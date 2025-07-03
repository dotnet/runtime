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

            foreach (string name in ExtractResources(assembly))
            {
                if (name == resourceName)
                {
                    return new ManifestResourceInfo(null, null, ResourceLocation.Embedded | ResourceLocation.ContainedInManifestFile);
                }
            }

            return null;
        }

        public sealed override string[] GetManifestResourceNames(Assembly assembly)
        {
            ArrayBuilder<string> arrayBuilder = default;

            foreach (string name in ExtractResources(assembly))
            {
                arrayBuilder.Add(name);
            }

            return arrayBuilder.ToArray();
        }

        public sealed override Stream GetManifestResourceStream(Assembly assembly, string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            Debug.Assert(assembly != null);
            string assemblyName = assembly.GetName().FullName;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.BlobIdResourceIndex, out NativeReader reader))
                {
                    continue;
                }
                NativeParser indexParser = new NativeParser(reader, 0);
                NativeHashtable indexHashTable = new NativeHashtable(indexParser);

                var lookup = indexHashTable.Lookup(TypeHashingAlgorithms.ComputeNameHashCode(assemblyName));
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    if (entryParser.StringEquals(assemblyName))
                    {
                        entryParser.SkipString(); // assemblyName
                        if (entryParser.StringEquals(name))
                        {
                            entryParser.SkipString(); // resourceName
                            int resourceOffset = (int)entryParser.GetUnsigned();
                            int resourceLength = (int)entryParser.GetUnsigned();
                            return ReadResourceFromBlob(resourceOffset, resourceLength, module);
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

            return null;
        }

        private static unsafe UnmanagedMemoryStream ReadResourceFromBlob(int resourceOffset, int resourceLength, NativeFormatModuleInfo module)
        {
            if (!module.TryFindBlob((int)ReflectionMapBlob.BlobIdResourceData, out byte* pBlob, out uint cbBlob))
            {
                throw new BadImageFormatException();
            }

            // resourceInfo is read from the executable image, so check it only in debug builds
            Debug.Assert(resourceOffset >= 0 && resourceLength >= 0 && (uint)(resourceOffset + resourceLength) <= cbBlob);
            return new UnmanagedMemoryStream(pBlob + resourceOffset, resourceLength);
        }

        private static IEnumerable<string> ExtractResources(Assembly assembly)
        {
            Debug.Assert(assembly != null);
            string assemblyName = assembly.GetName().FullName;

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                if (!TryGetNativeReaderForBlob(module, ReflectionMapBlob.BlobIdResourceIndex, out NativeReader reader))
                {
                    continue;
                }
                NativeParser indexParser = new NativeParser(reader, 0);
                NativeHashtable indexHashTable = new NativeHashtable(indexParser);

                var lookup = indexHashTable.Lookup(TypeHashingAlgorithms.ComputeNameHashCode(assemblyName));
                NativeParser entryParser;
                while (!(entryParser = lookup.GetNext()).IsNull)
                {
                    if (entryParser.StringEquals(assemblyName))
                    {
                        entryParser.SkipString(); // assemblyName
                        yield return entryParser.GetString();
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
        }
    }
}
