// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Reflection.Runtime.General;

using Internal.Runtime;
using Internal.Runtime.TypeLoader;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        public bool CompareMethodSignatures(RuntimeSignature signature1, RuntimeSignature signature2)
        {
            if (signature1.IsNativeLayoutSignature && signature2.IsNativeLayoutSignature)
            {
                if (signature1.StructuralEquals(signature2))
                    return true;

                NativeFormatModuleInfo module1 = ModuleList.GetModuleInfoByHandle(new TypeManagerHandle(signature1.ModuleHandle));
                NativeReader reader1 = GetNativeLayoutInfoReader(signature1);
                NativeParser parser1 = new NativeParser(reader1, signature1.NativeLayoutOffset);

                NativeFormatModuleInfo module2 = ModuleList.GetModuleInfoByHandle(new TypeManagerHandle(signature2.ModuleHandle));
                NativeReader reader2 = GetNativeLayoutInfoReader(signature2);
                NativeParser parser2 = new NativeParser(reader2, signature2.NativeLayoutOffset);

                return CompareMethodSigs(parser1, module1, parser2, module2);
            }
            else if (signature1.IsNativeLayoutSignature)
            {
                int token = signature2.Token;
                MetadataReader metadataReader = ModuleList.Instance.GetMetadataReaderForModule(new TypeManagerHandle(signature2.ModuleHandle));

                MethodSignatureComparer comparer = new MethodSignatureComparer(metadataReader, token.AsHandle().ToMethodHandle(metadataReader));
                return comparer.IsMatchingNativeLayoutMethodSignature(signature1);
            }
            else if (signature2.IsNativeLayoutSignature)
            {
                int token = signature1.Token;
                MetadataReader metadataReader = ModuleList.Instance.GetMetadataReaderForModule(new TypeManagerHandle(signature1.ModuleHandle));

                MethodSignatureComparer comparer = new MethodSignatureComparer(metadataReader, token.AsHandle().ToMethodHandle(metadataReader));
                return comparer.IsMatchingNativeLayoutMethodSignature(signature2);
            }
            else
            {
                // For now, RuntimeSignatures are only used to compare for method signature equality (along with their Name)
                // So we can implement this with the simple equals check
                if (signature1.Token != signature2.Token)
                    return false;

                if (signature1.ModuleHandle != signature2.ModuleHandle)
                    return false;

                return true;
            }
        }

        public uint GetGenericArgumentCountFromMethodNameAndSignature(MethodNameAndSignature signature)
        {
            if (signature.Signature.IsNativeLayoutSignature)
            {
                NativeReader reader = GetNativeLayoutInfoReader(signature.Signature);
                NativeParser parser = new NativeParser(reader, signature.Signature.NativeLayoutOffset);

                return GetGenericArgCountFromSig(parser);
            }
            else
            {
                ModuleInfo module = signature.Signature.GetModuleInfo();

#if ECMA_METADATA_SUPPORT
                if (module is NativeFormatModuleInfo)
#endif
                {
                    NativeFormatModuleInfo nativeFormatModule = (NativeFormatModuleInfo)module;
                    var metadataReader = nativeFormatModule.MetadataReader;
                    var methodHandle = signature.Signature.Token.AsHandle().ToMethodHandle(metadataReader);

                    var method = methodHandle.GetMethod(metadataReader);
                    var methodSignature = method.Signature.GetMethodSignature(metadataReader);
                    return checked((uint)methodSignature.GenericParameterCount);
                }
#if ECMA_METADATA_SUPPORT
                else
                {
                    EcmaModuleInfo ecmaModuleInfo = (EcmaModuleInfo)module;
                    var metadataReader = ecmaModuleInfo.MetadataReader;
                    var ecmaHandle = (System.Reflection.Metadata.MethodDefinitionHandle)System.Reflection.Metadata.Ecma335.MetadataTokens.Handle(signature.Signature.Token);
                    var method = metadataReader.GetMethodDefinition(ecmaHandle);
                    var blobHandle = method.Signature;
                    var blobReader = metadataReader.GetBlobReader(blobHandle);
                    byte sigByte = blobReader.ReadByte();
                    if ((sigByte & (byte)System.Reflection.Metadata.SignatureAttributes.Generic) == 0)
                        return 0;
                    uint genArgCount = checked((uint)blobReader.ReadCompressedInteger());
                    return genArgCount;
                }
#endif
            }
        }

        public bool TryGetMethodNameAndSignatureFromNativeLayoutSignature(RuntimeSignature signature, out MethodNameAndSignature nameAndSignature)
        {
            nameAndSignature = null;

            NativeReader reader = GetNativeLayoutInfoReader(signature);
            NativeParser parser = new NativeParser(reader, signature.NativeLayoutOffset);
            if (parser.IsNull)
                return false;

            RuntimeSignature methodSig;
            RuntimeSignature methodNameSig;
            nameAndSignature = GetMethodNameAndSignature(ref parser, new TypeManagerHandle(signature.ModuleHandle), out methodNameSig, out methodSig);

            return true;
        }

        public bool TryGetMethodNameAndSignaturePointersFromNativeLayoutSignature(TypeManagerHandle module, uint methodNameAndSigToken, out RuntimeSignature methodNameSig, out RuntimeSignature methodSig)
        {
            methodNameSig = default(RuntimeSignature);
            methodSig = default(RuntimeSignature);

            NativeReader reader = GetNativeLayoutInfoReader(module);
            NativeParser parser = new NativeParser(reader, methodNameAndSigToken);
            if (parser.IsNull)
                return false;

            methodNameSig = RuntimeSignature.CreateFromNativeLayoutSignature(module, parser.Offset);
            string methodName = parser.GetString();

            // Signatures are indirected to through a relative offset so that we don't have to parse them
            // when not comparing signatures (parsing them requires resolving types and is tremendously
            // expensive).
            NativeParser sigParser = parser.GetParserFromRelativeOffset();
            methodSig = RuntimeSignature.CreateFromNativeLayoutSignature(module, sigParser.Offset);

            return true;
        }

        public bool TryGetMethodNameAndSignatureFromNativeLayoutOffset(TypeManagerHandle moduleHandle, uint nativeLayoutOffset, out MethodNameAndSignature nameAndSignature)
        {
            nameAndSignature = null;

            NativeReader reader = GetNativeLayoutInfoReader(moduleHandle);
            NativeParser parser = new NativeParser(reader, nativeLayoutOffset);
            if (parser.IsNull)
                return false;

            RuntimeSignature methodSig;
            RuntimeSignature methodNameSig;
            nameAndSignature = GetMethodNameAndSignature(ref parser, moduleHandle, out methodNameSig, out methodSig);
            return true;
        }

        internal MethodNameAndSignature GetMethodNameAndSignature(ref NativeParser parser, TypeManagerHandle moduleHandle, out RuntimeSignature methodNameSig, out RuntimeSignature methodSig)
        {
            methodNameSig = RuntimeSignature.CreateFromNativeLayoutSignature(moduleHandle, parser.Offset);
            string methodName = parser.GetString();

            // Signatures are indirected to through a relative offset so that we don't have to parse them
            // when not comparing signatures (parsing them requires resolving types and is tremendously
            // expensive).
            NativeParser sigParser = parser.GetParserFromRelativeOffset();
            methodSig = RuntimeSignature.CreateFromNativeLayoutSignature(moduleHandle, sigParser.Offset);

            return new MethodNameAndSignature(methodName, methodSig);
        }

        internal bool IsStaticMethodSignature(RuntimeSignature methodSig)
        {
            if (methodSig.IsNativeLayoutSignature)
            {
                NativeReader reader = GetNativeLayoutInfoReader(methodSig);
                NativeParser parser = new NativeParser(reader, methodSig.NativeLayoutOffset);

                MethodCallingConvention callingConvention = (MethodCallingConvention)parser.GetUnsigned();
                return callingConvention.HasFlag(MethodCallingConvention.Static);
            }
            else
            {
                ModuleInfo module = methodSig.GetModuleInfo();

#if ECMA_METADATA_SUPPORT
                if (module is NativeFormatModuleInfo)
#endif
                {
                    NativeFormatModuleInfo nativeFormatModule = (NativeFormatModuleInfo)module;
                    var metadataReader = nativeFormatModule.MetadataReader;
                    var methodHandle = methodSig.Token.AsHandle().ToMethodHandle(metadataReader);

                    var method = methodHandle.GetMethod(metadataReader);
                    return (method.Flags & MethodAttributes.Static) != 0;
                }
#if ECMA_METADATA_SUPPORT
                else
                {
                    EcmaModuleInfo ecmaModuleInfo = (EcmaModuleInfo)module;
                    var metadataReader = ecmaModuleInfo.MetadataReader;
                    var ecmaHandle = (System.Reflection.Metadata.MethodDefinitionHandle)System.Reflection.Metadata.Ecma335.MetadataTokens.Handle(methodSig.Token);
                    var method = metadataReader.GetMethodDefinition(ecmaHandle);
                    var blobHandle = method.Signature;
                    var blobReader = metadataReader.GetBlobReader(blobHandle);
                    byte sigByte = blobReader.ReadByte();
                    return ((sigByte & (byte)System.Reflection.Metadata.SignatureAttributes.Instance) == 0);
                }
#endif
            }
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        // Create a TypeSystem.MethodSignature object from a RuntimeSignature that isn't a NativeLayoutSignature
        private TypeSystem.MethodSignature TypeSystemSigFromRuntimeSignature(TypeSystemContext context, RuntimeSignature signature)
        {
            Debug.Assert(!signature.IsNativeLayoutSignature);

            ModuleInfo module = signature.GetModuleInfo();

#if ECMA_METADATA_SUPPORT
            if (module is NativeFormatModuleInfo)
#endif
            {
                NativeFormatModuleInfo nativeFormatModule = (NativeFormatModuleInfo)module;
                var metadataReader = nativeFormatModule.MetadataReader;
                var methodHandle = signature.Token.AsHandle().ToMethodHandle(metadataReader);
                var metadataUnit = ((TypeLoaderTypeSystemContext)context).ResolveMetadataUnit(nativeFormatModule);
                var parser = new Internal.TypeSystem.NativeFormat.NativeFormatSignatureParser(metadataUnit, metadataReader.GetMethod(methodHandle).Signature, metadataReader);
                return parser.ParseMethodSignature();
            }
#if ECMA_METADATA_SUPPORT
            else
            {
                EcmaModuleInfo ecmaModuleInfo = (EcmaModuleInfo)module;
                TypeSystem.Ecma.EcmaModule ecmaModule = context.ResolveEcmaModule(ecmaModuleInfo);
                var ecmaHandle = System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(signature.Token);
                MethodDesc ecmaMethod = ecmaModule.GetMethod(ecmaHandle);
                return ecmaMethod.Signature;
            }
#endif
        }
#endif

        internal bool GetCallingConverterDataFromMethodSignature(TypeSystemContext context, RuntimeSignature methodSig, Instantiation typeInstantiation, Instantiation methodInstantiation, out bool hasThis, out TypeDesc[] parameters, out bool[] parametersWithGenericDependentLayout)
        {
            if (methodSig.IsNativeLayoutSignature)
                return GetCallingConverterDataFromMethodSignature_NativeLayout(context, methodSig, typeInstantiation, methodInstantiation, out hasThis, out parameters, out parametersWithGenericDependentLayout);
            else
            {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                var sig = TypeSystemSigFromRuntimeSignature(context, methodSig);
                return GetCallingConverterDataFromMethodSignature_MethodSignature(sig, typeInstantiation, methodInstantiation, out hasThis, out parameters, out parametersWithGenericDependentLayout);
#else
                parametersWithGenericDependentLayout = null;
                hasThis = false;
                parameters = null;
                return false;
#endif
            }
        }

        internal bool GetCallingConverterDataFromMethodSignature_NativeLayout(TypeSystemContext context, RuntimeSignature methodSig, Instantiation typeInstantiation, Instantiation methodInstantiation, out bool hasThis, out TypeDesc[] parameters, out bool[] parametersWithGenericDependentLayout)
        {
            return GetCallingConverterDataFromMethodSignature_NativeLayout_Common(
                context,
                methodSig,
                typeInstantiation,
                methodInstantiation,
                out hasThis,
                out parameters,
                out parametersWithGenericDependentLayout,
                null);
        }

        internal bool GetCallingConverterDataFromMethodSignature_NativeLayout_Common(
            TypeSystemContext context,
            RuntimeSignature methodSig,
            Instantiation typeInstantiation,
            Instantiation methodInstantiation,
            out bool hasThis,
            out TypeDesc[] parameters,
            out bool[] parametersWithGenericDependentLayout,
            NativeReader nativeReader)
        {
            hasThis = false;
            parameters = null;

            NativeLayoutInfoLoadContext nativeLayoutContext = new NativeLayoutInfoLoadContext();

            nativeLayoutContext._module = (NativeFormatModuleInfo)methodSig.GetModuleInfo();
            nativeLayoutContext._typeSystemContext = context;
            nativeLayoutContext._typeArgumentHandles = typeInstantiation;
            nativeLayoutContext._methodArgumentHandles = methodInstantiation;

            NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoByHandle(new TypeManagerHandle(methodSig.ModuleHandle));
            NativeReader reader = GetNativeLayoutInfoReader(methodSig);
            NativeParser parser = new NativeParser(reader, methodSig.NativeLayoutOffset);

            MethodCallingConvention callingConvention = (MethodCallingConvention)parser.GetUnsigned();
            hasThis = !callingConvention.HasFlag(MethodCallingConvention.Static);

            uint numGenArgs = callingConvention.HasFlag(MethodCallingConvention.Generic) ? parser.GetUnsigned() : 0;

            uint parameterCount = parser.GetUnsigned();
            parameters = new TypeDesc[parameterCount + 1];
            parametersWithGenericDependentLayout = new bool[parameterCount + 1];

            // One extra parameter to account for the return type
            for (uint i = 0; i <= parameterCount; i++)
            {
                // NativeParser is a struct, so it can be copied.
                NativeParser parserCopy = parser;

                // Parse the signature twice. The first time to find out the exact type of the signature
                // The second time to identify if the parameter loaded via the signature should be forced to be
                // passed byref as part of the universal generic calling convention.
                parameters[i] = GetConstructedTypeFromParserAndNativeLayoutContext(ref parser, nativeLayoutContext);
                parametersWithGenericDependentLayout[i] = TypeSignatureHasVarsNeedingCallingConventionConverter(ref parserCopy, module, context, HasVarsInvestigationLevel.Parameter);
                if (parameters[i] == null)
                    return false;
            }

            return true;
        }

#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
        private static bool GetCallingConverterDataFromMethodSignature_MethodSignature(TypeSystem.MethodSignature methodSignature, Instantiation typeInstantiation, Instantiation methodInstantiation, out bool hasThis, out TypeDesc[] parameters, out bool[] parametersWithGenericDependentLayout)
        {
            // Compute parameters dependent on generic instantiation for their layout
            parametersWithGenericDependentLayout = new bool[methodSignature.Length + 1];
            parametersWithGenericDependentLayout[0] = UniversalGenericParameterLayout.IsLayoutDependentOnGenericInstantiation(methodSignature.ReturnType);
            for (int i = 0; i < methodSignature.Length; i++)
            {
                parametersWithGenericDependentLayout[i + 1] = UniversalGenericParameterLayout.IsLayoutDependentOnGenericInstantiation(methodSignature[i]);
            }

            // Compute hasThis-ness
            hasThis = !methodSignature.IsStatic;

            // Compute parameter exact types
            parameters = new TypeDesc[methodSignature.Length + 1];

            parameters[0] = methodSignature.ReturnType.InstantiateSignature(typeInstantiation, methodInstantiation);
            for (int i = 0; i < methodSignature.Length; i++)
            {
                parameters[i + 1] = methodSignature[i].InstantiateSignature(typeInstantiation, methodInstantiation);
            }

            return true;
        }
#endif

        internal bool MethodSignatureHasVarsNeedingCallingConventionConverter(TypeSystemContext context, RuntimeSignature methodSig)
        {
            if (methodSig.IsNativeLayoutSignature)
                return MethodSignatureHasVarsNeedingCallingConventionConverter_NativeLayout(context, methodSig);
            else
            {
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
                var sig = TypeSystemSigFromRuntimeSignature(context, methodSig);
                return UniversalGenericParameterLayout.MethodSignatureHasVarsNeedingCallingConventionConverter(sig);
#else
                Environment.FailFast("Cannot parse signature");
                return false;
#endif
            }
        }

        private bool MethodSignatureHasVarsNeedingCallingConventionConverter_NativeLayout(TypeSystemContext context, RuntimeSignature methodSig)
        {
            NativeReader reader = GetNativeLayoutInfoReader(methodSig);
            NativeParser parser = new NativeParser(reader, methodSig.NativeLayoutOffset);
            NativeFormatModuleInfo module = ModuleList.Instance.GetModuleInfoByHandle(new TypeManagerHandle(methodSig.ModuleHandle));

            MethodCallingConvention callingConvention = (MethodCallingConvention)parser.GetUnsigned();
            uint numGenArgs = callingConvention.HasFlag(MethodCallingConvention.Generic) ? parser.GetUnsigned() : 0;
            uint parameterCount = parser.GetUnsigned();

            // Check the return type of the method
            if (TypeSignatureHasVarsNeedingCallingConventionConverter(ref parser, module, context, HasVarsInvestigationLevel.Parameter))
                return true;

            // Check the parameters of the method
            for (uint i = 0; i < parameterCount; i++)
            {
                if (TypeSignatureHasVarsNeedingCallingConventionConverter(ref parser, module, context, HasVarsInvestigationLevel.Parameter))
                    return true;
            }

            return false;
        }

        #region Private Helpers
        private enum HasVarsInvestigationLevel
        {
            Parameter,
            NotParameter,
            Ignore
        }

        /// <summary>
        /// IF THESE SEMANTICS EVER CHANGE UPDATE THE LOGIC WHICH DEFINES THIS BEHAVIOR IN
        /// THE DYNAMIC TYPE LOADER AS WELL AS THE COMPILER.
        /// (There is a version of this in UniversalGenericParameterLayout.cs that must be kept in sync with this.)
        ///
        /// Parameter's are considered to have type layout dependent on their generic instantiation
        /// if the type of the parameter in its signature is a type variable, or if the type is a generic
        /// structure which meets 2 characteristics:
        /// 1. Structure size/layout is affected by the size/layout of one or more of its generic parameters
        /// 2. One or more of the generic parameters is a type variable, or a generic structure which also recursively
        ///    would satisfy constraint 2. (Note, that in the recursion case, whether or not the structure is affected
        ///    by the size/layout of its generic parameters is not investigated.)
        ///
        /// Examples parameter types, and behavior.
        ///
        /// T = true
        /// List[T] = false
        /// StructNotDependentOnArgsForSize[T] = false
        /// GenStructDependencyOnArgsForSize[T] = true
        /// StructNotDependentOnArgsForSize[GenStructDependencyOnArgsForSize[T]] = true
        /// StructNotDependentOnArgsForSize[GenStructDependencyOnArgsForSize[List[T]]]] = false
        ///
        /// Example non-parameter type behavior
        /// T = true
        /// List[T] = false
        /// StructNotDependentOnArgsForSize[T] = *true*
        /// GenStructDependencyOnArgsForSize[T] = true
        /// StructNotDependentOnArgsForSize[GenStructDependencyOnArgsForSize[T]] = true
        /// StructNotDependentOnArgsForSize[GenStructDependencyOnArgsForSize[List[T]]]] = false
        /// </summary>
        private bool TypeSignatureHasVarsNeedingCallingConventionConverter(ref NativeParser parser, NativeFormatModuleInfo moduleHandle, TypeSystemContext context, HasVarsInvestigationLevel investigationLevel)
        {
            uint data;
            var kind = parser.GetTypeSignatureKind(out data);

            switch (kind)
            {
                case TypeSignatureKind.External: return false;
                case TypeSignatureKind.Variable: return true;
                case TypeSignatureKind.BuiltIn: return false;

                case TypeSignatureKind.Lookback:
                    {
                        var lookbackParser = parser.GetLookbackParser(data);
                        return TypeSignatureHasVarsNeedingCallingConventionConverter(ref lookbackParser, moduleHandle, context, investigationLevel);
                    }

                case TypeSignatureKind.Instantiation:
                    {
                        RuntimeTypeHandle genericTypeDef;
                        if (!TryGetTypeFromSimpleTypeSignature(ref parser, moduleHandle, out genericTypeDef))
                        {
                            Debug.Assert(false);
                            return true;    // Returning true will prevent further reading from the native parser
                        }

                        if (!RuntimeAugments.IsValueType(genericTypeDef))
                        {
                            // Reference types are treated like pointers. No calling convention conversion needed. Just consume the rest of the signature.
                            for (uint i = 0; i < data; i++)
                                TypeSignatureHasVarsNeedingCallingConventionConverter(ref parser, moduleHandle, context, HasVarsInvestigationLevel.Ignore);
                            return false;
                        }
                        else
                        {
                            bool result = false;
                            for (uint i = 0; i < data; i++)
                                result = TypeSignatureHasVarsNeedingCallingConventionConverter(ref parser, moduleHandle, context, HasVarsInvestigationLevel.NotParameter) || result;

                            if ((result == true) && (investigationLevel == HasVarsInvestigationLevel.Parameter))
                            {
                                if (!TryComputeHasInstantiationDeterminedSize(genericTypeDef, context, out result))
                                    Environment.FailFast("Unable to setup calling convention converter correctly");

                                return result;
                            }

                            return result;
                        }
                    }

                case TypeSignatureKind.Modifier:
                    {
                        // Arrays, pointers and byref types signatures are treated as pointers, not requiring calling convention conversion.
                        // Just consume the parameter type from the stream and return false;
                        TypeSignatureHasVarsNeedingCallingConventionConverter(ref parser, moduleHandle, context, HasVarsInvestigationLevel.Ignore);
                        return false;
                    }

                case TypeSignatureKind.MultiDimArray:
                    {
                        // No need for a calling convention converter for this case. Just consume the signature from the stream.

                        TypeSignatureHasVarsNeedingCallingConventionConverter(ref parser, moduleHandle, context, HasVarsInvestigationLevel.Ignore);

                        uint boundCount = parser.GetUnsigned();
                        for (uint i = 0; i < boundCount; i++)
                            parser.GetUnsigned();

                        uint lowerBoundCount = parser.GetUnsigned();
                        for (uint i = 0; i < lowerBoundCount; i++)
                            parser.GetUnsigned();
                    }
                    return false;

                case TypeSignatureKind.FunctionPointer:
                    {
                        // No need for a calling convention converter for this case. Just consume the signature from the stream.

                        uint argCount = parser.GetUnsigned();
                        for (uint i = 0; i < argCount; i++)
                            TypeSignatureHasVarsNeedingCallingConventionConverter(ref parser, moduleHandle, context, HasVarsInvestigationLevel.Ignore);
                    }
                    return false;

                default:
                    parser.ThrowBadImageFormatException();
                    return true;
            }
        }

        private bool TryGetTypeFromSimpleTypeSignature(ref NativeParser parser, NativeFormatModuleInfo moduleHandle, out RuntimeTypeHandle typeHandle)
        {
            uint data;
            TypeSignatureKind kind = parser.GetTypeSignatureKind(out data);

            if (kind == TypeSignatureKind.Lookback)
            {
                var lookbackParser = parser.GetLookbackParser(data);
                return TryGetTypeFromSimpleTypeSignature(ref lookbackParser, moduleHandle, out typeHandle);
            }
            else if (kind == TypeSignatureKind.External)
            {
                typeHandle = GetExternalTypeHandle(moduleHandle, data);
                return true;
            }
            else if (kind == TypeSignatureKind.BuiltIn)
            {
                typeHandle = ((WellKnownType)data).GetRuntimeTypeHandle();
                return true;
            }

            // Not a simple type signature... requires more work to skip
            typeHandle = default(RuntimeTypeHandle);
            return false;
        }

        private RuntimeTypeHandle GetExternalTypeHandle(NativeFormatModuleInfo moduleHandle, uint typeIndex)
        {
            Debug.Assert(moduleHandle != null);

            RuntimeTypeHandle result;

            TypeSystemContext context = TypeSystemContextFactory.Create();
            {
                NativeLayoutInfoLoadContext nativeLayoutContext = new NativeLayoutInfoLoadContext();
                nativeLayoutContext._module = moduleHandle;
                nativeLayoutContext._typeSystemContext = context;

                TypeDesc type = nativeLayoutContext.GetExternalType(typeIndex);
                result = type.RuntimeTypeHandle;
            }
            TypeSystemContextFactory.Recycle(context);

            Debug.Assert(!result.IsNull());
            return result;
        }

        private uint GetGenericArgCountFromSig(NativeParser parser)
        {
            MethodCallingConvention callingConvention = (MethodCallingConvention)parser.GetUnsigned();

            if ((callingConvention & MethodCallingConvention.Generic) == MethodCallingConvention.Generic)
            {
                return parser.GetUnsigned();
            }
            else
            {
                return 0;
            }
        }

        private bool CompareMethodSigs(NativeParser parser1, NativeFormatModuleInfo moduleHandle1, NativeParser parser2, NativeFormatModuleInfo moduleHandle2)
        {
            MethodCallingConvention callingConvention1 = (MethodCallingConvention)parser1.GetUnsigned();
            MethodCallingConvention callingConvention2 = (MethodCallingConvention)parser2.GetUnsigned();

            if (callingConvention1 != callingConvention2)
                return false;

            if ((callingConvention1 & MethodCallingConvention.Generic) == MethodCallingConvention.Generic)
            {
                if (parser1.GetUnsigned() != parser2.GetUnsigned())
                    return false;
            }

            uint parameterCount1 = parser1.GetUnsigned();
            uint parameterCount2 = parser2.GetUnsigned();
            if (parameterCount1 != parameterCount2)
                return false;

            // Compare one extra parameter to account for the return type
            for (uint i = 0; i <= parameterCount1; i++)
            {
                if (!CompareTypeSigs(ref parser1, moduleHandle1, ref parser2, moduleHandle2))
                    return false;
            }

            return true;
        }

        private bool CompareTypeSigs(ref NativeParser parser1, NativeFormatModuleInfo moduleHandle1, ref NativeParser parser2, NativeFormatModuleInfo moduleHandle2)
        {
            // startOffset lets us backtrack to the TypeSignatureKind for external types since the TypeLoader
            // expects to read it in.
            uint data1;
            uint startOffset1 = parser1.Offset;
            var typeSignatureKind1 = parser1.GetTypeSignatureKind(out data1);

            // If the parser is at a lookback type, get a new parser for it and recurse.
            // Since we haven't read the element type of parser2 yet, we just pass it in unchanged
            if (typeSignatureKind1 == TypeSignatureKind.Lookback)
            {
                NativeParser lookbackParser1 = parser1.GetLookbackParser(data1);
                return CompareTypeSigs(ref lookbackParser1, moduleHandle1, ref parser2, moduleHandle2);
            }

            uint data2;
            uint startOffset2 = parser2.Offset;
            var typeSignatureKind2 = parser2.GetTypeSignatureKind(out data2);

            // If parser2 is a lookback type, we need to rewind parser1 to its startOffset1
            // before recursing.
            if (typeSignatureKind2 == TypeSignatureKind.Lookback)
            {
                NativeParser lookbackParser2 = parser2.GetLookbackParser(data2);
                parser1 = new NativeParser(parser1.Reader, startOffset1);
                return CompareTypeSigs(ref parser1, moduleHandle1, ref lookbackParser2, moduleHandle2);
            }

            if (typeSignatureKind1 != typeSignatureKind2)
                return false;

            switch (typeSignatureKind1)
            {
                case TypeSignatureKind.Lookback:
                    {
                        //  Recursion above better have removed all lookbacks
                        Debug.Fail("Unexpected lookback type");
                        return false;
                    }

                case TypeSignatureKind.Modifier:
                    {
                        // Ensure the modifier kind (vector, pointer, byref) is the same
                        if (data1 != data2)
                            return false;
                        return CompareTypeSigs(ref parser1, moduleHandle1, ref parser2, moduleHandle2);
                    }

                case TypeSignatureKind.Variable:
                    {
                        // variable index is in data
                        if (data1 != data2)
                            return false;
                        break;
                    }

                case TypeSignatureKind.MultiDimArray:
                    {
                        // rank is in data
                        if (data1 != data2)
                            return false;

                        if (!CompareTypeSigs(ref parser1, moduleHandle1, ref parser2, moduleHandle2))
                            return false;

                        uint boundCount1 = parser1.GetUnsigned();
                        uint boundCount2 = parser2.GetUnsigned();
                        if (boundCount1 != boundCount2)
                            return false;

                        for (uint i = 0; i < boundCount1; i++)
                        {
                            if (parser1.GetUnsigned() != parser2.GetUnsigned())
                                return false;
                        }

                        uint lowerBoundCount1 = parser1.GetUnsigned();
                        uint lowerBoundCount2 = parser2.GetUnsigned();
                        if (lowerBoundCount1 != lowerBoundCount2)
                            return false;

                        for (uint i = 0; i < lowerBoundCount1; i++)
                        {
                            if (parser1.GetUnsigned() != parser2.GetUnsigned())
                                return false;
                        }
                        break;
                    }

                case TypeSignatureKind.FunctionPointer:
                    {
                        // callingConvention is in data
                        if (data1 != data2)
                            return false;
                        uint argCount1 = parser1.GetUnsigned();
                        uint argCount2 = parser2.GetUnsigned();
                        if (argCount1 != argCount2)
                            return false;
                        for (uint i = 0; i < argCount1; i++)
                        {
                            if (!CompareTypeSigs(ref parser1, moduleHandle1, ref parser2, moduleHandle2))
                                return false;
                        }
                        break;
                    }

                case TypeSignatureKind.Instantiation:
                    {
                        // Type parameter count is in data
                        if (data1 != data2)
                            return false;

                        if (!CompareTypeSigs(ref parser1, moduleHandle1, ref parser2, moduleHandle2))
                            return false;

                        for (uint i = 0; i < data1; i++)
                        {
                            if (!CompareTypeSigs(ref parser1, moduleHandle1, ref parser2, moduleHandle2))
                                return false;
                        }
                        break;
                    }

                case TypeSignatureKind.BuiltIn:
                    RuntimeTypeHandle typeHandle3 = ((WellKnownType)data1).GetRuntimeTypeHandle();
                    RuntimeTypeHandle typeHandle4 = ((WellKnownType)data2).GetRuntimeTypeHandle();
                    if (!typeHandle3.Equals(typeHandle4))
                        return false;

                    break;

                case TypeSignatureKind.External:
                    {
                        RuntimeTypeHandle typeHandle1 = GetExternalTypeHandle(moduleHandle1, data1);
                        RuntimeTypeHandle typeHandle2 = GetExternalTypeHandle(moduleHandle2, data2);
                        if (!typeHandle1.Equals(typeHandle2))
                            return false;

                        break;
                    }

                default:
                    return false;
            }
            return true;
        }
        #endregion
    }
}
