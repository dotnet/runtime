// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace R2RDump
{
    class R2RReader
    {
        private readonly PEReader _peReader;
        private readonly MetadataReader _mdReader;

        /// <summary>
        /// Byte array containing the ReadyToRun image
        /// </summary>
        public byte[] Image { get; }

        /// <summary>
        /// Name of the image file
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// True if the image is ReadyToRun
        /// </summary>
        public bool IsR2R { get; }

        /// <summary>
        /// The type of target machine
        /// </summary>
        public Machine Machine { get; }

        /// <summary>
        /// The preferred address of the first byte of image when loaded into memory; 
        /// must be a multiple of 64K.
        /// </summary>
        public ulong ImageBase { get; }

        /// <summary>
        /// The ReadyToRun header
        /// </summary>
        public R2RHeader R2RHeader { get; }

        /// <summary>
        /// The runtime functions and method signatures of each method
        /// </summary>
        public IList<R2RMethod> R2RMethods { get; }

        /// <summary>
        /// The available types from READYTORUN_SECTION_AVAILABLE_TYPES
        /// </summary>
        public List<string> AvailableTypes { get; }

        /// <summary>
        /// Initializes the fields of the R2RHeader and R2RMethods
        /// </summary>
        /// <param name="filename">PE image</param>
        /// <exception cref="BadImageFormatException">The Cor header flag must be ILLibrary</exception>
        public unsafe R2RReader(string filename)
        {
            Filename = filename;
            Image = File.ReadAllBytes(filename);

            fixed (byte* p = Image)
            {
                IntPtr ptr = (IntPtr)p;
                _peReader = new PEReader(p, Image.Length);

                IsR2R = (_peReader.PEHeaders.CorHeader.Flags == CorFlags.ILLibrary);
                if (!IsR2R)
                {
                    throw new BadImageFormatException("The file is not a ReadyToRun image");
                }

                Machine = _peReader.PEHeaders.CoffHeader.Machine;
                ImageBase = _peReader.PEHeaders.PEHeader.ImageBase;

                // initialize R2RHeader
                DirectoryEntry r2rHeaderDirectory = _peReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory;
                int r2rHeaderOffset = GetOffset(r2rHeaderDirectory.RelativeVirtualAddress);
                R2RHeader = new R2RHeader(Image, r2rHeaderDirectory.RelativeVirtualAddress, r2rHeaderOffset);
                if (r2rHeaderDirectory.Size != R2RHeader.Size)
                {
                    throw new BadImageFormatException("The calculated size of the R2RHeader doesn't match the size saved in the ManagedNativeHeaderDirectory");
                }

                if (_peReader.HasMetadata)
                {
                    _mdReader = _peReader.GetMetadataReader();

                    int runtimeFunctionSize = CalculateRuntimeFunctionSize();
                    R2RSection runtimeFunctionSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_RUNTIME_FUNCTIONS];
                    uint nRuntimeFunctions = (uint)(runtimeFunctionSection.Size / runtimeFunctionSize);
                    int runtimeFunctionOffset = GetOffset(runtimeFunctionSection.RelativeVirtualAddress);
                    bool[] isEntryPoint = new bool[nRuntimeFunctions];

                    // initialize R2RMethods
                    R2RMethods = new List<R2RMethod>();
                    ParseMethodDefEntrypoints(isEntryPoint);
                    ParseInstanceMethodEntrypoints(isEntryPoint);
                    ParseRuntimeFunctions(isEntryPoint, runtimeFunctionOffset, runtimeFunctionSize);

                    AvailableTypes = new List<string>();
                    ParseAvailableTypes();
                }
            }
        }

        /// <summary>
        /// Each runtime function entry has 3 fields for Amd64 machines (StartAddress, EndAddress, UnwindRVA), otherwise 2 fields (StartAddress, UnwindRVA)
        /// </summary>
        private int CalculateRuntimeFunctionSize()
        {
            if (Machine == Machine.Amd64)
            {
                return 3 * sizeof(int);
            }
            return 2 * sizeof(int);
        }

        /// <summary>
        /// Initialize non-generic R2RMethods with method signatures from MethodDefHandle, and runtime function indices from MethodDefEntryPoints
        /// </summary>
        private void ParseMethodDefEntrypoints(bool[] isEntryPoint)
        {
            int methodDefEntryPointsRVA = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_METHODDEF_ENTRYPOINTS].RelativeVirtualAddress;
            int methodDefEntryPointsOffset = GetOffset(methodDefEntryPointsRVA);
            NativeArray methodEntryPoints = new NativeArray(Image, (uint)methodDefEntryPointsOffset);
            uint nMethodEntryPoints = methodEntryPoints.GetCount();

            for (uint rid = 1; rid <= nMethodEntryPoints; rid++)
            {
                int offset = 0;
                if (methodEntryPoints.TryGetAt(Image, rid - 1, ref offset))
                {
                    R2RMethod method = new R2RMethod(Image, _mdReader, rid, GetEntryPointIdFromOffset(offset), null, null);

                    if (method.EntryPointRuntimeFunctionId < 0 || method.EntryPointRuntimeFunctionId >= isEntryPoint.Length)
                    {
                        throw new BadImageFormatException("EntryPointRuntimeFunctionId out of bounds");
                    }
                    isEntryPoint[method.EntryPointRuntimeFunctionId] = true;
                    R2RMethods.Add(method);
                }
            }
        }

        /// <summary>
        /// Initialize generic method instances with argument types and runtime function indices from InstanceMethodEntrypoints
        /// </summary>
        private void ParseInstanceMethodEntrypoints(bool[] isEntryPoint)
        {
            R2RSection instMethodEntryPointSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS];
            int instMethodEntryPointsOffset = GetOffset(instMethodEntryPointSection.RelativeVirtualAddress);
            NativeParser parser = new NativeParser(Image, (uint)instMethodEntryPointsOffset);
            NativeHashtable instMethodEntryPoints = new NativeHashtable(Image, parser);
            NativeHashtable.AllEntriesEnumerator allEntriesEnum = instMethodEntryPoints.EnumerateAllEntries();
            NativeParser curParser = allEntriesEnum.GetNext();
            while (!curParser.IsNull())
            {
                uint methodFlags = curParser.GetCompressedData();
                uint rid = curParser.GetCompressedData();
                if ((methodFlags & (byte)R2RMethod.EncodeMethodSigFlags.ENCODE_METHOD_SIG_MethodInstantiation) != 0)
                {
                    uint nArgs = curParser.GetCompressedData();
                    R2RMethod.GenericElementTypes[] args = new R2RMethod.GenericElementTypes[nArgs];
                    uint[] tokens = new uint[nArgs];
                    for (int i = 0; i < nArgs; i++)
                    {
                        args[i] = (R2RMethod.GenericElementTypes)curParser.GetByte();
                        if (args[i] == R2RMethod.GenericElementTypes.ValueType)
                        {
                            tokens[i] = curParser.GetCompressedData();
                            tokens[i] = (tokens[i] >> 2);
                        }
                    }

                    uint id = curParser.GetUnsigned();
                    id = id >> 1;
                    R2RMethod method = new R2RMethod(Image, _mdReader, rid, (int)id, args, tokens);
                    if (method.EntryPointRuntimeFunctionId >= 0 && method.EntryPointRuntimeFunctionId < isEntryPoint.Length)
                    {
                        isEntryPoint[method.EntryPointRuntimeFunctionId] = true;
                    }
                    R2RMethods.Add(method);
                }
                curParser = allEntriesEnum.GetNext();
            }
        }

        /// <summary>
        /// Get the RVAs of the runtime functions for each method
        /// </summary>
        private void ParseRuntimeFunctions(bool[] isEntryPoint, int runtimeFunctionOffset, int runtimeFunctionSize)
        {
            int curOffset = 0;
            foreach (R2RMethod method in R2RMethods)
            {
                int runtimeFunctionId = method.EntryPointRuntimeFunctionId;
                if (runtimeFunctionId == -1)
                    continue;
                curOffset = runtimeFunctionOffset + runtimeFunctionId * runtimeFunctionSize;
                do
                {
                    int startRva = NativeReader.ReadInt32(Image, ref curOffset);
                    int endRva = -1;
                    if (Machine == Machine.Amd64)
                    {
                        endRva = NativeReader.ReadInt32(Image, ref curOffset);
                    }
                    int unwindRva = NativeReader.ReadInt32(Image, ref curOffset);

                    method.RuntimeFunctions.Add(new RuntimeFunction(runtimeFunctionId, startRva, endRva, unwindRva, method));
                    runtimeFunctionId++;
                }
                while (runtimeFunctionId < isEntryPoint.Length && !isEntryPoint[runtimeFunctionId]);
            }
        }

        private void ParseAvailableTypes()
        {
            R2RSection availableTypesSection = R2RHeader.Sections[R2RSection.SectionType.READYTORUN_SECTION_AVAILABLE_TYPES];
            int availableTypesOffset = GetOffset(availableTypesSection.RelativeVirtualAddress);
            NativeParser parser = new NativeParser(Image, (uint)availableTypesOffset);
            NativeHashtable availableTypes = new NativeHashtable(Image, parser);
            NativeHashtable.AllEntriesEnumerator allEntriesEnum = availableTypes.EnumerateAllEntries();
            NativeParser curParser = allEntriesEnum.GetNext();
            while (!curParser.IsNull())
            {
                uint rid = curParser.GetUnsigned();
                rid = rid >> 1;
                TypeDefinitionHandle typeDefHandle = MetadataTokens.TypeDefinitionHandle((int)rid);

                TypeDefinition typeDef = _mdReader.GetTypeDefinition(typeDefHandle);
                string name = _mdReader.GetString(typeDef.Name);
                if (!typeDef.Namespace.IsNil)
                {
                    name = _mdReader.GetString(typeDef.Namespace) + "." + name;
                }
                AvailableTypes.Add(name);
                curParser = allEntriesEnum.GetNext();
            }
        }

        /// <summary>
        /// Get the index in the image byte array corresponding to the RVA
        /// </summary>
        /// <param name="rva">The relative virtual address</param>
        public int GetOffset(int rva)
        {
            int index = _peReader.PEHeaders.GetContainingSectionIndex(rva);
            SectionHeader containingSection = _peReader.PEHeaders.SectionHeaders[index];
            return rva - containingSection.VirtualAddress + containingSection.PointerToRawData;
        }

        /// <summary>
        /// Reads the method entrypoint from the offset. Used for non-generic methods
        /// </summary>
        private int GetEntryPointIdFromOffset(int offset)
        {
            // get the id of the entry point runtime function from the MethodEntryPoints NativeArray
            uint id = 0; // the RUNTIME_FUNCTIONS index
            offset = (int)NativeReader.DecodeUnsigned(Image, (uint)offset, ref id);
            if ((id & 1) != 0)
            {
                if ((id & 2) != 0)
                {
                    uint val = 0;
                    NativeReader.DecodeUnsigned(Image, (uint)offset, ref val);
                    offset -= (int)val;
                }
                // TODO: Dump fixups

                id >>= 2;
            }
            else
            {
                id >>= 1;
            }

            return (int)id;
        }
    }
}
