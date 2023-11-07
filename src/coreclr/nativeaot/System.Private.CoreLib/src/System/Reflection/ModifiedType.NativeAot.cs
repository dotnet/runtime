// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Runtime.General;

using Internal.Metadata.NativeFormat;

using Debug = System.Diagnostics.Debug;

namespace System.Reflection
{
    internal partial class ModifiedType
    {
        internal readonly struct TypeSignature
        {
            internal readonly MetadataReader Reader;
            internal readonly Handle Handle;
            public TypeSignature(MetadataReader reader, Handle handle)
                => (Reader, Handle) = (reader, handle);
        }

        internal Type GetTypeParameter(Type unmodifiedType, int index)
        {
            MetadataReader reader = _typeSignature.Reader;
            Handle handle = _typeSignature.Handle;

            while (handle.HandleType == HandleType.ModifiedType)
                handle = reader.GetModifiedType(handle.ToModifiedTypeHandle(reader)).Type;

            if (handle.HandleType == HandleType.TypeSpecification)
                handle = reader.GetTypeSpecification(handle.ToTypeSpecificationHandle(reader)).Signature;

            switch (handle.HandleType)
            {
                case HandleType.SZArraySignature:
                    Debug.Assert(index == 0);
                    return Create(unmodifiedType, new TypeSignature(reader, reader.GetSZArraySignature(handle.ToSZArraySignatureHandle(reader)).ElementType));
                case HandleType.ArraySignature:
                    Debug.Assert(index == 0);
                    return Create(unmodifiedType, new TypeSignature(reader, reader.GetArraySignature(handle.ToArraySignatureHandle(reader)).ElementType));
                case HandleType.PointerSignature:
                    Debug.Assert(index == 0);
                    return Create(unmodifiedType, new TypeSignature(reader, reader.GetPointerSignature(handle.ToPointerSignatureHandle(reader)).Type));
                case HandleType.ByReferenceSignature:
                    Debug.Assert(index == 0);
                    return Create(unmodifiedType, new TypeSignature(reader, reader.GetByReferenceSignature(handle.ToByReferenceSignatureHandle(reader)).Type));
                case HandleType.FunctionPointerSignature:
                    {
                        MethodSignature functionSig = reader.GetMethodSignature(
                            reader.GetFunctionPointerSignature(handle.ToFunctionPointerSignatureHandle(reader)).Signature);
                        if (index-- == 0)
                            return Create(unmodifiedType, new TypeSignature(reader, functionSig.ReturnType));

                        Debug.Assert(index <= functionSig.Parameters.Count);
                        foreach (Handle paramHandle in functionSig.Parameters)
                            if (index-- == 0)
                                return Create(unmodifiedType, new TypeSignature(reader, paramHandle));
                    }
                    break;
                case HandleType.TypeInstantiationSignature:
                    {
                        TypeInstantiationSignature typeInst =
                            reader.GetTypeInstantiationSignature(handle.ToTypeInstantiationSignatureHandle(reader));
                        Debug.Assert(index < typeInst.GenericTypeArguments.Count);
                        foreach (Handle paramHandle in typeInst.GenericTypeArguments)
                            if (index-- == 0)
                                return Create(unmodifiedType, new TypeSignature(reader, paramHandle));
                    }
                    break;
            }

            Debug.Fail(handle.HandleType.ToString());
            return null;
        }

        internal SignatureCallingConvention GetCallingConventionFromFunctionPointer()
        {
            MetadataReader reader = _typeSignature.Reader;
            Handle fnPtrTypeSigHandle = reader.GetTypeSpecification(
                _typeSignature.Handle.ToTypeSpecificationHandle(reader)).Signature;
            MethodSignatureHandle methodSigHandle = reader.GetFunctionPointerSignature(
                fnPtrTypeSigHandle.ToFunctionPointerSignatureHandle(reader)).Signature;

            Debug.Assert((int)Internal.Metadata.NativeFormat.SignatureCallingConvention.StdCall == (int)SignatureCallingConvention.StdCall);
            Debug.Assert((int)Internal.Metadata.NativeFormat.SignatureCallingConvention.Unmanaged == (int)SignatureCallingConvention.Unmanaged);
            return (SignatureCallingConvention)(reader.GetMethodSignature(methodSigHandle).CallingConvention
                & Internal.Metadata.NativeFormat.SignatureCallingConvention.UnmanagedCallingConventionMask);
        }

        private Type[] GetCustomModifiers(bool required)
        {
            ArrayBuilder<Type> builder = default;

            MetadataReader reader = _typeSignature.Reader;
            Handle handle = _typeSignature.Handle;

            while (handle.HandleType == HandleType.ModifiedType)
            {
                var modifiedType = reader.GetModifiedType(handle.ToModifiedTypeHandle(reader));

                handle = modifiedType.Type;

                if (modifiedType.IsOptional == required)
                    continue;

                builder.Add(modifiedType.ModifierType.Resolve(reader, new TypeContext(null, null)).ToType());
            }

            Type[] result = builder.ToArray();

            // We call Reverse for compat with CoreCLR that also reverses these.
            // ILDasm also reverses these but don't be fooled: you can go to
            // View -> MetaInfo -> Show to see the file format order in ILDasm.
            Array.Reverse(result);

            return result;
        }

        public static Type Create(Type unmodifiedType, MetadataReader reader, Handle typeSignature)
            => ModifiedType.Create(unmodifiedType, new TypeSignature(reader, typeSignature));
    }
}
