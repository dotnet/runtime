// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.IL;

using System.Linq;
using System.IO;
using System.Diagnostics;

namespace ILCompiler.IBC
{
    static class MIbcProfileParser
    {
        /// <summary>
        /// Parse an MIBC file for the methods that are interesting.
        /// The version bubble must be specified and will describe the restrict the set of methods parsed to those relevant to the compilation
        /// The onlyDefinedInAssembly parameter is used to restrict the set of types parsed to include only those which are defined in a specific module. Specify null to allow definitions from all modules.
        /// This limited parsing is not necessarily an exact set of prevention, so detailed algorithms that work at the individual method level are still necessary, but this allows avoiding excessive parsing.
        ///
        /// The format of the Mibc file is that of a .NET dll, with a global method named "AssemblyDictionary". Inside of that file are a series of references that are broken up by which assemblies define the individual methods.
        /// These references are encoded as IL code that represents the details.
        /// The format of these IL instruction is as follows.
        ///
        /// ldstr mibcGroupName
        /// ldtoken mibcGroupMethod
        /// pop
        /// {Repeat the above pattern N times, once per Mibc group}
        ///
        /// See comment above ReadMIbcGroup for details of the group format
        ///
        /// The mibcGroupName is in the following format "Assembly_{definingAssemblyName};{OtherAssemblyName};{OtherAssemblyName};...; (OtherAssemblyName is ; delimited)
        /// 
        /// </summary>
        /// <returns></returns>
        public static ProfileData ParseMIbcFile(CompilerTypeSystemContext tsc, string filename, HashSet<string> assemblyNamesInVersionBubble, string onlyDefinedInAssembly)
        {
            byte[] peData;

            using (var zipFile = ZipFile.OpenRead(filename))
            {
                var mibcDataEntry = zipFile.GetEntry(Path.GetFileName(filename) + ".dll");
                using (var mibcDataStream = mibcDataEntry.Open())
                {
                    peData = new byte[mibcDataEntry.Length];
                    using (BinaryReader br = new BinaryReader(mibcDataStream))
                    {
                        peData = br.ReadBytes(checked((int)mibcDataEntry.Length));
                    }
                }
            }

            using (var peReader = new System.Reflection.PortableExecutable.PEReader(System.Collections.Immutable.ImmutableArray.Create<byte>(peData)))
            {
                var mibcModule = EcmaModule.Create(tsc, peReader, null, null, new CustomCanonResolver(tsc));

                var assemblyDictionary = (EcmaMethod)mibcModule.GetGlobalModuleType().GetMethod("AssemblyDictionary", null);
                IEnumerable<MethodProfileData> loadedMethodProfileData = Enumerable.Empty<MethodProfileData>();

                EcmaMethodIL ilBody = EcmaMethodIL.Create(assemblyDictionary);
                byte[] ilBytes = ilBody.GetILBytes();
                int currentOffset = 0;

                string mibcGroupName = "";
                while (currentOffset < ilBytes.Length)
                {
                    ILOpcode opcode = (ILOpcode)ilBytes[currentOffset];
                    if (opcode == ILOpcode.prefix1)
                        opcode = 0x100 + (ILOpcode)ilBytes[currentOffset + 1];
                    switch (opcode)
                    {
                        case ILOpcode.ldstr:
                            if (mibcGroupName == "")
                            {
                                UInt32 userStringToken = (UInt32)(ilBytes[currentOffset + 1] + (ilBytes[currentOffset + 2] << 8) + (ilBytes[currentOffset + 3] << 16) + (ilBytes[currentOffset + 4] << 24));
                                mibcGroupName = (string)ilBody.GetObject((int)userStringToken);
                            }
                            break;

                        case ILOpcode.ldtoken:
                            if (String.IsNullOrEmpty(mibcGroupName))
                                break;

                            string[] assembliesByName = mibcGroupName.Split(';');

                            bool hasMatchingDefinition = (onlyDefinedInAssembly == null) || assembliesByName[0].Equals(onlyDefinedInAssembly);

                            if (!hasMatchingDefinition)
                                break;

                            bool areAllEntriesInVersionBubble = true;
                            foreach (string s in assembliesByName)
                            {
                                if (string.IsNullOrEmpty(s))
                                    continue;

                                if (!assemblyNamesInVersionBubble.Contains(s))
                                {
                                    areAllEntriesInVersionBubble = false;
                                    break;
                                }
                            }

                            if (!areAllEntriesInVersionBubble)
                                break;

                            uint token = (uint)(ilBytes[currentOffset + 1] + (ilBytes[currentOffset + 2] << 8) + (ilBytes[currentOffset + 3] << 16) + (ilBytes[currentOffset + 4] << 24));
                            loadedMethodProfileData = loadedMethodProfileData.Concat(ReadMIbcGroup(tsc, (EcmaMethod)ilBody.GetObject((int)token)));
                            break;
                        case ILOpcode.pop:
                            mibcGroupName = "";
                            break;
                    }

                    // This isn't correct if there is a switch opcode, but since we won't do that, its ok
                    currentOffset += opcode.GetSize();
                }

                return new IBCProfileData(false, loadedMethodProfileData);
            }
        }

        enum MibcGroupParseState
        {
            LookingForNextMethod,
            LookingForOptionalData,
            ProcessingExclusiveWeight,
            ProcessingCallgraphCount,
            ProcessingCallgraphToken,
            ProcessingCallgraphWeight,
        }

        /// <summary>
        /// Parse MIbcGroup method and return enumerable of MethodProfileData
        ///
        /// Like the AssemblyDictionary method, data is encoded via IL instructions. The format is
        ///
        /// ldtoken methodInProfileData
        /// Any series of instructions that does not include pop. Expansion data is encoded via ldstr "id"
        /// followed by a expansion specific sequence of il opcodes.
        /// pop
        /// {Repeat N times for N methods described}
        ///
        /// Extensions supported with current parser:
        ///
        /// ldstr "ExclusiveWeight"
        /// Any ldc.i4 or ldc.r4 or ldc.r8 instruction to indicate the exclusive weight
        ///
        /// ldstr "WeightedCallData"
        /// ldc.i4 <Count of methods called>
        /// Repeat <Count of methods called times>
        ///  ldtoken <Method called from this method>
        ///  ldc.i4 <Weight associated with calling the <Method called from this method>>
        ///
        /// This format is designed to be extensible to hold more data as we add new per method profile data without breaking existing parsers.
        /// </summary>
        static IEnumerable<MethodProfileData> ReadMIbcGroup(TypeSystemContext tsc, EcmaMethod method)
        {
            EcmaMethodIL ilBody = EcmaMethodIL.Create(method);
            byte[] ilBytes = ilBody.GetILBytes();
            int currentOffset = 0;
            object methodInProgress = null;
            object metadataNotResolvable = new object();
            object metadataObject = null;
            MibcGroupParseState state = MibcGroupParseState.LookingForNextMethod;
            int intValue = 0;
            int weightedCallGraphSize = 0;
            int profileEntryFound = 0;
            double exclusiveWeight = 0;
            Dictionary<MethodDesc, int> weights = null;
            bool processIntValue = false;
            while (currentOffset < ilBytes.Length)
            {
                ILOpcode opcode = (ILOpcode)ilBytes[currentOffset];
                if (opcode == ILOpcode.prefix1)
                    opcode = 0x100 + (ILOpcode)ilBytes[currentOffset + 1];
                processIntValue = false;
                switch (opcode)
                {
                    case ILOpcode.ldtoken:
                        {
                            uint token = BitConverter.ToUInt32(ilBytes.AsSpan(currentOffset + 1, 4));
                            metadataObject = null;
                            try
                            {
                                metadataObject = ilBody.GetObject((int)token);
                            }
                            catch (TypeSystemException)
                            {
                                // The method being referred to may be missing. In that situation,
                                // use the metadataNotResolvable sentinel to indicate that this record should be ignored
                                metadataObject = metadataNotResolvable;
                            }
                            switch (state)
                            {
                                case MibcGroupParseState.ProcessingCallgraphToken:
                                    state = MibcGroupParseState.ProcessingCallgraphWeight;
                                    break;
                                case MibcGroupParseState.LookingForNextMethod:
                                    methodInProgress = metadataObject;
                                    state = MibcGroupParseState.LookingForOptionalData;
                                    break;
                                default:
                                    state = MibcGroupParseState.LookingForOptionalData;
                                    break;
                            }
                        }
                        break;

                    case ILOpcode.ldc_r4:
                        {
                            float fltValue = BitConverter.ToSingle(ilBytes.AsSpan(currentOffset + 1, 4));

                            switch (state)
                            {
                                case MibcGroupParseState.ProcessingExclusiveWeight:
                                    exclusiveWeight = fltValue;
                                    state = MibcGroupParseState.LookingForOptionalData;
                                    break;

                                default:
                                    state = MibcGroupParseState.LookingForOptionalData;
                                    break;
                            }

                            break;
                        }

                    case ILOpcode.ldc_r8:
                        {
                            double dblValue = BitConverter.ToDouble(ilBytes.AsSpan(currentOffset + 1, 8));

                            switch (state)
                            {
                                case MibcGroupParseState.ProcessingExclusiveWeight:
                                    exclusiveWeight = dblValue;
                                    state = MibcGroupParseState.LookingForOptionalData;
                                    break;

                                default:
                                    state = MibcGroupParseState.LookingForOptionalData;
                                    break;
                            }
                            break;
                        }
                    case ILOpcode.ldc_i4_0:
                        intValue = 0;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_1:
                        intValue = 1;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_2:
                        intValue = 2;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_3:
                        intValue = 3;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_4:
                        intValue = 4;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_5:
                        intValue = 5;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_6:
                        intValue = 6;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_7:
                        intValue = 7;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_8:
                        intValue = 8;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_m1:
                        intValue = -1;
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4_s:
                        intValue = (sbyte)ilBytes[currentOffset + 1];
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4:
                        intValue = BitConverter.ToInt32(ilBytes.AsSpan(currentOffset + 1, 4));
                        processIntValue = true;
                        break;

                    case ILOpcode.ldstr:
                        {
                            UInt32 userStringToken = BitConverter.ToUInt32(ilBytes.AsSpan(currentOffset + 1, 4));
                            string optionalDataName = (string)ilBody.GetObject((int)userStringToken);
                            switch (optionalDataName)
                            {
                                case "ExclusiveWeight":
                                    state = MibcGroupParseState.ProcessingExclusiveWeight;
                                    break;

                                case "WeightedCallData":
                                    state = MibcGroupParseState.ProcessingCallgraphCount;
                                    break;

                                default:
                                    state = MibcGroupParseState.LookingForOptionalData;
                                    break;
                            }

                        }
                        break;
                    case ILOpcode.pop:
                        if (methodInProgress != metadataNotResolvable)
                        {
                            profileEntryFound++;
                            if (exclusiveWeight == 0)
                            {
                                // If no exclusive weight is found assign a non zero value that assumes the order in the pgo file is significant.
                                exclusiveWeight = Math.Min(1000000.0 - profileEntryFound, 0.0) / 1000000.0;
                            }
                            MethodProfileData mibcData = new MethodProfileData((MethodDesc)methodInProgress, MethodProfilingDataFlags.ReadMethodCode, exclusiveWeight, weights, 0xFFFFFFFF);
                            state = MibcGroupParseState.LookingForNextMethod;
                            exclusiveWeight = 0;
                            weights = null;
                            yield return mibcData;
                        }
                        methodInProgress = null;
                        break;
                    default:
                        state = MibcGroupParseState.LookingForOptionalData;
                        break;
                }

                if (processIntValue)
                {
                    switch (state)
                    {
                        case MibcGroupParseState.ProcessingExclusiveWeight:
                            exclusiveWeight = intValue;
                            state = MibcGroupParseState.LookingForOptionalData;
                            break;

                        case MibcGroupParseState.ProcessingCallgraphCount:
                            weightedCallGraphSize = intValue;
                            weights = new Dictionary<MethodDesc, int>();
                            if (weightedCallGraphSize > 0)
                                state = MibcGroupParseState.ProcessingCallgraphToken;
                            else
                                state = MibcGroupParseState.LookingForOptionalData;
                            break;

                        case MibcGroupParseState.ProcessingCallgraphWeight:
                            if (metadataObject != metadataNotResolvable)
                            {
                                weights.Add((MethodDesc)metadataObject, intValue);
                            }
                            weightedCallGraphSize--;
                            if (weightedCallGraphSize > 0)
                                state = MibcGroupParseState.ProcessingCallgraphToken;
                            else
                                state = MibcGroupParseState.LookingForOptionalData;
                            break;
                        default:
                            state = MibcGroupParseState.LookingForOptionalData;
                            break;
                    }
                }

                // This isn't correct if there is a switch opcode, but since we won't do that, its ok
                currentOffset += opcode.GetSize();
            }
        }

        /// <summary>
        /// Use this implementation of IModuleResolver to provide a module resolver which overrides resolution of System.Private.Canon module to point to a module
        /// that can resolve the CanonTypes out of the core library as CanonType.
        /// </summary>
        class CustomCanonResolver : IModuleResolver
        {
            private class CanonModule : ModuleDesc, IAssemblyDesc
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
    }
}
