// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            Assert.Same(b, e.Builder);

            var s = e.FieldSignature();
            AssertEx.Equal(new byte[] { 0x06 }, b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void BlobEncoder_Field()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);
            Assert.Same(b, e.Builder);

            var f = e.Field();
            f.CustomModifiers()
                .AddModifier(MetadataTokens.TypeDefinitionHandle(1), isOptional: true)
                .AddModifier(MetadataTokens.TypeDefinitionHandle(2), isOptional: false);
            f.Type(isByRef: true).Object();
            AssertEx.Equal(new byte[] { 0x06, 0x20, 0x04, 0x1F, 0x08, 0x10, 0x1C }, b.ToArray());

            b.Clear();
            f = e.Field();
            f.CustomModifiers()
                .AddModifier(MetadataTokens.TypeDefinitionHandle(1), isOptional: true)
                .AddModifier(MetadataTokens.TypeDefinitionHandle(2), isOptional: false);
            f.Type().Int32();
            AssertEx.Equal(new byte[] { 0x06, 0x20, 0x04, 0x1F, 0x08, 0x08 }, b.ToArray());
        }

        [Fact]
        public void BlobEncoder_MethodSpecificationSignature()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

            var s = e.TypeSpecificationSignature();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void BlobEncoder_PermissionSetBlob()
        {
            var b = new BlobBuilder();
            var e = new BlobEncoder(b);
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

            var s = e.AddVariable();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void LocalVariableTypeEncoder_CustomModifiers()
        {
            var b = new BlobBuilder();
            var e = new LocalVariableTypeEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.CustomModifiers();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void LocalVariableTypeEncoder_Type()
        {
            var b = new BlobBuilder();
            var e = new LocalVariableTypeEncoder(b);
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

            e.TypedReference();
            AssertEx.Equal(new byte[] { 0x16 }, b.ToArray());
        }

        [Fact]
        public void ParameterTypeEncoder_CustomModifiers()
        {
            var b = new BlobBuilder();
            var e = new ParameterTypeEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.CustomModifiers();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void ParameterTypeEncoder_Type()
        {
            var b = new BlobBuilder();
            var e = new ParameterTypeEncoder(b);
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

            e.TypedReference();
            AssertEx.Equal(new byte[] { 0x16 }, b.ToArray());
        }

        [Fact]
        public void PermissionSetEncoder_AddPermission()
        {
            var b = new BlobBuilder();
            var e = new PermissionSetEncoder(b);
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

            var s = e.AddArgument();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void FixedArgumentsEncoder_AddArgument()
        {
            var b = new BlobBuilder();
            var e = new FixedArgumentsEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.AddArgument();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void LiteralEncoder_Vector()
        {
            var b = new BlobBuilder();
            var e = new LiteralEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.Vector();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void LiteralEncoder_TaggedVector()
        {
            var b = new BlobBuilder();
            var e = new LiteralEncoder(b);
            Assert.Same(b, e.Builder);

            CustomAttributeArrayTypeEncoder arrayType;
            VectorEncoder vector;
            e.TaggedVector(out arrayType, out vector);

            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, arrayType.Builder);
            Assert.Same(b, vector.Builder);
            b.Clear();

            e.TaggedVector(
                at => Assert.Same(b, at.Builder),
                v => Assert.Same(b, v.Builder));

            Assert.Throws<ArgumentNullException>(() => e.TaggedVector(null, v => { }));
            Assert.Throws<ArgumentNullException>(() => e.TaggedVector(at => { }, null));
        }

        [Fact]
        public void LiteralEncoder_Scalar()
        {
            var b = new BlobBuilder();
            var e = new LiteralEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.Scalar();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void LiteralEncoder_TaggedScalar()
        {
            var b = new BlobBuilder();
            var e = new LiteralEncoder(b);
            Assert.Same(b, e.Builder);

            CustomAttributeElementTypeEncoder elementType;
            ScalarEncoder scalar;
            e.TaggedScalar(out elementType, out scalar);

            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, elementType.Builder);
            Assert.Same(b, scalar.Builder);
            b.Clear();

            e.TaggedScalar(
                et => Assert.Same(b, et.Builder),
                s => Assert.Same(b, s.Builder));

            Assert.Throws<ArgumentNullException>(() => e.TaggedScalar(null, s => { }));
            Assert.Throws<ArgumentNullException>(() => e.TaggedScalar(et => { }, null));
        }

        [Fact]
        public void ScalarEncoder_NullArray()
        {
            var b = new BlobBuilder();
            var e = new ScalarEncoder(b);
            Assert.Same(b, e.Builder);

            e.NullArray();
            AssertEx.Equal(new byte[] { 0xff, 0xff, 0xff, 0xff }, b.ToArray());
        }

        [Fact]
        public void ScalarEncoder_Constant()
        {
            var b = new BlobBuilder();
            var e = new ScalarEncoder(b);
            Assert.Same(b, e.Builder);

            e.Constant(null);
            AssertEx.Equal(new byte[] { 0xff }, b.ToArray());
            b.Clear();

            e.Constant("");
            AssertEx.Equal(new byte[] { 0x00 }, b.ToArray());
            b.Clear();

            e.Constant("abc");
            AssertEx.Equal(new byte[] { 0x03, 0x61, 0x62, 0x63 }, b.ToArray());
            b.Clear();

            e.Constant("\ud800"); // unpaired surrogate
            AssertEx.Equal(new byte[] { 0x03, 0xED, 0xA0, 0x80 }, b.ToArray());
            b.Clear();

            e.Constant(true);
            AssertEx.Equal(new byte[] { 0x01 }, b.ToArray());
            b.Clear();

            e.Constant(HandleKind.UserString);
            AssertEx.Equal(new byte[] { 0x70 }, b.ToArray());
            b.Clear();

            e.Constant((byte)0xAB);
            AssertEx.Equal(new byte[] { 0xAB }, b.ToArray());
            b.Clear();

            e.Constant((sbyte)0x12);
            AssertEx.Equal(new byte[] { 0x12 }, b.ToArray());
            b.Clear();

            e.Constant((ushort)0xABCD);
            AssertEx.Equal(new byte[] { 0xCD, 0xAB }, b.ToArray());
            b.Clear();

            e.Constant((short)0x1234);
            AssertEx.Equal(new byte[] { 0x34, 0x12 }, b.ToArray());
            b.Clear();

            e.Constant((char)0xABCD);
            AssertEx.Equal(new byte[] { 0xCD, 0xAB }, b.ToArray());
            b.Clear();

            e.Constant(0xABCD);
            AssertEx.Equal(new byte[] { 0xCD, 0xAB, 0x00, 0x00 }, b.ToArray());
            b.Clear();

            e.Constant((uint)0xABCD);
            AssertEx.Equal(new byte[] { 0xCD, 0xAB, 0x00, 0x00 }, b.ToArray());
            b.Clear();

            e.Constant(0x1122334455667788);
            AssertEx.Equal(new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 }, b.ToArray());
            b.Clear();

            e.Constant(0xAABBCCDDEEFF1122);
            AssertEx.Equal(new byte[] { 0x22, 0x11, 0xFF, 0xEE, 0xDD, 0xCC, 0xBB, 0xAA }, b.ToArray());
            b.Clear();

            e.Constant(0.1f);
            AssertEx.Equal(new byte[] { 0xCD, 0xCC, 0xCC, 0x3D }, b.ToArray());
            b.Clear();

            e.Constant(0.1);
            AssertEx.Equal(new byte[] { 0x9A, 0x99, 0x99, 0x99, 0x99, 0x99, 0xB9, 0x3F }, b.ToArray());
            b.Clear();
        }

        [Fact]
        public void ScalarEncoder_Type()
        {
            var b = new BlobBuilder();
            var e = new ScalarEncoder(b);
            Assert.Same(b, e.Builder);

            e.SystemType(null);
            AssertEx.Equal(new byte[] { 0xff }, b.ToArray());
            b.Clear();

            e.SystemType("abc");
            AssertEx.Equal(new byte[] { 0x03, 0x61, 0x62, 0x63 }, b.ToArray());
            b.Clear();

            e.SystemType("\ud800"); // unpaired surrogate
            AssertEx.Equal(new byte[] { 0x03, 0xED, 0xA0, 0x80 }, b.ToArray());
            b.Clear();

            AssertExtensions.Throws<ArgumentException>("serializedTypeName", () => e.SystemType(""));
        }

        [Fact]
        public void LiteralsEncoder_Scalar()
        {
            var b = new BlobBuilder();
            var e = new LiteralsEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.AddLiteral();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void VectorEncoder_Count()
        {
            var b = new BlobBuilder();
            var e = new VectorEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.Count(0);
            AssertEx.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            s = e.Count(int.MaxValue);
            AssertEx.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.Count(-1));
        }

        [Fact]
        public void NameEncoder_Name()
        {
            var b = new BlobBuilder();
            var e = new NameEncoder(b);
            Assert.Same(b, e.Builder);

            e.Name("abc");
            AssertEx.Equal(new byte[] { 0x03, 0x61, 0x62, 0x63 }, b.ToArray());
            b.Clear();

            e.Name("\ud800"); // unpaired surrogate
            AssertEx.Equal(new byte[] { 0x03, 0xED, 0xA0, 0x80 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentNullException>(() => e.Name(null));
            AssertExtensions.Throws<ArgumentException>("name", () => e.Name(""));
        }

        [Fact]
        public void CustomAttributeNamedArgumentsEncoder_Count()
        {
            var b = new BlobBuilder();
            var e = new CustomAttributeNamedArgumentsEncoder(b);
            Assert.Same(b, e.Builder);

            e.Count(0);
            AssertEx.Equal(new byte[] { 0x00, 0x00 }, b.ToArray());
            b.Clear();

            e.Count(ushort.MaxValue);
            AssertEx.Equal(new byte[] { 0xff, 0xff }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.Count(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Count(ushort.MaxValue + 1));
        }

        [Fact]
        public void NamedArgumentsEncoder_AddArgument()
        {
            var b = new BlobBuilder();
            var e = new NamedArgumentsEncoder(b);
            Assert.Same(b, e.Builder);

            NamedArgumentTypeEncoder type;
            NameEncoder name;
            LiteralEncoder literal;
            e.AddArgument(true, out type, out name, out literal);

            AssertEx.Equal(new byte[] { 0x53 }, b.ToArray());
            Assert.Same(b, type.Builder);
            Assert.Same(b, name.Builder);
            Assert.Same(b, literal.Builder);
            b.Clear();

            e.AddArgument(false,
                t => Assert.Same(b, t.Builder),
                n => Assert.Same(b, n.Builder),
                l => Assert.Same(b, l.Builder));
            AssertEx.Equal(new byte[] { 0x54 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentNullException>(() => e.AddArgument(true, null, _ => { }, _ => { }));
            Assert.Throws<ArgumentNullException>(() => e.AddArgument(true, _ => { }, null, _ => { }));
            Assert.Throws<ArgumentNullException>(() => e.AddArgument(true, _ => { }, _ => { }, null));
        }

        [Fact]
        public void NamedArgumentTypeEncoder_ScalarType()
        {
            var b = new BlobBuilder();
            var e = new NamedArgumentTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.ScalarType();
            AssertEx.Equal(new byte[0], b.ToArray());
            b.Clear();
        }

        [Fact]
        public void NamedArgumentTypeEncoder_Object()
        {
            var b = new BlobBuilder();
            var e = new NamedArgumentTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.Object();
            AssertEx.Equal(new byte[] { 0x51 }, b.ToArray());
            b.Clear();
        }

        [Fact]
        public void NamedArgumentTypeEncoder_SZArray()
        {
            var b = new BlobBuilder();
            var e = new NamedArgumentTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.SZArray();
            AssertEx.Equal(new byte[0], b.ToArray());
            b.Clear();
        }

        [Fact]
        public void CustomAttributeArrayTypeEncoder_ObjectArray()
        {
            var b = new BlobBuilder();
            var e = new CustomAttributeArrayTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.ObjectArray();
            AssertEx.Equal(new byte[] { 0x1D, 0x51 }, b.ToArray());
            b.Clear();
        }

        [Fact]
        public void CustomAttributeArrayTypeEncoder_ElementType()
        {
            var b = new BlobBuilder();
            var e = new CustomAttributeArrayTypeEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.ElementType();
            AssertEx.Equal(new byte[] { 0x1D }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();
        }

        [Fact]
        public void CustomAttributeElementTypeEncoder_Primitives()
        {
            var b = new BlobBuilder();
            var e = new CustomAttributeElementTypeEncoder(b);
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
        }

        [Fact]
        public void CustomAttributeElementTypeEncoder_PrimitiveType()
        {
            var b = new BlobBuilder();
            var e = new CustomAttributeElementTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.PrimitiveType(PrimitiveSerializationTypeCode.Boolean);
            AssertEx.Equal(new byte[] { 0x02 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.Char);
            AssertEx.Equal(new byte[] { 0x03 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.SByte);
            AssertEx.Equal(new byte[] { 0x04 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.Byte);
            AssertEx.Equal(new byte[] { 0x05 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.Int16);
            AssertEx.Equal(new byte[] { 0x06 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.UInt16);
            AssertEx.Equal(new byte[] { 0x07 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.Int32);
            AssertEx.Equal(new byte[] { 0x08 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.UInt32);
            AssertEx.Equal(new byte[] { 0x09 }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.Int64);
            AssertEx.Equal(new byte[] { 0x0A }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.UInt64);
            AssertEx.Equal(new byte[] { 0x0B }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.Single);
            AssertEx.Equal(new byte[] { 0x0C }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.Double);
            AssertEx.Equal(new byte[] { 0x0D }, b.ToArray());
            b.Clear();

            e.PrimitiveType(PrimitiveSerializationTypeCode.String);
            AssertEx.Equal(new byte[] { 0x0E }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentOutOfRangeException>(() => e.PrimitiveType((PrimitiveSerializationTypeCode)255));
        }

        [Fact]
        public void CustomAttributeElementTypeEncoder_SystemType()
        {
            var b = new BlobBuilder();
            var e = new CustomAttributeElementTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.SystemType();
            AssertEx.Equal(new byte[] { 0x50 }, b.ToArray());
        }

        [Fact]
        public void CustomAttributeElementTypeEncoder_Enum()
        {
            var b = new BlobBuilder();
            var e = new CustomAttributeElementTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.Enum("abc");
            AssertEx.Equal(new byte[] { 0x55, 0x03, 0x61, 0x62, 0x63 }, b.ToArray());
            b.Clear();

            e.Enum("\ud800"); // unpaired surrogate
            AssertEx.Equal(new byte[] { 0x55, 0x03, 0xED, 0xA0, 0x80 }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentNullException>(() => e.Enum(null));
            AssertExtensions.Throws<ArgumentException>("enumTypeName", () => e.Enum(""));
        }

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
            Assert.Same(b, e.Builder);

            e.Type(MetadataTokens.TypeDefinitionHandle(1), isValueType: true);
            AssertEx.Equal(new byte[] { 0x11, 0x04 }, b.ToArray());
            b.Clear();

            e.Type(MetadataTokens.TypeDefinitionHandle(1), isValueType: false);
            AssertEx.Equal(new byte[] { 0x12, 0x04 }, b.ToArray());
            b.Clear();

            e.Type(MetadataTokens.TypeReferenceHandle(1), isValueType: false);
            AssertEx.Equal(new byte[] { 0x12, 0x05 }, b.ToArray());
            b.Clear();

            AssertExtensions.Throws<ArgumentException>(null, () => e.Type(MetadataTokens.TypeSpecificationHandle(1), isValueType: false));
            AssertExtensions.Throws<ArgumentException>(null, () => e.Type(default(EntityHandle), isValueType: false));
            Assert.Equal(0, b.Count);
        }

        [Fact]
        public void SignatureTypeEncoder_FunctionPointer()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);
            Assert.Same(b, e.Builder);

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

            AssertExtensions.Throws<ArgumentException>("attributes", () => e.FunctionPointer(0, (FunctionPointerAttributes)1000, genericParameterCount: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.FunctionPointer(0, 0, genericParameterCount: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.FunctionPointer(0, 0, genericParameterCount: ushort.MaxValue + 1));
            Assert.Equal(0, b.Count);
        }

        [Fact]
        public void SignatureTypeEncoder_GenericInstantiation()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);
            Assert.Same(b, e.Builder);

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

            AssertExtensions.Throws<ArgumentException>(null, () => e.GenericInstantiation(MetadataTokens.TypeSpecificationHandle(1), 1, isValueType: false));
            AssertExtensions.Throws<ArgumentException>(null, () => e.GenericInstantiation(default(EntityHandle), 1, isValueType: false));
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
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

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
            Assert.Same(b, e.Builder);

            var p = e.Pointer();
            AssertEx.Equal(new byte[] { 0x0F }, b.ToArray());
            Assert.Same(b, p.Builder);
        }

        [Fact]
        public void SignatureTypeEncoder_VoidPointer()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.VoidPointer();
            AssertEx.Equal(new byte[] { 0x0F, 0x01 }, b.ToArray());
        }

        [Fact]
        public void SignatureTypeEncoder_SZArray()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);
            Assert.Same(b, e.Builder);

            var a = e.SZArray();
            AssertEx.Equal(new byte[] { 0x1D }, b.ToArray());
            Assert.Same(b, a.Builder);
        }

        [Fact]
        public void SignatureTypeEncoder_CustomModifiers()
        {
            var b = new BlobBuilder();
            var e = new SignatureTypeEncoder(b);
            Assert.Same(b, e.Builder);

            var a = e.CustomModifiers();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, a.Builder);
        }

        [Fact]
        public void CustomModifiersEncoder_AddModifier()
        {
            var b = new BlobBuilder();
            var e = new CustomModifiersEncoder(b);
            Assert.Same(b, e.Builder);

            var a = e.AddModifier(MetadataTokens.TypeDefinitionHandle(1), true);
            AssertEx.Equal(new byte[] { 0x20, 0x04 }, b.ToArray());
            Assert.Same(b, a.Builder);
            b.Clear();

            e.AddModifier(MetadataTokens.TypeReferenceHandle(1), false);
            AssertEx.Equal(new byte[] { 0x1f, 0x05 }, b.ToArray());
            b.Clear();

            e.AddModifier(MetadataTokens.TypeSpecificationHandle(1), false);
            AssertEx.Equal(new byte[] { 0x1f, 0x06 }, b.ToArray());
            b.Clear();

            AssertExtensions.Throws<ArgumentException>("type", () => e.AddModifier(default(EntityHandle), true));
            AssertExtensions.Throws<ArgumentException>("type", () => e.AddModifier(default(TypeDefinitionHandle), true));
            AssertExtensions.Throws<ArgumentException>("type", () => e.AddModifier(default(TypeReferenceHandle), true));
            AssertExtensions.Throws<ArgumentException>("type", () => e.AddModifier(default(TypeSpecificationHandle), true));
            AssertExtensions.Throws<ArgumentException>(null, () => e.AddModifier(MetadataTokens.FieldDefinitionHandle(1), true));
        }

        [Fact]
        public void ArrayShapeEncoder_Shape()
        {
            var b = new BlobBuilder();
            var e = new ArrayShapeEncoder(b);
            Assert.Same(b, e.Builder);

            e.Shape(ushort.MaxValue, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty);
            AssertEx.Equal(new byte[]
            {
                0xC0, 0x00, 0xFF, 0xFF,
                0x00,
                0x00
            }, b.ToArray());
            b.Clear();

            e.Shape(3, ImmutableArray.Create(0x0A), ImmutableArray<int>.Empty);
            AssertEx.Equal(new byte[]
            {
                0x03,
                0x01, 0x0A,
                0x00
            }, b.ToArray());
            b.Clear();

            e.Shape(3, ImmutableArray.Create(0x0A, 0x0B), ImmutableArray.Create(0x02, 0x03));
            AssertEx.Equal(new byte[]
            {
                0x03,
                0x02, 0x0A, 0x0B,
                0x02, 0x04, 0x06
            }, b.ToArray());
            b.Clear();

            e.Shape(3, ImmutableArray<int>.Empty, ImmutableArray.Create(-2, -1));
            AssertEx.Equal(new byte[]
            {
                0x03,
                0x00,
                0x02, 0x7D, 0x7F
            }, b.ToArray());
            b.Clear();

            e.Shape(3, ImmutableArray.Create(BlobWriterImpl.MaxCompressedIntegerValue), ImmutableArray.Create(BlobWriterImpl.MinSignedCompressedIntegerValue, BlobWriterImpl.MaxSignedCompressedIntegerValue));
            AssertEx.Equal(new byte[]
            {
                0x03,
                0x01, 0xDF, 0xFF, 0xFF, 0xFF,
                0x02, 0xC0, 0x00, 0x00, 0x01, 0xDF, 0xFF, 0xFF, 0xFE
            }, b.ToArray());
            b.Clear();

            Assert.Throws<ArgumentNullException>(() => e.Shape(1, default(ImmutableArray<int>), ImmutableArray<int>.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(0, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(-1, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(ushort.MaxValue + 1, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(1, ImmutableArray.Create(1,2,3), ImmutableArray<int>.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(1, ImmutableArray<int>.Empty, ImmutableArray.Create(1,2,3)));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(1, ImmutableArray.Create(-1), ImmutableArray<int>.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(1, ImmutableArray.Create(BlobWriterImpl.MaxCompressedIntegerValue + 1), ImmutableArray<int>.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(1, ImmutableArray<int>.Empty, ImmutableArray.Create(BlobWriterImpl.MinSignedCompressedIntegerValue - 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => e.Shape(1, ImmutableArray<int>.Empty, ImmutableArray.Create(BlobWriterImpl.MaxSignedCompressedIntegerValue + 1)));
        }

        [Fact]
        public void ReturnTypeEncoder_CustomModifiers()
        {
            var b = new BlobBuilder();
            var e = new ReturnTypeEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.CustomModifiers();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void ReturnTypeEncoder_Type()
        {
            var b = new BlobBuilder();
            var e = new ReturnTypeEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.Type(true);
            AssertEx.Equal(new byte[] { 0x10 }, b.ToArray());
            Assert.Same(b, s.Builder);
            b.Clear();

            e.Type(false);
            AssertEx.Equal(new byte[0], b.ToArray());
            b.Clear();

            e.Type();
            AssertEx.Equal(new byte[0], b.ToArray());
            b.Clear();
        }

        [Fact]
        public void ReturnTypeEncoder_TypedReference()
        {
            var b = new BlobBuilder();
            var e = new ReturnTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.TypedReference();
            AssertEx.Equal(new byte[] { 0x16 }, b.ToArray());
        }

        [Fact]
        public void ReturnTypeEncoder_Void()
        {
            var b = new BlobBuilder();
            var e = new ReturnTypeEncoder(b);
            Assert.Same(b, e.Builder);

            e.Void();
            AssertEx.Equal(new byte[] { 0x01 }, b.ToArray());
        }

        [Fact]
        public void ParametersEncoder_AddParameter()
        {
            var b = new BlobBuilder();
            var e = new ParametersEncoder(b);
            Assert.Same(b, e.Builder);

            var s = e.AddParameter();
            AssertEx.Equal(new byte[0], b.ToArray());
            Assert.Same(b, s.Builder);
        }

        [Fact]
        public void ParametersEncoder_StartVarArgs()
        {
            var b = new BlobBuilder();
            var e = new ParametersEncoder(b, hasVarArgs: true);
            Assert.Same(b, e.Builder);

            var s = e.StartVarArgs();
            AssertEx.Equal(new byte[] { 0x41 }, b.ToArray());
            Assert.Same(b, s.Builder);
            Assert.False(s.HasVarArgs);

            Assert.Throws<InvalidOperationException>(() => s.StartVarArgs());
        }
    }
}
