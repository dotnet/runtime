// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.IL;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.IO.Compression;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Diagnostics.CodeAnalysis;
using ILCompiler.Reflection.ReadyToRun;
using Microsoft.Diagnostics.Tools.Pgo;
using Internal.Pgo;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    struct MIbcData
    {
        public object MetadataObject;
    }


    class MibcReader
    {
        static IEnumerable<MIbcData> ReadMIbcGroup(EcmaMethod method)
        {
            EcmaMethodIL ilBody = EcmaMethodIL.Create((EcmaMethod)method);
            byte[] ilBytes = ilBody.GetILBytes();
            int currentOffset = 0;
            object metadataObject = null;
            while (currentOffset < ilBytes.Length)
            {
                ILOpcode opcode = (ILOpcode)ilBytes[currentOffset];
                if (opcode == ILOpcode.prefix1)
                    opcode = 0x100 + (ILOpcode)ilBytes[currentOffset + 1];
                switch (opcode)
                {
                    case ILOpcode.ldtoken:
                        UInt32 token = BinaryPrimitives.ReadUInt32LittleEndian(ilBytes.AsSpan(currentOffset + 1));

                        if (metadataObject == null)
                            metadataObject = ilBody.GetObject((int)token);
                        break;
                    case ILOpcode.pop:
                        MIbcData mibcData = new MIbcData();
                        mibcData.MetadataObject = metadataObject;
                        yield return mibcData;

                        metadataObject = null;
                        break;
                }

                // This isn't correct if there is a switch opcode, but since we won't do that, its ok
                currentOffset += opcode.GetSize();
            }
        }

        class CanonModule : ModuleDesc, IAssemblyDesc
        {
            public CanonModule(TypeSystemContext wrappedContext) : base(wrappedContext, null)
            {
            }

            public override IEnumerable<MetadataType> GetAllTypes()
            {
                throw new NotImplementedException();
            }

            public override MetadataType GetGlobalModuleType()
            {
                throw new NotImplementedException();
            }

            public override MetadataType GetType(string nameSpace, string name, bool throwIfNotFound = true)
            {
                TypeSystemContext context = Context;

                if (context.SupportsCanon && (nameSpace == context.CanonType.Namespace) && (name == context.CanonType.Name))
                    return Context.CanonType;
                if (context.SupportsUniversalCanon && (nameSpace == context.UniversalCanonType.Namespace) && (name == context.UniversalCanonType.Name))
                    return Context.UniversalCanonType;
                else
                {
                    if (throwIfNotFound)
                    {
                        throw new TypeLoadException($"{nameSpace}.{name}");
                    }
                    return null;
                }
            }

            public AssemblyName GetName()
            {
                return new AssemblyName("System.Private.Canon");
            }
        }

        class CustomCanonResolver : IModuleResolver
        {
            CanonModule _canonModule;
            AssemblyName _canonModuleName;
            IModuleResolver _wrappedResolver;

            public CustomCanonResolver(TypeSystemContext wrappedContext)
            {
                _canonModule = new CanonModule(wrappedContext);
                _canonModuleName = _canonModule.GetName();
                _wrappedResolver = wrappedContext;
            }

            ModuleDesc IModuleResolver.ResolveAssembly(AssemblyName name, bool throwIfNotFound)
            {
                if (name.Name == _canonModuleName.Name)
                    return _canonModule;
                else
                    return _wrappedResolver.ResolveAssembly(name, throwIfNotFound);
            }

            ModuleDesc IModuleResolver.ResolveModule(IAssemblyDesc referencingModule, string fileName, bool throwIfNotFound)
            {
                return _wrappedResolver.ResolveModule(referencingModule, fileName, throwIfNotFound);
            }
        }

        public static IEnumerable<MIbcData> ReadMIbcData(TypeSystemContext tsc, System.Reflection.PortableExecutable.PEReader peReader)
        {
            var module = EcmaModule.Create(tsc, peReader, null, null, new CustomCanonResolver(tsc));

            var loadedMethod = (EcmaMethod)module.GetGlobalModuleType().GetMethod("AssemblyDictionary", null);
            EcmaMethodIL ilBody = EcmaMethodIL.Create(loadedMethod);
            byte[] ilBytes = ilBody.GetILBytes();
            int currentOffset = 0;
            while (currentOffset < ilBytes.Length)
            {
                ILOpcode opcode = (ILOpcode)ilBytes[currentOffset];
                if (opcode == ILOpcode.prefix1)
                    opcode = 0x100 + (ILOpcode)ilBytes[currentOffset + 1];
                switch (opcode)
                {
                    case ILOpcode.ldtoken:
                        UInt32 token = BinaryPrimitives.ReadUInt32LittleEndian(ilBytes.AsSpan(currentOffset + 1));
                        foreach (var data in ReadMIbcGroup((EcmaMethod)ilBody.GetObject((int)token)))
                            yield return data;
                        break;
                    case ILOpcode.pop:
                        break;
                }

                // This isn't correct if there is a switch opcode, but since we won't do that, its ok
                currentOffset += opcode.GetSize();
            }
            GC.KeepAlive(peReader);
        }
    }
}
