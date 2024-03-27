// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This is a generated file - do not manually edit!

#pragma warning disable 649, SA1121, IDE0036, SA1129

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Internal.NativeFormat;
using Debug = System.Diagnostics.Debug;

namespace Internal.Metadata.NativeFormat
{
    internal static partial class MdBinaryReader
    {
        public static unsafe uint Read(this NativeReader reader, uint offset, out BooleanCollection values)
        {
            values = new BooleanCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(bool));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out CharCollection values)
        {
            values = new CharCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(char));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out ByteCollection values)
        {
            values = new ByteCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(byte));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out SByteCollection values)
        {
            values = new SByteCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(sbyte));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out Int16Collection values)
        {
            values = new Int16Collection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(short));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out UInt16Collection values)
        {
            values = new UInt16Collection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(ushort));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out Int32Collection values)
        {
            values = new Int32Collection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(int));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out UInt32Collection values)
        {
            values = new UInt32Collection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(uint));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out Int64Collection values)
        {
            values = new Int64Collection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(long));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out UInt64Collection values)
        {
            values = new UInt64Collection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(ulong));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out SingleCollection values)
        {
            values = new SingleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(float));
            return offset;
        } // Read

        public static unsafe uint Read(this NativeReader reader, uint offset, out DoubleCollection values)
        {
            values = new DoubleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            offset = checked(offset + count * sizeof(double));
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out AssemblyFlags value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (AssemblyFlags)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out AssemblyHashAlgorithm value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (AssemblyHashAlgorithm)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out CallingConventions value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (CallingConventions)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out SignatureCallingConvention value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (SignatureCallingConvention)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out EventAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (EventAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out FieldAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (FieldAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out GenericParameterAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (GenericParameterAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out GenericParameterKind value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (GenericParameterKind)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (MethodAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodImplAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (MethodImplAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodSemanticsAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (MethodSemanticsAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out NamedArgumentMemberKind value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (NamedArgumentMemberKind)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ParameterAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (ParameterAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out PInvokeAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (PInvokeAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out PropertyAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (PropertyAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeAttributes value)
        {
            uint ivalue;
            offset = reader.DecodeUnsigned(offset, out ivalue);
            value = (TypeAttributes)ivalue;
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out HandleCollection values)
        {
            values = new HandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ArraySignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ArraySignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ByReferenceSignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ByReferenceSignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantBooleanArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantBooleanArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantBooleanValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantBooleanValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantBoxedEnumValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantBoxedEnumValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantByteArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantByteArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantByteValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantByteValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantCharArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantCharArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantCharValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantCharValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantDoubleArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantDoubleArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantDoubleValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantDoubleValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantEnumArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantEnumArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantHandleArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantHandleArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantInt16ArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantInt16ArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantInt16ValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantInt16ValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantInt32ArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantInt32ArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantInt32ValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantInt32ValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantInt64ArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantInt64ArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantInt64ValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantInt64ValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantReferenceValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantReferenceValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantSByteArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantSByteArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantSByteValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantSByteValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantSingleArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantSingleArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantSingleValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantSingleValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantStringArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantStringArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantStringValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantStringValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantUInt16ArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantUInt16ArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantUInt16ValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantUInt16ValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantUInt32ArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantUInt32ArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantUInt32ValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantUInt32ValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantUInt64ArrayHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantUInt64ArrayHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ConstantUInt64ValueHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ConstantUInt64ValueHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out CustomAttributeHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new CustomAttributeHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out EventHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new EventHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out FieldHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new FieldHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out FieldSignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new FieldSignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out FunctionPointerSignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new FunctionPointerSignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out GenericParameterHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new GenericParameterHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MemberReferenceHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new MemberReferenceHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new MethodHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodInstantiationHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new MethodInstantiationHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodSemanticsHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new MethodSemanticsHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodSignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new MethodSignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodTypeVariableSignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new MethodTypeVariableSignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ModifiedTypeHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ModifiedTypeHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out NamedArgumentHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new NamedArgumentHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out NamespaceDefinitionHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new NamespaceDefinitionHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out NamespaceReferenceHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new NamespaceReferenceHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ParameterHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ParameterHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out PointerSignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new PointerSignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out PropertyHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new PropertyHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out PropertySignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new PropertySignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out QualifiedFieldHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new QualifiedFieldHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out QualifiedMethodHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new QualifiedMethodHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out SZArraySignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new SZArraySignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ScopeDefinitionHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ScopeDefinitionHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ScopeReferenceHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new ScopeReferenceHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeDefinitionHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new TypeDefinitionHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeForwarderHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new TypeForwarderHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeInstantiationSignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new TypeInstantiationSignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeReferenceHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new TypeReferenceHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeSpecificationHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new TypeSpecificationHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeVariableSignatureHandle handle)
        {
            uint value;
            offset = reader.DecodeUnsigned(offset, out value);
            handle = new TypeVariableSignatureHandle((int)value);
            handle._Validate();
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out NamedArgumentHandleCollection values)
        {
            values = new NamedArgumentHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodSemanticsHandleCollection values)
        {
            values = new MethodSemanticsHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out CustomAttributeHandleCollection values)
        {
            values = new CustomAttributeHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ParameterHandleCollection values)
        {
            values = new ParameterHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out GenericParameterHandleCollection values)
        {
            values = new GenericParameterHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeDefinitionHandleCollection values)
        {
            values = new TypeDefinitionHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out TypeForwarderHandleCollection values)
        {
            values = new TypeForwarderHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out NamespaceDefinitionHandleCollection values)
        {
            values = new NamespaceDefinitionHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out MethodHandleCollection values)
        {
            values = new MethodHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out FieldHandleCollection values)
        {
            values = new FieldHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out PropertyHandleCollection values)
        {
            values = new PropertyHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out EventHandleCollection values)
        {
            values = new EventHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read

        public static uint Read(this NativeReader reader, uint offset, out ScopeDefinitionHandleCollection values)
        {
            values = new ScopeDefinitionHandleCollection(reader, offset);
            uint count;
            offset = reader.DecodeUnsigned(offset, out count);
            for (uint i = 0; i < count; ++i)
            {
                offset = reader.SkipInteger(offset);
            }
            return offset;
        } // Read
    } // MdBinaryReader
} // Internal.Metadata.NativeFormat
