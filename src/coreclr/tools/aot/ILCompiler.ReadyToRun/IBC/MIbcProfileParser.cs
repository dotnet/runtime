// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Compression;
using System.Reflection;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.IL;
using Internal.Pgo;

using System.Linq;
using System.IO;
using System.Diagnostics;

using System.Reflection.PortableExecutable;

namespace ILCompiler.IBC
{
    static class MIbcProfileParser
    {
        private class MetadataLoaderForPgoData : IPgoSchemaDataLoader<TypeSystemEntityOrUnknown>
        {
            private readonly EcmaMethodIL _ilBody;

            public MetadataLoaderForPgoData(EcmaMethodIL ilBody)
            {
                _ilBody = ilBody;
            }
            TypeSystemEntityOrUnknown IPgoSchemaDataLoader<TypeSystemEntityOrUnknown>.TypeFromLong(long token)
            {
                try
                {
                    if (token == 0)
                        return new TypeSystemEntityOrUnknown(null);
                    if ((token & 0xFF000000) == 0)
                    {
                        // token type is 0, therefore it can't be a type
                        return new TypeSystemEntityOrUnknown((int)token);
                    }
                    TypeDesc foundType = _ilBody.GetObject((int)token, NotFoundBehavior.ReturnNull) as TypeDesc;
                    if (foundType == null)
                    {
                        return new TypeSystemEntityOrUnknown((int)token & 0x00FFFFFF);
                    }
                    return new TypeSystemEntityOrUnknown(foundType);
                }
                catch
                {
                    return new TypeSystemEntityOrUnknown((int)token);
                }
            }
        }

        public static PEReader OpenMibcAsPEReader(string filename)
        {
            byte[] peData = null;
            PEReader peReader = null;

            {
                FileStream fsMibcFile = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x1000, useAsync: false);
                bool disposeOnException = true;

                try
                {
                    byte firstByte = (byte)fsMibcFile.ReadByte();
                    byte secondByte = (byte)fsMibcFile.ReadByte();
                    fsMibcFile.Seek(0, SeekOrigin.Begin);
                    if (firstByte == 0x4d && secondByte == 0x5a)
                    {
                        // Uncompressed Mibc format, starts with 'MZ' prefix like all other PE files
                        peReader = new PEReader(fsMibcFile, PEStreamOptions.Default);
                        disposeOnException = false;
                    }
                    else
                    {
                        using (var zipFile = new ZipArchive(fsMibcFile, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null))
                        {
                            disposeOnException = false;
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
                    }
                }
                finally
                {
                    if (disposeOnException)
                        fsMibcFile.Dispose();
                }
            }

            if (peData != null)
            {
                peReader = new PEReader(System.Collections.Immutable.ImmutableArray.Create<byte>(peData));
            }

            return peReader;
        }

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
        public static ProfileData ParseMIbcFile(TypeSystemContext tsc, PEReader peReader, HashSet<string> assemblyNamesInVersionBubble, string onlyDefinedInAssembly)
        {
            var mibcModule = EcmaModule.Create(tsc, peReader, null, null, new CustomCanonResolver(tsc));

            var assemblyDictionary = (EcmaMethod)mibcModule.GetGlobalModuleType().GetMethod("AssemblyDictionary", null);
            IEnumerable<MethodProfileData> loadedMethodProfileData = Enumerable.Empty<MethodProfileData>();

            EcmaMethodIL ilBody = EcmaMethodIL.Create(assemblyDictionary);
            ILReader ilReader = new ILReader(ilBody.GetILBytes());

            string mibcGroupName = "";
            while (ilReader.HasNext)
            {
                ILOpcode opcode = ilReader.ReadILOpcode();
                switch (opcode)
                {
                    case ILOpcode.ldstr:
                        int userStringToken = ilReader.ReadILToken();
                        Debug.Assert(mibcGroupName == "");
                        if (mibcGroupName == "")
                        {
                            mibcGroupName = (string)ilBody.GetObject(userStringToken);
                        }
                        break;

                    case ILOpcode.ldtoken:
                        int token = ilReader.ReadILToken();

                        if (String.IsNullOrEmpty(mibcGroupName))
                            break;

                        string[] assembliesByName = mibcGroupName.Split(';');

                        bool hasMatchingDefinition = (onlyDefinedInAssembly == null) || assembliesByName[0].Equals(onlyDefinedInAssembly);

                        if (!hasMatchingDefinition)
                            break;

                        if (assemblyNamesInVersionBubble != null)
                        {
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
                        }

                        loadedMethodProfileData = loadedMethodProfileData.Concat(ReadMIbcGroup(tsc, (EcmaMethod)ilBody.GetObject(token)));
                        break;
                    case ILOpcode.pop:
                        mibcGroupName = "";
                        break;
                    default:
                        ilReader.Skip(opcode);
                        break;
                }
            }

            return new IBCProfileData(false, loadedMethodProfileData);
        }

        enum MibcGroupParseState
        {
            LookingForNextMethod,
            LookingForOptionalData,
            ProcessingExclusiveWeight,
            ProcessingCallgraphCount,
            ProcessingCallgraphToken,
            ProcessingCallgraphWeight,
            ProcessingInstrumentationData,
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
            MetadataLoaderForPgoData metadataLoader = new MetadataLoaderForPgoData(ilBody);
            ILReader ilReader = new ILReader(ilBody.GetILBytes());
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
            List<long> instrumentationDataLongs = null;
            PgoSchemaElem[] pgoSchemaData = null;

            while (ilReader.HasNext)
            {
                ILOpcode opcode = ilReader.ReadILOpcode();
                processIntValue = false;
                switch (opcode)
                {
                    case ILOpcode.ldtoken:
                        {
                            int token = ilReader.ReadILToken();
                            if (state == MibcGroupParseState.ProcessingInstrumentationData)
                            {
                                instrumentationDataLongs.Add(token);
                            }
                            else
                            {
                                metadataObject = null;
                                try
                                {
                                    metadataObject = ilBody.GetObject(token, NotFoundBehavior.ReturnNull);
                                    if (metadataObject == null)
                                        metadataObject = metadataNotResolvable;
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
                        }
                        break;

                    case ILOpcode.ldc_r4:
                        {
                            float fltValue = ilReader.ReadILFloat();

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
                            double dblValue = ilReader.ReadILDouble();

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
                        intValue = (sbyte)ilReader.ReadILByte();
                        processIntValue = true;
                        break;
                    case ILOpcode.ldc_i4:
                        intValue = (int)ilReader.ReadILUInt32();
                        processIntValue = true;
                        break;

                    case ILOpcode.ldc_i8:
                        if (state == MibcGroupParseState.ProcessingInstrumentationData)
                        {
                            instrumentationDataLongs.Add((long)ilReader.ReadILUInt64());
                        }
                        break;

                    case ILOpcode.ldstr:
                        {
                            int userStringToken = ilReader.ReadILToken();
                            string optionalDataName = (string)ilBody.GetObject(userStringToken);
                            switch (optionalDataName)
                            {
                                case "ExclusiveWeight":
                                    state = MibcGroupParseState.ProcessingExclusiveWeight;
                                    break;

                                case "WeightedCallData":
                                    state = MibcGroupParseState.ProcessingCallgraphCount;
                                    break;

                                case "InstrumentationDataStart":
                                    state = MibcGroupParseState.ProcessingInstrumentationData;
                                    instrumentationDataLongs = new List<long>();
                                    break;

                                case "InstrumentationDataEnd":
                                    if (instrumentationDataLongs != null)
                                    {
                                        instrumentationDataLongs.Add(2); // MarshalMask 2 (Type)
                                        instrumentationDataLongs.Add(0); // PgoInstrumentationKind.Done (0)
                                        pgoSchemaData = PgoProcessor.ParsePgoData<TypeSystemEntityOrUnknown>(metadataLoader, instrumentationDataLongs, false).ToArray();
                                    }
                                    state = MibcGroupParseState.LookingForOptionalData;
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
                            if (methodInProgress != null)
                            {
                                // If the method being loaded didn't have meaningful input, skip
                                MethodProfileData mibcData = new MethodProfileData((MethodDesc)methodInProgress, MethodProfilingDataFlags.ReadMethodCode, exclusiveWeight, weights, 0xFFFFFFFF, pgoSchemaData);
                                yield return mibcData;
                            }
                            state = MibcGroupParseState.LookingForNextMethod;
                            exclusiveWeight = 0;
                            weights = null;
                            instrumentationDataLongs = null;
                            pgoSchemaData = null;
                        }
                        methodInProgress = null;
                        break;
                    default:
                        state = MibcGroupParseState.LookingForOptionalData;
                        ilReader.Skip(opcode);
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
                        case MibcGroupParseState.ProcessingInstrumentationData:
                            instrumentationDataLongs.Add(intValue);
                            break;
                        default:
                            state = MibcGroupParseState.LookingForOptionalData;
                            instrumentationDataLongs = null;
                            break;
                    }
                }
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

                public override MetadataType GetType(string nameSpace, string name, NotFoundBehavior notFoundBehavior)
                {
                    TypeSystemContext context = Context;

                    if (context.SupportsCanon && (nameSpace == context.CanonType.Namespace) && (name == context.CanonType.Name))
                        return Context.CanonType;
                    if (context.SupportsUniversalCanon && (nameSpace == context.UniversalCanonType.Namespace) && (name == context.UniversalCanonType.Name))
                        return Context.UniversalCanonType;
                    else
                    {
                        if (notFoundBehavior != NotFoundBehavior.ReturnNull)
                        {
                            var failure = ResolutionFailure.GetTypeLoadResolutionFailure(nameSpace, name, "System.Private.Canon");
                            ModuleDesc.GetTypeResolutionFailure = failure;
                            if (notFoundBehavior == NotFoundBehavior.Throw)
                                failure.Throw();

                            return null;
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
