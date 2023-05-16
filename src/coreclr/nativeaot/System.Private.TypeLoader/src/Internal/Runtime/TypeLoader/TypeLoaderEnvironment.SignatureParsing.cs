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

                NativeFormatModuleInfo module1 = ModuleList.Instance.GetModuleInfoByHandle(new TypeManagerHandle(signature1.ModuleHandle));
                NativeReader reader1 = GetNativeLayoutInfoReader(signature1);
                NativeParser parser1 = new NativeParser(reader1, signature1.NativeLayoutOffset);

                NativeFormatModuleInfo module2 = ModuleList.Instance.GetModuleInfoByHandle(new TypeManagerHandle(signature2.ModuleHandle));
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

        public static bool IsStaticMethodSignature(MethodNameAndSignature signature)
        {
            Debug.Assert(signature.Signature.IsNativeLayoutSignature);
            NativeReader reader = GetNativeLayoutInfoReader(signature.Signature);
            NativeParser parser = new NativeParser(reader, signature.Signature.NativeLayoutOffset);

            MethodCallingConvention callingConvention = (MethodCallingConvention)parser.GetUnsigned();
            return (callingConvention & MethodCallingConvention.Static) != 0;
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
                NativeFormatModuleInfo nativeFormatModule = (NativeFormatModuleInfo)module;
                var metadataReader = nativeFormatModule.MetadataReader;
                var methodHandle = signature.Signature.Token.AsHandle().ToMethodHandle(metadataReader);

                var method = methodHandle.GetMethod(metadataReader);
                var methodSignature = method.Signature.GetMethodSignature(metadataReader);
                return checked((uint)methodSignature.GenericParameterCount);
            }
        }

        public bool TryGetMethodNameAndSignatureFromNativeLayoutSignature(RuntimeSignature signature, out MethodNameAndSignature nameAndSignature)
        {
            nameAndSignature = null;

            NativeReader reader = GetNativeLayoutInfoReader(signature);
            NativeParser parser = new NativeParser(reader, signature.NativeLayoutOffset);
            if (parser.IsNull)
                return false;

            nameAndSignature = GetMethodNameAndSignature(ref parser, new TypeManagerHandle(signature.ModuleHandle), out _, out _);

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
            parser.SkipString(); // methodName

            // Signatures are indirected to through a relative offset so that we don't have to parse them
            // when not comparing signatures (parsing them requires resolving types and is tremendously
            // expensive).
            NativeParser sigParser = parser.GetParserFromRelativeOffset();
            methodSig = RuntimeSignature.CreateFromNativeLayoutSignature(module, sigParser.Offset);

            return true;
        }

        public MethodNameAndSignature GetMethodNameAndSignatureFromNativeLayoutOffset(TypeManagerHandle moduleHandle, uint nativeLayoutOffset)
        {
            NativeReader reader = GetNativeLayoutInfoReader(moduleHandle);
            NativeParser parser = new NativeParser(reader, nativeLayoutOffset);
            return GetMethodNameAndSignature(ref parser, moduleHandle, out _, out _);
        }

        internal static MethodNameAndSignature GetMethodNameAndSignature(ref NativeParser parser, TypeManagerHandle moduleHandle, out RuntimeSignature methodNameSig, out RuntimeSignature methodSig)
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

        #region Private Helpers

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

        private static RuntimeTypeHandle GetExternalTypeHandle(NativeFormatModuleInfo moduleHandle, uint typeIndex)
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

        private static uint GetGenericArgCountFromSig(NativeParser parser)
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
