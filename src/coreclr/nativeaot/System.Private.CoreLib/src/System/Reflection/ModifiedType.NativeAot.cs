// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Runtime.General;

using Internal.Metadata.NativeFormat;

using Debug = System.Diagnostics.Debug;
using NativeFormatTypeSignature = Internal.Metadata.NativeFormat.TypeSignature;

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        internal readonly struct TypeSignature
        {
            internal readonly NativeFormatTypeSignature Signature;
            public TypeSignature(NativeFormatTypeSignature signature)
                => Signature = signature;
        }

        internal Type GetTypeParameter(Type unmodifiedType, int index)
        {
            NativeFormatTypeSignature signature = _typeSignature.Signature;
            if (signature.Reader is null)
                return Create(unmodifiedType);

            signature = signature.SkipCustomModifiers();
            SignatureReader reader = signature.GetReader();
            switch ((SignatureTypeCode)reader.ReadByte())
            {
                case SignatureTypeCode.SZArray:
                case SignatureTypeCode.Array:
                case SignatureTypeCode.Pointer:
                case SignatureTypeCode.ByReference:
                    Debug.Assert(index == 0);
                    return Create(unmodifiedType, reader.Position);
                case SignatureTypeCode.FunctionPointer:
                {
                    reader.ReadMethodHeader(out _, out int count);
                    if (index-- == 0)
                        return Create(unmodifiedType, reader.Position);

                    Debug.Assert(index < count);
                    reader.SkipType();
                    for (int i = 0; i < count; i++)
                    {
                        if (reader.TypeCode == SignatureTypeCode.Sentinel)
                            reader.ReadByte();
                        if (index-- == 0)
                            return Create(unmodifiedType, reader.Position);
                        reader.SkipType();
                    }
                }
                break;
                case SignatureTypeCode.GenericTypeInstance:
                {
                    reader.SkipType();
                    int count = reader.ReadCompressedInteger();
                    Debug.Assert(index < count);
                    for (int i = 0; i < count; i++)
                    {
                        if (index-- == 0)
                            return Create(unmodifiedType, reader.Position);
                        reader.SkipType();
                    }
                }
                break;
            }

            Debug.Fail(signature.GetReader().TypeCode.ToString());
            return null;
        }

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointer()
        {
            NativeFormatTypeSignature signature = _typeSignature.Signature;
            if (signature.Reader is null)
                return default;

            SignatureReader reader = signature.SkipCustomModifiers().GetReader();
            Debug.Assert(reader.TypeCode == SignatureTypeCode.FunctionPointer);
            reader.ReadByte();
            var callingConvention = (Internal.Metadata.NativeFormat.SignatureCallingConvention)reader.ReadByte();

            Debug.Assert((int)Internal.Metadata.NativeFormat.SignatureCallingConvention.StdCall == (int)SignatureCallingConvention.StdCall);
            Debug.Assert((int)Internal.Metadata.NativeFormat.SignatureCallingConvention.Unmanaged == (int)SignatureCallingConvention.Unmanaged);
            return (SignatureCallingConvention)(callingConvention
                & Internal.Metadata.NativeFormat.SignatureCallingConvention.UnmanagedCallingConventionMask);
        }

        private Type[] GetCustomModifiers(bool isOptional)
        {
            NativeFormatTypeSignature signature = _typeSignature.Signature;
            if (signature.Reader is null)
                return [];

            return signature.GetCustomModifiers(new TypeContext(null, null), isOptional);
        }

        internal static Type Create(Type unmodifiedType)
            => Create(unmodifiedType, default(TypeSignature));

        public static Type Create(Type unmodifiedType, NativeFormatTypeSignature typeSignature)
            => ModifiedType.Create(unmodifiedType, new TypeSignature(typeSignature));
    }
}
