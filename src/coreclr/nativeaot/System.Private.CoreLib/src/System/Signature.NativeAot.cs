// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;

using Internal.Metadata.NativeFormat;

namespace System
{
    // NativeFormat counterpart of CoreCLR's Signature (RuntimeHandles.cs). The offset helpers are managed ports of the
    // Signature_* helpers in src/coreclr/vm/runtimehandles.cpp and should be kept in sync with them. Offsets are relative
    // to the start of the ECMA-335 II.23.2 signature blob, and -1 denotes a position whose signature is not available.
    internal sealed unsafe partial class Signature
    {
        private readonly RuntimeType[]? _arguments;
        private readonly RuntimeType _returnTypeORfieldType;
        private readonly void* _sig;
        private readonly int _csig;
        private readonly MetadataReader _reader;
        private readonly TypeContext _typeContext;

        internal Signature(MetadataReader reader, ByteCollection blob, TypeContext typeContext)
        {
            SigPointer sig = blob.GetSigPointer();
            _sig = sig._ptr;
            _csig = (int)sig._len;
            _reader = reader;
            _typeContext = typeContext;

            // Resolve the types eagerly, like Signature_Init in src/coreclr/vm/runtimehandles.cpp.
            SigParser sp = GetParser(0);
            if (sp.GetCallingConvInfo() == MdSigCallingConvention.Field)
            {
                _returnTypeORfieldType = ResolveType(ref sp);
            }
            else
            {
                sp = GetParser(0);
                sp.SkipMethodHeaderSignature(out int argCount, skipReturnType: false);
                _returnTypeORfieldType = ResolveType(ref sp);

                RuntimeType[] arguments = argCount == 0 ? [] : new RuntimeType[argCount];
                for (int i = 0; i < arguments.Length; i++)
                    arguments[i] = ResolveType(ref sp);
                _arguments = arguments;
            }
        }

        internal RuntimeType[] Arguments
        {
            get
            {
                Debug.Assert(_arguments != null);
                return _arguments;
            }
        }
        internal RuntimeType ReturnType => _returnTypeORfieldType;
        internal RuntimeType FieldType => _returnTypeORfieldType;

        private RuntimeType ResolveType(ref SigParser sp) => TypeResolver.Resolve(ref sp, _reader, _typeContext).ToType();

        internal int GetParameterOffset(int parameterIndex)
        {
            SigParser sp = GetParser(0);

            MdSigCallingConvention callConv = sp.GetCallingConvInfo();
            if ((callConv & MdSigCallingConvention.CallConvMask) != MdSigCallingConvention.Field)
            {
                if ((callConv & MdSigCallingConvention.Generic) != 0)
                    sp.GetData();

                int numArgs = sp.GetData();
                Debug.Assert((uint)parameterIndex <= (uint)numArgs);

                for (int i = 0; i < parameterIndex; i++)
                    sp.SkipExactlyOne();
            }
            else
            {
                Debug.Assert(parameterIndex == 0);
            }

            return GetOffset(sp);
        }

        internal int GetTypeParameterOffset(int offset, int index)
        {
            if (offset < 0)
            {
                Debug.Assert(offset == -1);
                return offset;
            }

            SigParser sp = GetParser(offset);

            int argCnt;
            switch (sp.GetElemType())
            {
                case CorElementType.ELEMENT_TYPE_FNPTR:
                    sp.SkipMethodHeaderSignature(out argCnt, skipReturnType: false);
                    Debug.Assert((uint)index <= (uint)argCnt);
                    break;
                case CorElementType.ELEMENT_TYPE_GENERICINST:
                    sp.SkipExactlyOne();

                    argCnt = sp.GetData();
                    Debug.Assert((uint)index < (uint)argCnt);
                    break;
                case CorElementType.ELEMENT_TYPE_ARRAY:
                case CorElementType.ELEMENT_TYPE_SZARRAY:
                case CorElementType.ELEMENT_TYPE_BYREF:
                case CorElementType.ELEMENT_TYPE_PTR:
                    Debug.Assert(index == 0);
                    break;
                case CorElementType.ELEMENT_TYPE_VAR:
                case CorElementType.ELEMENT_TYPE_MVAR:
                    // The signature of a substituted generic variable doesn't describe the substituted type's components.
                    return -1;
                default:
                    Debug.Fail("Unexpected element type");
                    return -1;
            }

            for (int i = 0; i < index; i++)
                sp.SkipExactlyOne();

            return GetOffset(sp);
        }

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointerAtOffset(int offset)
        {
            if (offset < 0)
            {
                Debug.Assert(offset == -1);
                return SignatureCallingConvention.Default;
            }

            SigParser sp = GetParser(offset);

            CorElementType elementType = sp.GetElemType();
            Debug.Assert(elementType == CorElementType.ELEMENT_TYPE_FNPTR);

            return (SignatureCallingConvention)sp.GetCallingConv();
        }

        internal Type[] GetCustomModifiers(int parameterIndex, bool isOptional) =>
            GetCustomModifiersAtOffset(GetParameterOffset(parameterIndex), isOptional);

        internal Type[] GetCustomModifiersAtOffset(int offset, bool isOptional)
        {
            ArrayBuilder<Type> customModifiers = default;

            SigParser sp = GetParser(offset);
            sp.SkipAnyVASentinel();
            while (sp.TryGetCustomModifier(out bool modifierIsOptional, out Handle modifierType))
            {
                if (isOptional == modifierIsOptional)
                    customModifiers.Add(modifierType.Resolve(_reader, _typeContext).ToType());
            }

            // Match CoreCLR's reverse metadata order for custom modifiers.
            customModifiers.AsSpan().Reverse();
            return customModifiers.ToArray();
        }

        private SigParser GetParser(int offset)
        {
            Debug.Assert((uint)offset < (uint)_csig);
            return new SigParser((byte*)_sig + offset, (uint)(_csig - offset));
        }

        private int GetOffset(SigParser sp) => (int)(sp.GetPtr() - (byte*)_sig);
    }
}
