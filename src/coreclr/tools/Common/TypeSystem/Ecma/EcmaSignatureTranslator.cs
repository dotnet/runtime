// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Internal.TypeSystem.Ecma
{
    public struct EcmaSignatureTranslator
    {
        BlobReader _input;
        BlobBuilder _output;

        Func<int, int> _getAlternateStreamToken;

        public EcmaSignatureTranslator(BlobReader input, BlobBuilder output, Func<int, int> getAlternateStreamToken)
        {
            _input = input;
            _output = output;
            _getAlternateStreamToken = getAlternateStreamToken;
        }

        // Various parsing functions for processing through the locals of a function and translating them to the
        // alternate form with new tokens.
        int ParseCompressedInt()
        {
            int value = _input.ReadCompressedInteger();
            _output.WriteCompressedInteger(value);
            return value;
        }

        int ParseCompressedSignedInt()
        {
            int value = _input.ReadCompressedSignedInteger();
            _output.WriteCompressedSignedInteger(value);
            return value;
        }

        byte ParseByte()
        {
            byte value = _input.ReadByte();
            _output.WriteByte(value);
            return value;
        }

        byte PeekByte()
        {
            byte value = _input.ReadByte();
            _input.Offset = _input.Offset - 1;
            return value;
        }

        public void ParseMemberRefSignature()
        {
            byte sigHeader = PeekByte();
            if (sigHeader == (byte)SignatureKind.Field)
                ParseFieldSignature();
            else
                ParseMethodSignature();
        }

        public void ParseFieldSignature()
        {
            byte sigHeader = ParseByte();
            if (sigHeader != (byte)SignatureKind.Field)
                throw new BadImageFormatException();
            ParseType();
        }

        public void ParseMethodSignature()
        {
            byte sigHeader = ParseByte();
            if (((int)sigHeader & (int)SignatureAttributes.Generic) == (int)SignatureAttributes.Generic)
            {
                // Parse arity
                ParseCompressedInt();
            }
            int argCount = ParseCompressedInt();
            for (int i = 0; i <= argCount; i++)
                ParseType();
        }

        public void ParseLocalsSignature()
        {
            byte sigHeader = ParseByte();
            if ((SignatureKind)sigHeader != SignatureKind.LocalVariables)
                throw new BadImageFormatException();

            int localsCount = ParseCompressedInt();
            for (int i = 0; i < localsCount; i++)
            {
                ParseType();
            }
        }

        public void ParseMethodSpecSignature()
        {
            byte sigHeader = ParseByte();
            if ((SignatureKind)sigHeader != SignatureKind.MethodSpecification)
            {
                throw new BadImageFormatException();
            }

            int argCount = ParseCompressedInt();
            for (int i = 0; i < argCount; i++)
            {
                ParseType();
            }
        }


        void ParseTypeHandle()
        {
            int token = MetadataTokens.GetToken(_input.ReadTypeHandle());
            int newToken = _getAlternateStreamToken(token);
            int newEncodedHandle = CodedIndex.TypeDefOrRefOrSpec(MetadataTokens.EntityHandle(newToken));
            _output.WriteCompressedInteger(newEncodedHandle);
        }

        public void ParseType()
        {
            SignatureTypeCode typeCode;
            for (; ; )
            {
                int sigcodeRaw = ParseCompressedInt();
                const int ELEMENT_TYPE_CLASS = 0x12;
                const int ELEMENT_TYPE_VALUETYPE = 0x11;
                if (sigcodeRaw == ELEMENT_TYPE_CLASS || sigcodeRaw == ELEMENT_TYPE_VALUETYPE)
                    typeCode = SignatureTypeCode.TypeHandle;
                else
                    typeCode = (SignatureTypeCode)sigcodeRaw;

                switch (typeCode)
                {
                    case SignatureTypeCode.RequiredModifier:
                    case SignatureTypeCode.OptionalModifier:
                        ParseTypeHandle();
                        continue;
                    case SignatureTypeCode.Pinned:
                    case SignatureTypeCode.Sentinel:
                        continue;
                }
                break;
            }

            switch (typeCode)
            {
                case SignatureTypeCode.Void:
                case SignatureTypeCode.Boolean:
                case SignatureTypeCode.SByte:
                case SignatureTypeCode.Byte:
                case SignatureTypeCode.Int16:
                case SignatureTypeCode.UInt16:
                case SignatureTypeCode.Int32:
                case SignatureTypeCode.UInt32:
                case SignatureTypeCode.Int64:
                case SignatureTypeCode.UInt64:
                case SignatureTypeCode.Single:
                case SignatureTypeCode.Double:
                case SignatureTypeCode.Char:
                case SignatureTypeCode.String:
                case SignatureTypeCode.IntPtr:
                case SignatureTypeCode.UIntPtr:
                case SignatureTypeCode.Object:
                case SignatureTypeCode.TypedReference:
                    break;

                case SignatureTypeCode.TypeHandle:
                    ParseTypeHandle();
                    break;
                case SignatureTypeCode.SZArray:
                case SignatureTypeCode.ByReference:
                case SignatureTypeCode.Pointer:
                    {
                        ParseType();
                        break;
                    }
                case SignatureTypeCode.Array:
                    {
                        ParseType();
                        var rank = ParseCompressedInt();

                        var boundsCount = ParseCompressedInt();
                        for (int i = 0; i < boundsCount; i++)
                            ParseCompressedInt();
                        var lowerBoundsCount = ParseCompressedInt();
                        for (int j = 0; j < lowerBoundsCount; j++)
                            ParseCompressedSignedInt();
                        break;
                    }
                case SignatureTypeCode.GenericTypeParameter:
                case SignatureTypeCode.GenericMethodParameter:
                    ParseCompressedInt();
                    break;
                case SignatureTypeCode.GenericTypeInstance:
                    {
                        ParseType();
                        int instanceLength = ParseCompressedInt();
                        for (int i = 0; i < instanceLength; i++)
                        {
                            ParseType();
                        }
                        break;
                    }

                case SignatureTypeCode.FunctionPointer:
                    ParseMethodSignature();
                    break;
                default:
                    ThrowHelper.ThrowBadImageFormatException();
                    return;
            }
        }
    }
}
