// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;

namespace System
{
    // NativeFormat counterpart of CoreCLR's Signature. Offsets are relative to the start of the ECMA-335 II.23.2
    // signature blob, and -1 denotes a position whose signature is not available.
    internal sealed class Signature
    {
        private readonly MetadataReader _reader;
        private readonly uint _sig;
        private readonly int _csig;
        private readonly TypeContext _typeContext;

        internal Signature(MetadataReader reader, ByteCollection blob, TypeContext typeContext)
        {
            TypeSignature start = new SignatureReader(reader, blob).Position;
            _reader = reader;
            _sig = start.Offset;
            _csig = (int)(start.EndOffset - start.Offset);
            _typeContext = typeContext;
        }

        internal int ArgumentCount
        {
            get
            {
                SignatureReader reader = GetReader(0);
                reader.ReadMethodHeader(out _, out int argumentCount);
                return argumentCount;
            }
        }

        // Index 0 is the return type of a method, the type of a property, or the type of a field.
        internal RuntimeTypeInfo GetParameterType(int parameterIndex) =>
            GetReader(GetParameterOffset(parameterIndex)).Position.Resolve(_typeContext);

        internal int GetParameterOffset(int parameterIndex)
        {
            SignatureReader reader = GetReader(0);

            MdSigCallingConvention callConv = reader.ReadCallingConventionInfo();
            if ((callConv & MdSigCallingConvention.CallConvMask) != MdSigCallingConvention.Field)
            {
                if ((callConv & MdSigCallingConvention.Generic) != 0)
                    reader.ReadCompressedInteger();

                int numArgs = reader.ReadCompressedInteger();
                Debug.Assert((uint)parameterIndex <= (uint)numArgs);

                for (int i = 0; i < parameterIndex; i++)
                    reader.SkipType();
            }
            else
            {
                Debug.Assert(parameterIndex == 0);
            }

            return GetOffset(reader);
        }

        internal int GetTypeParameterOffset(int offset, int index)
        {
            if (offset < 0)
            {
                Debug.Assert(offset == -1);
                return offset;
            }

            SignatureReader reader = GetReader(offset);

            int argCnt;
            switch (reader.ReadElementType())
            {
                case CorElementType.ELEMENT_TYPE_FNPTR:
                    reader.ReadMethodHeader(out _, out argCnt);
                    Debug.Assert((uint)index <= (uint)argCnt);
                    break;
                case CorElementType.ELEMENT_TYPE_GENERICINST:
                    reader.SkipType();

                    argCnt = reader.ReadCompressedInteger();
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
                reader.SkipType();

            return GetOffset(reader);
        }

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointerAtOffset(int offset)
        {
            if (offset < 0)
            {
                Debug.Assert(offset == -1);
                return SignatureCallingConvention.Default;
            }

            SignatureReader reader = GetReader(offset);

            CorElementType elementType = reader.ReadElementType();
            Debug.Assert(elementType == CorElementType.ELEMENT_TYPE_FNPTR);

            return (SignatureCallingConvention)(reader.ReadCallingConventionInfo() & MdSigCallingConvention.CallConvMask);
        }

        internal Type[] GetCustomModifiers(int parameterIndex, bool isOptional) =>
            GetCustomModifiersAtOffset(GetParameterOffset(parameterIndex), isOptional);

        // Always returns a freshly allocated array that is safe to return to API callers.
        internal Type[] GetCustomModifiersAtOffset(int offset, bool isOptional)
        {
            ArrayBuilder<Type> customModifiers = default;

            SignatureReader reader = GetReader(offset);
            reader.SkipAnyVASentinel();
            while (reader.TryReadCustomModifier(out bool modifierIsOptional, out Handle modifierType))
            {
                if (isOptional == modifierIsOptional)
                    customModifiers.Add(modifierType.Resolve(_reader, _typeContext).ToType());
            }

            // Match CoreCLR's reverse metadata order for custom modifiers.
            customModifiers.AsSpan().Reverse();
            return customModifiers.ToArray();
        }

        private SignatureReader GetReader(int offset)
        {
            Debug.Assert((uint)offset < (uint)_csig);
            return new SignatureReader(_reader, _sig + (uint)offset, _sig + (uint)_csig);
        }

        private int GetOffset(SignatureReader reader) => (int)(reader.Position.Offset - _sig);
    }
}
