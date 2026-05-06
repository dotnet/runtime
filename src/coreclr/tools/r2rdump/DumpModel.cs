// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using ILCompiler.Reflection.ReadyToRun;

namespace R2RDump
{
    internal sealed class DumpModel : IAssemblyResolver
    {
        /// <summary>
        /// Probing extensions to use when looking up assemblies under reference paths.
        /// </summary>
        private readonly static string[] ProbeExtensions = new[] { ".ni.exe", ".ni.dll", ".exe", ".dll" };

        public bool DiffHideSameDisasm { get; init; }
        public bool Disasm { get; init; }
        public bool GC { get; init; }
        public bool HideOffsets { get; init; }
        public bool HideTransitions { get; init; }
        public bool InlineSignatureBinary { get; init; }
        public bool Pgo { get; init; }
        public bool Raw { get; init; }
        public bool Naked { get; init; }
        public bool Normalize { get; init; }
        public List<string> Reference { get; init; }
        public DirectoryInfo[] ReferencePath { get; init; }
        public bool SignatureBinary { get; init; }
        public bool SectionContents { get; init; }
        public SignatureFormattingOptions SignatureFormattingOptions { get; init; }
        public bool Unwind { get; init; }

        /// <summary>
        /// Try to locate a (reference) assembly based on an AssemblyRef handle using the list of explicit reference assemblies
        /// and the list of reference paths passed to R2RDump.
        /// </summary>
        /// <param name="metadataReader">Containing metadata reader for the assembly reference handle</param>
        /// <param name="assemblyReferenceHandle">Handle representing the assembly reference</param>
        /// <param name="parentFile">Name of assembly from which we're performing the lookup</param>
        /// <returns></returns>
        public IAssemblyMetadata FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile)
        {
            string simpleName = metadataReader.GetString(metadataReader.GetAssemblyReference(assemblyReferenceHandle).Name);
            return FindAssembly(simpleName, parentFile);
        }

        /// <summary>
        /// Try to locate a (reference) assembly using the list of explicit reference assemblies
        /// and the list of reference paths passed to R2RDump.
        /// </summary>
        /// <param name="simpleName">Simple name of the assembly to look up</param>
        /// <param name="parentFile">Name of assembly from which we're performing the lookup</param>
        /// <returns></returns>
        public IAssemblyMetadata FindAssembly(string simpleName, string parentFile)
        {
            foreach (string refAsm in Reference)
            {
                if (Path.GetFileNameWithoutExtension(refAsm).Equals(simpleName, StringComparison.OrdinalIgnoreCase))
                {
                    return Open(refAsm);
                }
            }

            IEnumerable<string> allRefPaths = new string[] { Path.GetDirectoryName(parentFile) }
                .Concat((ReferencePath ?? Enumerable.Empty<DirectoryInfo>()).Select(path => path.FullName));

            foreach (string refPath in allRefPaths)
            {
                foreach (string extension in ProbeExtensions)
                {
                    try
                    {
                        string probeFile = Path.Combine(refPath, simpleName + extension);
                        if (File.Exists(probeFile))
                        {
                            return Open(probeFile);
                        }
                    }
                    catch (BadImageFormatException)
                    {
                    }
                }
            }

            return null;

            static IAssemblyMetadata Open(string filename)
            {
                byte[] image = File.ReadAllBytes(filename);

                PEReader peReader = new PEReader(Unsafe.As<byte[], ImmutableArray<byte>>(ref image));

                if (!peReader.HasMetadata)
                {
                    throw new BadImageFormatException($"ECMA metadata not found in file '{filename}'");
                }

                return new StandaloneAssemblyMetadata(peReader);
            }
        }
    }
}
