// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.TypeLoading.Ecma
{
    internal readonly struct EcmaMethodDecoder : IMethodDecoder
    {
        private readonly MethodDefinitionHandle _handle;
        private readonly EcmaModule _module;

        internal EcmaMethodDecoder(MethodDefinitionHandle handle, EcmaModule module)
            : this()
        {
            _handle = handle;
            _module = module;
            _neverAccessThisExceptThroughMethodDefinitionProperty = handle.GetMethodDefinition(Reader);
        }

        public RoModule GetRoModule() => _module;

        public string ComputeName() => MethodDefinition.Name.GetString(Reader);
        public int MetadataToken => _handle.GetToken();
        public IEnumerable<CustomAttributeData> ComputeTrueCustomAttributes() => MethodDefinition.GetCustomAttributes().ToTrueCustomAttributes(_module);

        public int ComputeGenericParameterCount() => MethodDefinition.GetGenericParameters().Count;
        public RoType[] ComputeGenericArgumentsOrParameters()
        {
            GenericParameterHandleCollection gphs = MethodDefinition.GetGenericParameters();
            int count = gphs.Count;
            if (count == 0)
                return Array.Empty<RoType>();

            RoType[] gps = new RoType[count];
            foreach (GenericParameterHandle gph in gphs)
            {
                RoType gp = gph.ResolveGenericParameter(_module);
                gps[gp.GenericParameterPosition] = gp;
            }
            return gps;
        }

        public MethodAttributes ComputeAttributes() => MethodDefinition.Attributes;

        public CallingConventions ComputeCallingConvention()
        {
            BlobReader signatureBlob = MethodDefinition.Signature.GetBlobReader(Reader);
            SignatureHeader sigHeader = signatureBlob.ReadSignatureHeader();

            CallingConventions result;
            if (sigHeader.CallingConvention == SignatureCallingConvention.VarArgs)
                result = CallingConventions.VarArgs;
            else
                result = CallingConventions.Standard;

            if (sigHeader.IsInstance)
                result |= CallingConventions.HasThis;

            if (sigHeader.HasExplicitThis)
                result |= CallingConventions.ExplicitThis;

            return result;
        }

        public MethodImplAttributes ComputeMethodImplementationFlags() => MethodDefinition.ImplAttributes;

        public MethodSig<RoParameter> SpecializeMethodSig(IRoMethodBase roMethodBase)
        {
            MetadataReader reader = Reader;
            MethodDefinition methodDefiniton = MethodDefinition;
            MethodSignature<RoType> sig = methodDefiniton.DecodeSignature(_module, roMethodBase.TypeContext);
            int numParameters = sig.RequiredParameterCount;
            MethodSig<RoParameter> methodSig = new MethodSig<RoParameter>(numParameters);
            foreach (ParameterHandle ph in methodDefiniton.GetParameters())
            {
                Parameter p = ph.GetParameter(reader);
                int position = p.SequenceNumber - 1;
                Type parameterType = position == -1 ? sig.ReturnType : sig.ParameterTypes[position];
                methodSig[position] = new EcmaFatMethodParameter(roMethodBase, position, parameterType, ph);
            }

            for (int position = -1; position < numParameters; position++)
            {
                Type parameterType = position == -1 ? sig.ReturnType : sig.ParameterTypes[position];
                if (methodSig[position] == null)
                    methodSig[position] = new RoThinMethodParameter(roMethodBase, position, parameterType);
            }

            return methodSig;
        }

        public MethodSig<RoType> SpecializeCustomModifiers(in TypeContext typeContext)
        {
            MethodSignature<RoType> sig = MethodDefinition.DecodeSignature(new EcmaModifiedTypeProvider(_module), typeContext);
            int numParameters = sig.RequiredParameterCount;
            MethodSig<RoType> methodSig = new MethodSig<RoType>(numParameters);
            for (int position = -1; position < numParameters; position++)
            {
                RoType roType = position == -1 ? sig.ReturnType : sig.ParameterTypes[position];
                methodSig[position] = roType;
            }
            return methodSig;
        }

        public MethodSig<string> SpecializeMethodSigStrings(in TypeContext typeContext)
        {
            ISignatureTypeProvider<string, TypeContext> typeProvider = EcmaSignatureTypeProviderForToString.Instance;
            MethodSignature<string> sig = MethodDefinition.DecodeSignature(typeProvider, typeContext);
            int parameterCount = sig.ParameterTypes.Length;
            MethodSig<string> results = new MethodSig<string>(parameterCount);
            results[-1] = sig.ReturnType;
            for (int position = 0; position < parameterCount; position++)
            {
                results[position] = sig.ParameterTypes[position];
            }
            return results;
        }

        public MethodBody SpecializeMethodBody(IRoMethodBase owner)
        {
            int rva = MethodDefinition.RelativeVirtualAddress;
            if (rva == 0)
                return null;
            return new EcmaMethodBody(owner, ((EcmaModule)(owner.MethodBase.Module)).PEReader.GetMethodBody(rva));
        }

        public DllImportAttribute ComputeDllImportAttribute()
        {
            MetadataReader reader = Reader;
            MethodImport mi = MethodDefinition.GetImport();
            string libraryName = mi.Module.GetModuleReference(reader).Name.GetString(reader);
            string entryPointName = mi.Name.GetString(reader);
            MethodImportAttributes a = mi.Attributes;

            CharSet charSet;
            switch (a & MethodImportAttributes.CharSetMask)
            {
                case MethodImportAttributes.CharSetAnsi: charSet = CharSet.Ansi; break;
                case MethodImportAttributes.CharSetAuto: charSet = CharSet.Auto; break;
                case MethodImportAttributes.CharSetUnicode: charSet = CharSet.Unicode; break;
                default:
                    charSet = CharSet.None; break; // Note: CharSet.None is actually the typical case, not an error case.
            }

            CallingConvention callConv;
            switch (a & MethodImportAttributes.CallingConventionMask)
            {
                case MethodImportAttributes.CallingConventionCDecl: callConv = CallingConvention.Cdecl; break;
                case MethodImportAttributes.CallingConventionFastCall: callConv = CallingConvention.FastCall; break;
                case MethodImportAttributes.CallingConventionStdCall: callConv = CallingConvention.StdCall; break;
                case MethodImportAttributes.CallingConventionThisCall: callConv = CallingConvention.ThisCall; break;
                case MethodImportAttributes.CallingConventionWinApi: callConv = CallingConvention.Winapi; break;
                default:
                    throw new BadImageFormatException();
            }

            return new DllImportAttribute(libraryName)
            {
                EntryPoint = entryPointName,
                ExactSpelling = (a & MethodImportAttributes.ExactSpelling) != 0,
                CharSet = charSet,
                CallingConvention = callConv,
                PreserveSig = (ComputeMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0,
                SetLastError = (a & MethodImportAttributes.SetLastError) != 0,
                BestFitMapping = (a & MethodImportAttributes.BestFitMappingMask) == MethodImportAttributes.BestFitMappingEnable,
                ThrowOnUnmappableChar = (a & MethodImportAttributes.ThrowOnUnmappableCharMask) == MethodImportAttributes.ThrowOnUnmappableCharEnable,
            };
        }

        private MetadataReader Reader => _module.Reader;
        private MetadataLoadContext Loader => GetRoModule().Loader;

        private MethodDefinition MethodDefinition { get { Loader.DisposeCheck(); return _neverAccessThisExceptThroughMethodDefinitionProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly MethodDefinition _neverAccessThisExceptThroughMethodDefinitionProperty;
    }
}
