// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class BlobEncodersTests
    {
        [Fact]
        public void Ctors()
        {
            Assert.Throws<ArgumentNullException>(() => new BlobEncoder(null));
        }

        [Fact]
        public void BlobEncoder_FieldSignature()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            var s = e.FieldSignature();
            AssertEx.Equal(new byte[] { 0x06 }, b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void BlobEncoder_MethodSpecificationSignature()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            var s = e.MethodSpecificationSignature(genericArgumentCount: 0);
            AssertEx.Equal(new byte[] { 0x0A, 0x00 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            e.MethodSpecificationSignature(genericArgumentCount: 1234);
            AssertEx.Equal(new byte[] { 0x0A, 0x84, 0xD2 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.MethodSpecificationSignature(genericArgumentCount: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.MethodSpecificationSignature(genericArgumentCount: ushort.MaxValue + 1));
        }

        [Fact]
        public void BlobEncoder_MethodSignature()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            var s = e.MethodSignature();
            AssertEx.Equal(new byte[] { 0x00 }, b.ToArray());
            Assert.Same(b, s.Builder);
            Assert.False(s.HasVarArgs);
            b.Clear();

            s = e.MethodSignature(
                convention: SignatureCallingConvention.StdCall,
                genericParameterCount: 1234,
                isInstanceMethod: true);

            AssertEx.Equal(new byte[] { 0x32, 0x84, 0xD2 }, b.ToArray());
            Assert.False(s.HasVarArgs);
            b.Clear();

            s = e.MethodSignature(
                convention: SignatureCallingConvention.VarArgs,
                genericParameterCount: 1,
                isInstanceMethod: false);

            AssertEx.Equal(new byte[] { 0x15, 0x01 }, b.ToArray());
            Assert.True(s.HasVarArgs);
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.MethodSignature(genericParameterCount: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.MethodSignature(genericParameterCount: ushort.MaxValue + 1));
        }

        [Fact]
        public void BlobEncoder_PropertySignature()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            var s = e.PropertySignature();
            AssertEx.Equal(new byte[] { 0x08 }, b.ToArray());
            Assert.Same(b, s.Builder);
            Assert.False(s.HasVarArgs);
            b.Clear();

            s = e.PropertySignature(isInstanceProperty: true);
            AssertEx.Equal(new byte[] { 0x28 }, b.ToArray());
            Assert.False(s.HasVarArgs);
            b.Clear();
        }

        [Fact]
        public void BlobEncoder_CustomAttributeSignature()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            FixedArgumentsEncoder fixedArgs;
            CustomAttributeNamedArgumentsEncoder namedArgs;
            e.CustomAttributeSignature(out fixedArgs, out namedArgs);

            AssertEx.Equal(new byte[] { 0x01, 0x00 }, b.ToArray());
            Assert.Same(b, fixedArgs.Builder);
            Assert.Same(b, namedArgs.Builder);
            b.Clear();

            e.CustomAttributeSignature(
                f => Assert.Same(b, f.Builder),
                n => Assert.Same(b, namedArgs.Builder));

            AssertEx.Equal(new byte[] { 0x01, 0x00 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentNullException>(() => e.CustomAttributeSignature(null, n => { }));
            Assert.Throws<ArgumentNullException>(() => e.CustomAttributeSignature(f => { }, null));
        }

        [Fact]
        public void BlobEncoder_LocalVariableSignature()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            var s = e.LocalVariableSignature(variableCount: 0);
            AssertEx.Equal(new byte[] { 0x07, 0x00 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            s = e.LocalVariableSignature(variableCount: 1000000);
            AssertEx.Equal(new byte[] { 0x07, 0xC0, 0x0F, 0x42, 0x40 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.LocalVariableSignature(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.LocalVariableSignature(BlobWriterImpl.MaxCompressedIntegerValue + 1));
        }

        [Fact]
        public void BlobEncoder_TypeSpecificationSignature()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            var s = e.TypeSpecificationSignature();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void BlobEncoder_PermissionSetBlob()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            var s = e.PermissionSetBlob(attributeCount: 0);
            AssertEx.Equal(new byte[] { 0x2e, 0x00 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            s = e.PermissionSetBlob(attributeCount: 1000000);
            AssertEx.Equal(new byte[] { 0x2e, 0xC0, 0x0F, 0x42, 0x40 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.PermissionSetBlob(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.PermissionSetBlob(BlobWriterImpl.MaxCompressedIntegerValue + 1));
        }

        [Fact]
        public void BlobEncoder_PermissionSetArguments()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);

            var s = e.PermissionSetArguments(argumentCount: 0);
            AssertEx.Equal(new byte[] { 0x00 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            s = e.PermissionSetArguments(argumentCount: 1000000);
            AssertEx.Equal(new byte[] { 0xC0, 0x0F, 0x42, 0x40 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.PermissionSetArguments(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.PermissionSetArguments(BlobWriterImpl.MaxCompressedIntegerValue + 1));
        }

        [Fact]
        public void MethodSignatureEncoder_Parameters()
        {
            var b = new BlobBuilder();
            var e = new MethodSignatureEncoder(b, hasVarArgs: false);

            ReturnTypeEncoder returnType;
            ParametersEncoder parameters;
            e.Parameters(0, out returnType, out parameters);
            AssertEx.Equal(new byte[] { 0x00 }, b.ToArray());
            Assert.Same(b, parameters.Builder);
            Assert.Same(b, returnType.Builder);
            b.Clear();

            e.Parameters(1000000, out returnType, out parameters);
            AssertEx.Equal(new byte[] { 0xC0, 0x0F, 0x42, 0x40 }, b.ToArray());
            b.Clear();

            e.Parameters(10,
                rt => Assert.Same(b, rt.Builder),
                ps => Assert.Same(b, ps.Builder));
            AssertEx.Equal(new byte[] { 0x0A }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.Parameters(-1, out returnType, out parameters));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Parameters(BlobWriterImpl.MaxCompressedIntegerValue + 1, out returnType, out parameters));
            Assert.Throws<ArgumentNullException>(() => e.Parameters(0, null, ps => { }));
            Assert.Throws<ArgumentNullException>(() => e.Parameters(0, rt => { }, null));
        }

        [Fact]
        public void LocalVariablesEncoder_AddVariable()
        {
            var b = new BlobBuilder();
            var e = new LocalVariablesEncoder(b);

            var s = e.AddVariable();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void LocalVariableTypeEncoder_CustomModifiers()
        {
            var b = new BlobBuilder();
            var e = new LocalVariableTypeEncoder(b);

            var s = e.CustomModifiers();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void LocalVariableTypeEncoder_Type()
        {
            var b = new BlobBuilder();
            var e = new LocalVariableTypeEncoder(b);

            var s = e.Type();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            s = e.Type(isByRef: true);
            AssertEx.Equal(new byte[] { 0x10 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            s = e.Type(isPinned: true);
            AssertEx.Equal(new byte[] { 0x45 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            s = e.Type(isByRef: true, isPinned: true);
            AssertEx.Equal(new byte[] { 0x45, 0x10 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();
        }

        [Fact]
        public void LocalVariableTypeEncoder_TypedReference()
        {
            var b = new BlobBuilder();
            var e = new LocalVariableTypeEncoder(b);

            e.TypedReference();
            AssertEx.Equal(new byte[] { 0x16 }, b.ToArray());
        }

        [Fact]
        public void ParameterTypeEncoder_CustomModifiers()
        {
            var b = new BlobBuilder();
            var e = new ParameterTypeEncoder(b);

            var s = e.CustomModifiers();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void ParameterTypeEncoder_Type()
        {
            var b = new BlobBuilder();
            var e = new ParameterTypeEncoder(b);

            var s = e.Type();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            s = e.Type(isByRef: true);
            AssertEx.Equal(new byte[] { 0x10 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();
        }

        [Fact]
        public void ParameterTypeEncoder_TypedReference()
        {
            var b = new BlobBuilder();
            var e = new ParameterTypeEncoder(b);

            e.TypedReference();
            AssertEx.Equal(new byte[] { 0x16 }, b.ToArray());
        }

        [Fact]
        public void PermissionSetEncoder_AddPermission()
        {
            var b = new BlobBuilder();
            var e = new PermissionSetEncoder(b);

            var s = e.AddPermission("ABCD", ImmutableArray.Create<byte>(1, 2, 3));
            Assert.Same(b, s.Builder);
            AssertEx.Equal(new byte[] { 0x04, 0x41, 0x42, 0x43, 0x44, 0x03, 0x01, 0x02, 0x03 }, b.ToArray());
            b.Clear();

            var args = new BlobBuilder();
            args.WriteBytes(new byte[] { 1, 2, 3 });

            s = e.AddPermission("ABCD", args);
            Assert.Same(b, s.Builder);
            AssertEx.Equal(new byte[] { 0x04, 0x41, 0x42, 0x43, 0x44, 0x03, 0x01, 0x02, 0x03 }, b.ToArray());
            b.Clear();

            s = e.AddPermission("", ImmutableArray.Create<byte>());
            AssertEx.Equal(new byte[] { 0x00, 0x00 }, b.ToArray());
            b.Clear();

            s = e.AddPermission("", new BlobBuilder());
            AssertEx.Equal(new byte[] { 0x00, 0x00 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentNullException>(() => e.AddPermission(null, ImmutableArray.Create<byte>(1)));
            Assert.Throws<ArgumentNullException>(() => e.AddPermission(null, args));

            Assert.Throws<ArgumentNullException>(() => e.AddPermission("A", default(ImmutableArray<byte>)));
            Assert.Throws<ArgumentNullException>(() => e.AddPermission("A", null));
        }

        [Fact]
        public void GenericTypeArgumentsEncoder_AddArgument()
        {
            var b = new BlobBuilder();
            var e = new GenericTypeArgumentsEncoder(b);

            var s = e.AddArgument();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void FixedArgumentsEncoder_AddArgument()
        {
            var b = new BlobBuilder();
            var e = new FixedArgumentsEncoder(b);

            var s = e.AddArgument();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        // TODO:
        // LiteralEncoder
        // ScalarEncoder
        // LiteralsEncoder
        // VectorEncoder
        // NameEncoder
        // CustomAttributeNamedArgumentsEncoder
        // NamedArgumentsEncoder
        // NamedArgumentTypeEncoder
        // CustomAttributeArrayTypeEncoder
        // CustomAttributeElementTypeEncoder

        [Fact]
        public void SignatureTypeEncoder_Primitives()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.Boolean();
            AssertEx.Equal(new byte[] { 0x02 }, b.ToArray());
            b.Clear();

            e.Char();
            AssertEx.Equal(new byte[] { 0x03 }, b.ToArray());
            b.Clear();

            e.SByte();
            AssertEx.Equal(new byte[] { 0x04 }, b.ToArray());
            b.Clear();

            e.Byte();
            AssertEx.Equal(new byte[] { 0x05 }, b.ToArray());
            b.Clear();

            e.Int16();
            AssertEx.Equal(new byte[] { 0x06 }, b.ToArray());
            b.Clear();

            e.UInt16();
            AssertEx.Equal(new byte[] { 0x07 }, b.ToArray());
            b.Clear();

            e.Int32();
            AssertEx.Equal(new byte[] { 0x08 }, b.ToArray());
            b.Clear();

            e.UInt32();
            AssertEx.Equal(new byte[] { 0x09 }, b.ToArray());
            b.Clear();

            e.Int64();
            AssertEx.Equal(new byte[] { 0x0A }, b.ToArray());
            b.Clear();

            e.UInt64();
            AssertEx.Equal(new byte[] { 0x0B }, b.ToArray());
            b.Clear();

            e.Single();
            AssertEx.Equal(new byte[] { 0x0C }, b.ToArray());
            b.Clear();

            e.Double();
            AssertEx.Equal(new byte[] { 0x0D }, b.ToArray());
            b.Clear();

            e.String();
            AssertEx.Equal(new byte[] { 0x0E }, b.ToArray());
            b.Clear();

            e.IntPtr();
            AssertEx.Equal(new byte[] { 0x18 }, b.ToArray());
            b.Clear();

            e.UIntPtr();
            AssertEx.Equal(new byte[] { 0x19 }, b.ToArray());
            b.Clear();

            e.Object();
            AssertEx.Equal(new byte[] { 0x1C }, b.ToArray());
            b.Clear();
        }

        [Fact]
        public void SignatureTypeEncoder_PrimitiveType()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.PrimitiveType(PrimitiveTypeCode.Boolean);
            AssertEx.Equal(new byte[] { 0x02 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.Char);
            AssertEx.Equal(new byte[] { 0x03 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.SByte);
            AssertEx.Equal(new byte[] { 0x04 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.Byte);
            AssertEx.Equal(new byte[] { 0x05 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.Int16);
            AssertEx.Equal(new byte[] { 0x06 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.UInt16);
            AssertEx.Equal(new byte[] { 0x07 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.Int32);
            AssertEx.Equal(new byte[] { 0x08 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.UInt32);
            AssertEx.Equal(new byte[] { 0x09 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.Int64);
            AssertEx.Equal(new byte[] { 0x0A }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.UInt64);
            AssertEx.Equal(new byte[] { 0x0B }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.Single);
            AssertEx.Equal(new byte[] { 0x0C }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.Double);
            AssertEx.Equal(new byte[] { 0x0D }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.String);
            AssertEx.Equal(new byte[] { 0x0E }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.IntPtr);
            AssertEx.Equal(new byte[] { 0x18 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.UIntPtr);
            AssertEx.Equal(new byte[] { 0x19 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveTypeCode.Object);
            AssertEx.Equal(new byte[] { 0x1C }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.PrimitiveType(PrimitiveTypeCode.Void));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.PrimitiveType(PrimitiveTypeCode.TypedReference));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.PrimitiveType((PrimitiveTypeCode)255));
        }

        [Fact]
        public void SignatureTypeEncoder_Array()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);
            Assert.Same(b, e.Builder);

            SignatureTypeEncoder elementType;
            ArrayShapeEncoder arrayShape;
            e.Array(out elementType, out arrayShape);
            AssertEx.Equal(new byte[] { 0x14 }, b.ToArray());
            Assert.Same(b, elementType.Builder);
            Assert.Same(b, arrayShape.Builder);
            b.Clear();

            e.Array(
                t => Assert.Same(b, t.Builder),
                s => Assert.Same(b, s.Builder));
            AssertEx.Equal(new byte[] { 0x14 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentNullException>(() => e.Array(null, n => { }));
            Assert.Throws<ArgumentNullException>(() => e.Array(n => { }, null));
        }

        [Fact]
        public void SignatureTypeEncoder_Type()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            e.Type(MetadataTokens.TypeDefinitionHandle(1), isValueType: true);
            AssertEx.Equal(new byte[] { 0x11, 0x04 }, b.ToArray());
            b.Clear();

            e.Type(MetadataTokens.TypeDefinitionHandle(1), isValueType: false);
            AssertEx.Equal(new byte[] { 0x12, 0x04 }, b.ToArray());
            b.Clear();

            e.Type(MetadataTokens.TypeReferenceHandle(1), isValueType: false);
            AssertEx.Equal(new byte[] { 0x12, 0x05 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentException>(() => e.Type(MetadataTokens.TypeSpecificationHandle(1), isValueType: false));
            Assert.Throws<ArgumentException>(() => e.Type(default(EntityHandle), isValueType: false));
            Assert.Equal(0, b.Count);
        }

        [Fact]
        public void SignatureTypeEncoder_FunctionPointer()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            var m = e.FunctionPointer(
                SignatureCallingConvention.CDecl,
                FunctionPointerAttributes.HasThis,
                genericParameterCount: 1);

            Assert.Same(b, m.Builder);

            AssertEx.Equal(new byte[] { 0x1B, 0x21, 0x01 }, b.ToArray());
            b.Clear();

            e.FunctionPointer(
                SignatureCallingConvention.Default,
                FunctionPointerAttributes.HasExplicitThis,
                genericParameterCount: 0);

            AssertEx.Equal(new byte[] { 0x1B, 0x60 }, b.ToArray());
            b.Clear();

            e.FunctionPointer();

            AssertEx.Equal(new byte[] { 0x1B, 0x00 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentException>(() => e.FunctionPointer(0, (FunctionPointerAttributes)1000, genericParameterCount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.FunctionPointer(0, 0, genericParameterCount: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.FunctionPointer(0, 0, genericParameterCount: ushort.MaxValue + 1));
            Assert.Equal(0, b.Count);
        }

        [Fact]
        public void SignatureTypeEncoder_GenericInstantiation()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            var m = e.GenericInstantiation(MetadataTokens.TypeDefinitionHandle(1), 1, true);
            Assert.Same(b, m.Builder);
            AssertEx.Equal(new byte[] { 0x15, 0x11, 0x04, 0x01 }, b.ToArray());
            b.Clear();

            e.GenericInstantiation(MetadataTokens.TypeReferenceHandle(1), 1, true);
            AssertEx.Equal(new byte[] { 0x15, 0x11, 0x05, 0x01 }, b.ToArray());
            b.Clear();

            e.GenericInstantiation(MetadataTokens.TypeDefinitionHandle(1), 1000, false);
            AssertEx.Equal(new byte[] { 0x15, 0x12, 0x04, 0x83, 0xE8 }, b.ToArray());
            b.Clear();

            e.GenericInstantiation(MetadataTokens.TypeDefinitionHandle(1), ushort.MaxValue, false);
            AssertEx.Equal(new byte[] { 0x15, 0x12, 0x04, 0xC0, 0x00, 0xFF, 0xFF }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentException>(() => e.GenericInstantiation(MetadataTokens.TypeSpecificationHandle(1), 1, isValueType: false));
            Assert.Throws<ArgumentException>(() => e.GenericInstantiation(default(EntityHandle), 1, isValueType: false));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.GenericInstantiation(default(TypeDefinitionHandle), 0, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.GenericInstantiation(default(TypeDefinitionHandle), -1, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.GenericInstantiation(default(TypeDefinitionHandle), ushort.MaxValue + 1, true));
            Assert.Equal(0, b.Count);
        }

        [Fact]
        public void SignatureTypeEncoder_GenericMethodTypeParameter()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            e.GenericMethodTypeParameter(0);
            AssertEx.Equal(new byte[] { 0x1E, 0x00 }, b.ToArray());
            b.Clear();

            e.GenericMethodTypeParameter(ushort.MaxValue);
            AssertEx.Equal(new byte[] { 0x1E, 0xC0, 0x00, 0xFF, 0xFF }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.GenericMethodTypeParameter(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.GenericMethodTypeParameter(ushort.MaxValue + 1));
            Assert.Equal(0, b.Count);
        }

        [Fact]
        public void SignatureTypeEncoder_GenericTypeParameter()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            e.GenericTypeParameter(0);
            AssertEx.Equal(new byte[] { 0x13, 0x00 }, b.ToArray());
            b.Clear();

            e.GenericTypeParameter(ushort.MaxValue);
            AssertEx.Equal(new byte[] { 0x13, 0xC0, 0x00, 0xFF, 0xFF }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.GenericTypeParameter(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.GenericTypeParameter(ushort.MaxValue + 1));
            Assert.Equal(0, b.Count);
        }

        [Fact]
        public void SignatureTypeEncoder_Pointer()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            var p = e.Pointer();
            AssertEx.Equal(new byte[] { 0x0F }, b.ToArray());
            Assert.Same(b, p.Builder);
        }

        [Fact]
        public void SignatureTypeEncoder_VoidPointer()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            e.VoidPointer();
            AssertEx.Equal(new byte[] { 0x0F, 0x01 }, b.ToArray());
        }

        [Fact]
        public void SignatureTypeEncoder_SZArray()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            var a = e.SZArray();
            AssertEx.Equal(new byte[] { 0x1D }, b.ToArray());
            Assert.Same(b, a.Builder);
        }

        [Fact]
        public void SignatureTypeEncoder_CustomModifiers()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);

            var a = e.CustomModifiers();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, a.Builder);
        }

        // CustomModifiersEncoder
        // ArrayShapeEncoder
        // ReturnTypeEncoder
        // ParametersEncoder
    }
}
