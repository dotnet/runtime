// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class InstructionEncoderTests
    {
        [Fact]
        public void Ctor()
        {
            var code = new BlobBuilder();
            var flow = new ControlFlowBuilder();
            var ie = new InstructionEncoder(code, flow);
            Assert.Same(ie.CodeBuilder, code);
            Assert.Same(ie.ControlFlowBuilder, flow);

            Assert.Throws<ArgumentNullException>(() => new InstructionEncoder(null));
        }

        [Fact]
        public void OpCode()
        {
            var builder = new BlobBuilder();
            builder.WriteByte(0xff);

            var il = new InstructionEncoder(builder);
            Assert.Equal(1, il.Offset);

            il.OpCode(ILOpCode.Add);
            Assert.Equal(2, il.Offset);

            builder.WriteByte(0xee);
            Assert.Equal(3, il.Offset);

            il.OpCode(ILOpCode.Arglist);
            Assert.Equal(5, il.Offset);

            builder.WriteByte(0xdd);
            Assert.Equal(6, il.Offset);

            il.OpCode(ILOpCode.Readonly);
            Assert.Equal(8, il.Offset);

            builder.WriteByte(0xcc);
            Assert.Equal(9, il.Offset);

            AssertEx.Equal(new byte[]
            {
                0xFF,
                0x58,
                0xEE,
                0xFE, 0x00,
                0xDD,
                0xFE, 0x1E,
                0xCC
            }, builder.ToArray());
        }

        [Fact]
        public void TokenInstructions()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);
            Assert.Equal(0, il.Offset);

            il.Token(MetadataTokens.TypeDefinitionHandle(0x123456));
            Assert.Equal(4, il.Offset);

            il.Token(0x7F7E7D7C);
            Assert.Equal(8, il.Offset);

            il.LoadString(MetadataTokens.UserStringHandle(0x010203));
            Assert.Equal(13, il.Offset);

            il.Call(MetadataTokens.MethodDefinitionHandle(0xA0A1A2));
            Assert.Equal(18, il.Offset);

            il.Call(MetadataTokens.MethodSpecificationHandle(0xB0B1B2));
            Assert.Equal(23, il.Offset);

            il.Call(MetadataTokens.MemberReferenceHandle(0xC0C1C2));
            Assert.Equal(28, il.Offset);

            il.Call((EntityHandle)MetadataTokens.MethodDefinitionHandle(0xD0D1D2));
            Assert.Equal(33, il.Offset);

            il.Call((EntityHandle)MetadataTokens.MethodSpecificationHandle(0xE0E1E2));
            Assert.Equal(38, il.Offset);

            il.Call((EntityHandle)MetadataTokens.MemberReferenceHandle(0xF0F1F2));
            Assert.Equal(43, il.Offset);

            il.CallIndirect(MetadataTokens.StandaloneSignatureHandle(0x001122));
            Assert.Equal(48, il.Offset);

            AssertEx.Equal(new byte[]
            {
                0x56, 0x34, 0x12, 0x02,
                0x7C, 0x7D, 0x7E, 0x7F,
                (byte)ILOpCode.Ldstr, 0x03, 0x02, 0x01, 0x70,
                (byte)ILOpCode.Call, 0xA2, 0xA1, 0xA0, 0x06,
                (byte)ILOpCode.Call, 0xB2, 0xB1, 0xB0, 0x2B,
                (byte)ILOpCode.Call, 0xC2, 0xC1, 0xC0, 0x0A,
                (byte)ILOpCode.Call, 0xD2, 0xD1, 0xD0, 0x06,
                (byte)ILOpCode.Call, 0xE2, 0xE1, 0xE0, 0x2B,
                (byte)ILOpCode.Call, 0xF2, 0xF1, 0xF0, 0x0A,
                (byte)ILOpCode.Calli, 0x22, 0x11, 0x00, 0x11
            }, builder.ToArray());
        }

        [Fact]
        public void LoadConstantI4()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            for (int i = -1; i < 9; i++)
            {
                il.LoadConstantI4(i);
            }

            il.LoadConstantI4(sbyte.MinValue);
            il.LoadConstantI4(sbyte.MaxValue);
            il.LoadConstantI4(-2);
            il.LoadConstantI4(10);
            il.LoadConstantI4(int.MinValue);
            il.LoadConstantI4(int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Ldc_i4_m1,
                (byte)ILOpCode.Ldc_i4_0,
                (byte)ILOpCode.Ldc_i4_1,
                (byte)ILOpCode.Ldc_i4_2,
                (byte)ILOpCode.Ldc_i4_3,
                (byte)ILOpCode.Ldc_i4_4,
                (byte)ILOpCode.Ldc_i4_5,
                (byte)ILOpCode.Ldc_i4_6,
                (byte)ILOpCode.Ldc_i4_7,
                (byte)ILOpCode.Ldc_i4_8,
                (byte)ILOpCode.Ldc_i4_s, 0x80,
                (byte)ILOpCode.Ldc_i4_s, 0x7F,
                (byte)ILOpCode.Ldc_i4_s, 0xFE,
                (byte)ILOpCode.Ldc_i4_s, 0x0A,
                (byte)ILOpCode.Ldc_i4, 0x00, 0x00, 0x00, 0x80,
                (byte)ILOpCode.Ldc_i4, 0xFF, 0xFF, 0xFF, 0x7F
            }, builder.ToArray());
        }

        [Fact]
        public void LoadConstantI8()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.LoadConstantI8(0);
            il.LoadConstantI8(long.MinValue);
            il.LoadConstantI8(long.MaxValue);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Ldc_i8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                (byte)ILOpCode.Ldc_i8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80,
                (byte)ILOpCode.Ldc_i8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F
            }, builder.ToArray());
        }

        [Fact]
        public void LoadConstantR4()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.LoadConstantR4(0.0f);
            il.LoadConstantR4(float.MaxValue);
            il.LoadConstantR4(float.MinValue);
            il.LoadConstantR4(float.NaN);
            il.LoadConstantR4(float.NegativeInfinity);
            il.LoadConstantR4(float.PositiveInfinity);
            il.LoadConstantR4(float.Epsilon);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Ldc_r4, 0x00, 0x00, 0x00, 0x00,
                (byte)ILOpCode.Ldc_r4, 0xFF, 0xFF, 0x7F, 0x7F,
                (byte)ILOpCode.Ldc_r4, 0xFF, 0xFF, 0x7F, 0xFF,
                (byte)ILOpCode.Ldc_r4, 0x00, 0x00, 0xC0, 0xFF,
                (byte)ILOpCode.Ldc_r4, 0x00, 0x00, 0x80, 0xFF,
                (byte)ILOpCode.Ldc_r4, 0x00, 0x00, 0x80, 0x7F,
                (byte)ILOpCode.Ldc_r4, 0x01, 0x00, 0x00, 0x00
            }, builder.ToArray());
        }

        [Fact]
        public void LoadConstantR8()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.LoadConstantR8(0.0);
            il.LoadConstantR8(double.MaxValue);
            il.LoadConstantR8(double.MinValue);
            il.LoadConstantR8(double.NaN);
            il.LoadConstantR8(double.NegativeInfinity);
            il.LoadConstantR8(double.PositiveInfinity);
            il.LoadConstantR8(double.Epsilon);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Ldc_r8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                (byte)ILOpCode.Ldc_r8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0x7F,
                (byte)ILOpCode.Ldc_r8, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0xFF,
                (byte)ILOpCode.Ldc_r8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0xFF,
                (byte)ILOpCode.Ldc_r8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0xFF,
                (byte)ILOpCode.Ldc_r8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x7F,
                (byte)ILOpCode.Ldc_r8, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            }, builder.ToArray());
        }

        [Fact]
        public void LoadLocal()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.LoadLocal(0);
            il.LoadLocal(1);
            il.LoadLocal(2);
            il.LoadLocal(3);
            il.LoadLocal(4);
            il.LoadLocal(byte.MaxValue);
            il.LoadLocal(byte.MaxValue + 1);
            il.LoadLocal(int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Ldloc_0,
                (byte)ILOpCode.Ldloc_1,
                (byte)ILOpCode.Ldloc_2,
                (byte)ILOpCode.Ldloc_3,
                (byte)ILOpCode.Ldloc_s, 0x04,
                (byte)ILOpCode.Ldloc_s, 0xFF,
                0xFE, 0x0C, 0x00, 0x01, 0x00, 0x00,
                0xFE, 0x0C, 0xFF, 0xFF, 0xFF, 0x7F
            }, builder.ToArray());

            Assert.Throws<ArgumentOutOfRangeException>(() => il.LoadLocal(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => il.LoadLocal(int.MinValue));
        }

        [Fact]
        public void StoreLocal()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.StoreLocal(0);
            il.StoreLocal(1);
            il.StoreLocal(2);
            il.StoreLocal(3);
            il.StoreLocal(4);
            il.StoreLocal(byte.MaxValue);
            il.StoreLocal(byte.MaxValue + 1);
            il.StoreLocal(int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Stloc_0,
                (byte)ILOpCode.Stloc_1,
                (byte)ILOpCode.Stloc_2,
                (byte)ILOpCode.Stloc_3,
                (byte)ILOpCode.Stloc_s, 0x04,
                (byte)ILOpCode.Stloc_s, 0xFF,
                0xFE, 0x0E, 0x00, 0x01, 0x00, 0x00,
                0xFE, 0x0E, 0xFF, 0xFF, 0xFF, 0x7F
            }, builder.ToArray());

            Assert.Throws<ArgumentOutOfRangeException>(() => il.StoreLocal(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => il.StoreLocal(int.MinValue));
        }

        [Fact]
        public void LoadLocalAddress()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.LoadLocalAddress(0);
            il.LoadLocalAddress(1);
            il.LoadLocalAddress(2);
            il.LoadLocalAddress(3);
            il.LoadLocalAddress(4);
            il.LoadLocalAddress(byte.MaxValue);
            il.LoadLocalAddress(byte.MaxValue + 1);
            il.LoadLocalAddress(int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Ldloca_s, 0x00,
                (byte)ILOpCode.Ldloca_s, 0x01,
                (byte)ILOpCode.Ldloca_s, 0x02,
                (byte)ILOpCode.Ldloca_s, 0x03,
                (byte)ILOpCode.Ldloca_s, 0x04,
                (byte)ILOpCode.Ldloca_s, 0xFF,
                0xFE, 0x0D, 0x00, 0x01, 0x00, 0x00,
                0xFE, 0x0D, 0xFF, 0xFF, 0xFF, 0x7F
            }, builder.ToArray());

            Assert.Throws<ArgumentOutOfRangeException>(() => il.LoadLocalAddress(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => il.LoadLocalAddress(int.MinValue));
        }

        [Fact]
        public void LoadArgument()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.LoadArgument(0);
            il.LoadArgument(1);
            il.LoadArgument(2);
            il.LoadArgument(3);
            il.LoadArgument(4);
            il.LoadArgument(byte.MaxValue);
            il.LoadArgument(byte.MaxValue + 1);
            il.LoadArgument(int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Ldarg_0,
                (byte)ILOpCode.Ldarg_1,
                (byte)ILOpCode.Ldarg_2,
                (byte)ILOpCode.Ldarg_3,
                (byte)ILOpCode.Ldarg_s, 0x04,
                (byte)ILOpCode.Ldarg_s, 0xFF,
                0xFE, 0x09, 0x00, 0x01, 0x00, 0x00,
                0xFE, 0x09, 0xFF, 0xFF, 0xFF, 0x7F
            }, builder.ToArray());

            Assert.Throws<ArgumentOutOfRangeException>(() => il.LoadArgument(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => il.LoadArgument(int.MinValue));
        }

        [Fact]
        public void LoadArgumentAddress()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.LoadArgumentAddress(0);
            il.LoadArgumentAddress(1);
            il.LoadArgumentAddress(2);
            il.LoadArgumentAddress(3);
            il.LoadArgumentAddress(4);
            il.LoadArgumentAddress(byte.MaxValue);
            il.LoadArgumentAddress(byte.MaxValue + 1);
            il.LoadArgumentAddress(int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Ldarga_s, 0x00,
                (byte)ILOpCode.Ldarga_s, 0x01,
                (byte)ILOpCode.Ldarga_s, 0x02,
                (byte)ILOpCode.Ldarga_s, 0x03,
                (byte)ILOpCode.Ldarga_s, 0x04,
                (byte)ILOpCode.Ldarga_s, 0xFF,
                0xFE, 0x0A, 0x00, 0x01, 0x00, 0x00,
                0xFE, 0x0A, 0xFF, 0xFF, 0xFF, 0x7F
            }, builder.ToArray());

            Assert.Throws<ArgumentOutOfRangeException>(() => il.LoadArgumentAddress(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => il.LoadArgumentAddress(int.MinValue));
        }

        [Fact]
        public void StoreArgument()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);

            il.StoreArgument(0);
            il.StoreArgument(1);
            il.StoreArgument(2);
            il.StoreArgument(3);
            il.StoreArgument(4);
            il.StoreArgument(byte.MaxValue);
            il.StoreArgument(byte.MaxValue + 1);
            il.StoreArgument(int.MaxValue);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Starg_s, 0x00,
                (byte)ILOpCode.Starg_s, 0x01,
                (byte)ILOpCode.Starg_s, 0x02,
                (byte)ILOpCode.Starg_s, 0x03,
                (byte)ILOpCode.Starg_s, 0x04,
                (byte)ILOpCode.Starg_s, 0xFF,
                0xFE, 0x0B, 0x00, 0x01, 0x00, 0x00,
                0xFE, 0x0B, 0xFF, 0xFF, 0xFF, 0x7F
            }, builder.ToArray());

            Assert.Throws<ArgumentOutOfRangeException>(() => il.StoreArgument(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => il.StoreArgument(int.MinValue));
        }

        [Fact]
        public void Branch()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder, new ControlFlowBuilder());
            var l = il.DefineLabel();

            il.Branch(ILOpCode.Br_s, l);
            il.Branch(ILOpCode.Brfalse_s, l);
            il.Branch(ILOpCode.Brtrue_s, l);
            il.Branch(ILOpCode.Beq_s, l);
            il.Branch(ILOpCode.Bge_s, l);
            il.Branch(ILOpCode.Bgt_s, l);
            il.Branch(ILOpCode.Ble_s, l);
            il.Branch(ILOpCode.Blt_s, l);
            il.Branch(ILOpCode.Bne_un_s, l);
            il.Branch(ILOpCode.Bge_un_s, l);
            il.Branch(ILOpCode.Bgt_un_s, l);
            il.Branch(ILOpCode.Ble_un_s, l);
            il.Branch(ILOpCode.Blt_un_s, l);
            il.Branch(ILOpCode.Leave_s, l);
            il.Branch(ILOpCode.Br, l);
            il.Branch(ILOpCode.Brfalse, l);
            il.Branch(ILOpCode.Brtrue, l);
            il.Branch(ILOpCode.Beq, l);
            il.Branch(ILOpCode.Bge, l);
            il.Branch(ILOpCode.Bgt, l);
            il.Branch(ILOpCode.Ble, l);
            il.Branch(ILOpCode.Blt, l);
            il.Branch(ILOpCode.Bne_un, l);
            il.Branch(ILOpCode.Bge_un, l);
            il.Branch(ILOpCode.Bgt_un, l);
            il.Branch(ILOpCode.Ble_un, l);
            il.Branch(ILOpCode.Blt_un, l);
            il.Branch(ILOpCode.Leave, l);

            AssertEx.Equal(new byte[]
            {
                (byte)ILOpCode.Br_s,      0xff,
                (byte)ILOpCode.Brfalse_s, 0xff,
                (byte)ILOpCode.Brtrue_s,  0xff,
                (byte)ILOpCode.Beq_s,     0xff,
                (byte)ILOpCode.Bge_s,     0xff,
                (byte)ILOpCode.Bgt_s,     0xff,
                (byte)ILOpCode.Ble_s,     0xff,
                (byte)ILOpCode.Blt_s,     0xff,
                (byte)ILOpCode.Bne_un_s,  0xff,
                (byte)ILOpCode.Bge_un_s,  0xff,
                (byte)ILOpCode.Bgt_un_s,  0xff,
                (byte)ILOpCode.Ble_un_s,  0xff,
                (byte)ILOpCode.Blt_un_s,  0xff,
                (byte)ILOpCode.Leave_s,   0xff,
                (byte)ILOpCode.Br,        0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Brfalse,   0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Brtrue,    0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Beq,       0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Bge,       0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Bgt,       0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Ble,       0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Blt,       0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Bne_un,    0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Bge_un,    0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Bgt_un,    0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Ble_un,    0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Blt_un,    0xff, 0xff, 0xff, 0xff,
                (byte)ILOpCode.Leave,     0xff, 0xff, 0xff, 0xff,
            }, builder.ToArray());
        }

        [Fact]
        public void BranchAndLabel_Errors()
        {
            var builder = new BlobBuilder();
            var il = new InstructionEncoder(builder);
            var ilcf1 = new InstructionEncoder(builder, new ControlFlowBuilder());
            var ilcf2 = new InstructionEncoder(builder, new ControlFlowBuilder());

            var l1 = ilcf1.DefineLabel();
            ilcf2.DefineLabel();
            var l2 = ilcf2.DefineLabel();

            Assert.Throws<InvalidOperationException>(() => il.DefineLabel());
            Assert.Throws<InvalidOperationException>(() => il.Branch(ILOpCode.Br, default(LabelHandle)));
            Assert.Throws<InvalidOperationException>(() => il.MarkLabel(default(LabelHandle)));

            Assert.Throws<ArgumentNullException>(() => ilcf1.Branch(ILOpCode.Br, default(LabelHandle)));
            Assert.Throws<ArgumentNullException>(() => ilcf1.MarkLabel(default(LabelHandle)));
            AssertExtensions.Throws<ArgumentException>("label", () => ilcf1.Branch(ILOpCode.Br, l2));
            AssertExtensions.Throws<ArgumentException>("label", () => ilcf1.MarkLabel(l2));

            AssertExtensions.Throws<ArgumentException>("opCode", () => ilcf1.Branch(ILOpCode.Box, l1));
        }

        [Fact]
        public void MarkLabel()
        {
            var code = new BlobBuilder();
            var il = new InstructionEncoder(code, new ControlFlowBuilder());

            var l = il.DefineLabel();

            il.Branch(ILOpCode.Br_s, l);

            il.MarkLabel(l);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);

            il.MarkLabel(l);
            il.OpCode(ILOpCode.Ret);

            var builder = new BlobBuilder();
            new MethodBodyStreamEncoder(builder).AddMethodBody(il);

            AssertEx.Equal(new byte[]
            {
                0x16, // header
                (byte)ILOpCode.Br_s, 0x02,
                (byte)ILOpCode.Nop,
                (byte)ILOpCode.Nop,
                (byte)ILOpCode.Ret,
            }, builder.ToArray());
        }
    }
}
