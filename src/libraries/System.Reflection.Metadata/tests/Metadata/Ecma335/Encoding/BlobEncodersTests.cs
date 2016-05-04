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
        // SignatureTypeEncoder
        // CustomModifiersEncoder
        // ArrayShapeEncoder
        // ReturnTypeEncoder
        // ParametersEncoder
    }
}
