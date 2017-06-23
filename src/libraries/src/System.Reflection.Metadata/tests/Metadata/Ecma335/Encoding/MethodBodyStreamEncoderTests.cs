// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class MethodBodyStreamEncoderTests
    {
        private static void WriteFakeILWithBranches(BlobBuilder builder, ControlFlowBuilder branchBuilder, int size)
        {
            Assert.Equal(0, builder.Count);

            const byte filling = 0x01;
            int ilOffset = 0;
            foreach (var branch in branchBuilder.Branches)
            {
                builder.WriteBytes(filling, branch.ILOffset - ilOffset);

                Assert.Equal(branch.ILOffset, builder.Count);
                builder.WriteByte((byte)branch.OpCode);

                int operandSize = branch.OpCode.GetBranchOperandSize();
                if (operandSize == 1)
                {
                    builder.WriteSByte(-1);
                }
                else
                {
                    builder.WriteInt32(-1);
                }

                ilOffset = branch.ILOffset + sizeof(byte) + operandSize;
            }

            builder.WriteBytes(filling, size - ilOffset);
            Assert.Equal(size, builder.Count);
        }

        [Fact]
        public void AddMethodBody_Errors()
        {
            var streamBuilder = new BlobBuilder();
            var il = new InstructionEncoder(new BlobBuilder());

            Assert.Throws<ArgumentNullException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(default(InstructionEncoder)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(il, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(il, ushort.MaxValue + 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(codeSize: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(codeSize: 1, maxStack: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(codeSize: 1, maxStack: ushort.MaxValue + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(codeSize: 1, exceptionRegionCount: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(codeSize: 1, exceptionRegionCount: 699051));
        }

        [Fact]
        public void AddMethodBody_Reserved_Tiny1()
        {
            var streamBuilder = new BlobBuilder(32);
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            streamBuilder.WriteBytes(0x01, 3);

            var body = encoder.AddMethodBody(10);
            Assert.Equal(3, body.Offset);
         
            var segment = body.Instructions.GetBytes();
            Assert.Equal(4, segment.Offset); // +1 byte for the header
            Assert.Equal(10, segment.Count);

            Assert.Null(body.ExceptionRegions.Builder);

            new BlobWriter(body.Instructions).WriteBytes(0x02, 10);

            AssertEx.Equal(new byte[] 
            {
                0x01, 0x01, 0x01,
                0x2A, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02
            }, streamBuilder.ToArray());
        }

        [Fact]
        public void AddMethodBody_Reserved_Tiny_AttributesIgnored()
        {
            var streamBuilder = new BlobBuilder();
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            var body = encoder.AddMethodBody(10, attributes: MethodBodyAttributes.None);
            new BlobWriter(body.Instructions).WriteBytes(0x02, 10);

            AssertEx.Equal(new byte[]
            {
                0x2A, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02
            }, streamBuilder.ToArray());
        }

        [Fact]
        public void AddMethodBody_Reserved_Tiny_MaxStackIgnored()
        {
            var streamBuilder = new BlobBuilder();
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            var body = encoder.AddMethodBody(10, maxStack: 7);
            new BlobWriter(body.Instructions).WriteBytes(0x02, 10);

            AssertEx.Equal(new byte[]
            {
                0x2A, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02
            }, streamBuilder.ToArray());
        }

        [Fact]
        public void AddMethodBody_Reserved_Tiny_Empty()
        {
            var streamBuilder = new BlobBuilder();
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            var body = encoder.AddMethodBody(0);
            Assert.Equal(0, body.Offset);

            var segment = body.Instructions.GetBytes();
            Assert.Equal(1, segment.Offset); // +1 byte for the header
            Assert.Equal(0, segment.Count);

            Assert.Null(body.ExceptionRegions.Builder);

            AssertEx.Equal(new byte[]
            {
                0x02
            }, streamBuilder.ToArray());
        }

        [Fact]
        public void AddMethodBody_Reserved_Fat1()
        {
            var streamBuilder = new BlobBuilder(32);
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            streamBuilder.WriteBytes(0x01, 3);

            var body = encoder.AddMethodBody(10, maxStack: 9);
            Assert.Equal(4, body.Offset); // 4B aligned

            var segment = body.Instructions.GetBytes();
            Assert.Equal(16, segment.Offset); // +12 byte for the header
            Assert.Equal(10, segment.Count);

            Assert.Null(body.ExceptionRegions.Builder);

            new BlobWriter(body.Instructions).WriteBytes(0x02, 10);

            AssertEx.Equal(new byte[] 
            {
                0x01, 0x01, 0x01,
                0x00, // padding
                0x13, 0x30,
                0x09, 0x00, // max stack
                0x0A, 0x00, 0x00, 0x00, // code size
                0x00, 0x00, 0x00, 0x00, // local sig
                0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02 
            }, streamBuilder.ToArray());
        }

        [Fact]
        public void AddMethodBody_Reserved_Fat2()
        {
            var streamBuilder = new BlobBuilder(32);
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            streamBuilder.WriteBytes(0x01, 3);

            var body = encoder.AddMethodBody(10, localVariablesSignature: MetadataTokens.StandaloneSignatureHandle(0xABCDEF));
            Assert.Equal(4, body.Offset); // 4B aligned

            var segment = body.Instructions.GetBytes();
            Assert.Equal(16, segment.Offset); // +12 byte for the header
            Assert.Equal(10, segment.Count);

            Assert.Null(body.ExceptionRegions.Builder);

            new BlobWriter(body.Instructions).WriteBytes(0x02, 10);

            AssertEx.Equal(new byte[]
            {
                0x01, 0x01, 0x01,
                0x00, // padding
                0x13, 0x30,
                0x08, 0x00, // max stack
                0x0A, 0x00, 0x00, 0x00, // code size
                0xEF, 0xCD, 0xAB, 0x11, // local sig
                0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02
            }, streamBuilder.ToArray());
        }

        [Fact]
        public void AddMethodBody_Reserved_Exceptions_Fat1()
        {
            var streamBuilder = new BlobBuilder(32);
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            streamBuilder.WriteBytes(0x01, 3);

            var body = encoder.AddMethodBody(10, exceptionRegionCount: 699050);
            Assert.Equal(4, body.Offset); // 4B aligned

            var segment = body.Instructions.GetBytes();
            Assert.Equal(16, segment.Offset); // +12 byte for the header
            Assert.Equal(10, segment.Count);

            Assert.Same(streamBuilder, body.ExceptionRegions.Builder);

            new BlobWriter(body.Instructions).WriteBytes(0x02, 10);

            AssertEx.Equal(new byte[]
            {
                0x01, 0x01, 0x01,
                0x00, // padding
                0x1B, 0x30, // flags
                0x08, 0x00, // max stack
                0x0A, 0x00, 0x00, 0x00, // code size
                0x00, 0x00, 0x00, 0x00, // local sig
                0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,

                // exception table

                0x00, 0x00,      // padding
                0x41,            // kind
                0xF4, 0xFF, 0xFF // size fo the table
            }, streamBuilder.ToArray());
        }

        [Fact]
        public unsafe void TinyBody()
        {
            var streamBuilder = new BlobBuilder();
            var codeBuilder = new BlobBuilder();
            var flowBuilder = new ControlFlowBuilder();

            var il = new InstructionEncoder(codeBuilder, flowBuilder);

            codeBuilder.WriteBytes(1, 61);
            var l1 = il.DefineLabel();
            il.MarkLabel(l1);

            Assert.Equal(61, flowBuilder.Labels.Single());

            il.Branch(ILOpCode.Br_s, l1);

            var brInfo = flowBuilder.Branches.Single();
            Assert.Equal(61, brInfo.ILOffset);
            Assert.Equal(l1, brInfo.Label);
            Assert.Equal(ILOpCode.Br_s, brInfo.OpCode);

            AssertEx.Equal(new byte[] { 1, (byte)ILOpCode.Br_s, unchecked((byte)-1) }, codeBuilder.ToArray(60, 3));

            var streamEncoder = new MethodBodyStreamEncoder(streamBuilder);
            int bodyOffset = streamEncoder.AddMethodBody(
                il,
                maxStack: 2,
                localVariablesSignature: default(StandaloneSignatureHandle),
                attributes: MethodBodyAttributes.None);

            var bodyBytes = streamBuilder.ToArray();

            AssertEx.Equal(new byte[] 
            {
                0xFE, // tiny header
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x2B, 0xFE
            }, bodyBytes);

            fixed (byte* bodyPtr = &bodyBytes[0])
            {
                var body = MethodBodyBlock.Create(new BlobReader(bodyPtr, bodyBytes.Length));

                Assert.Equal(0, body.ExceptionRegions.Length);
                Assert.Equal(default(StandaloneSignatureHandle), body.LocalSignature);
                Assert.Equal(8, body.MaxStack);
                Assert.Equal(bodyBytes.Length, body.Size);

                var ilBytes = body.GetILBytes();
                AssertEx.Equal(new byte[]
                {
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x2B, 0xFE
                }, ilBytes);
            }
        }

        [Fact]
        public unsafe void FatBody()
        {
            var streamBuilder = new BlobBuilder();
            var codeBuilder = new BlobBuilder();
            var flowBuilder = new ControlFlowBuilder();
            var il = new InstructionEncoder(codeBuilder, flowBuilder);

            codeBuilder.WriteBytes(1, 62);
            var l1 = il.DefineLabel();
            il.MarkLabel(l1);

            Assert.Equal(62, flowBuilder.Labels.Single());

            il.Branch(ILOpCode.Br_s, l1);

            var brInfo = flowBuilder.Branches.Single();
            Assert.Equal(62, brInfo.ILOffset);
            Assert.Equal(l1, brInfo.Label);
            Assert.Equal(ILOpCode.Br_s, brInfo.OpCode);

            AssertEx.Equal(new byte[] { 1, 1, (byte)ILOpCode.Br_s, unchecked((byte)-1) }, codeBuilder.ToArray(60, 4));

            var streamEncoder = new MethodBodyStreamEncoder(streamBuilder);
            int bodyOffset = streamEncoder.AddMethodBody(
                il,
                maxStack: 2,
                localVariablesSignature: default(StandaloneSignatureHandle),
                attributes: MethodBodyAttributes.None);

            var bodyBytes = streamBuilder.ToArray();

            AssertEx.Equal(new byte[]
            {
                0x03, 0x30, 0x02, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // fat header
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x2B, 0xFE
            }, bodyBytes);

            fixed (byte* bodyPtr = &bodyBytes[0])
            {
                var body = MethodBodyBlock.Create(new BlobReader(bodyPtr, bodyBytes.Length));

                Assert.Equal(0, body.ExceptionRegions.Length);
                Assert.Equal(default(StandaloneSignatureHandle), body.LocalSignature);
                Assert.Equal(2, body.MaxStack);
                Assert.Equal(bodyBytes.Length, body.Size);

                var ilBytes = body.GetILBytes();
                AssertEx.Equal(new byte[]
                {
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x2B, 0xFE
                }, ilBytes);
            }
        }

        [Fact]
        public void Branches1()
        {
            var flowBuilder = new ControlFlowBuilder();

            var l0 = flowBuilder.AddLabel();
            var l64 = flowBuilder.AddLabel();
            var l255 = flowBuilder.AddLabel();

            flowBuilder.MarkLabel(0, l0);
            flowBuilder.MarkLabel(64, l64);
            flowBuilder.MarkLabel(255, l255);

            flowBuilder.AddBranch(0, l255, ILOpCode.Bge);
            flowBuilder.AddBranch(16, l0, ILOpCode.Bge_un_s); // blob boundary
            flowBuilder.AddBranch(33, l255, ILOpCode.Ble);    // blob boundary
            flowBuilder.AddBranch(38, l0, ILOpCode.Ble_un_s); // branches immediately next to each other
            flowBuilder.AddBranch(40, l255, ILOpCode.Blt);    // branches immediately next to each other
            flowBuilder.AddBranch(46, l64, ILOpCode.Blt_un_s);
            flowBuilder.AddBranch(254, l0, ILOpCode.Brfalse); // long branch at the end

            var dstBuilder = new BlobBuilder();
            var srcBuilder = new BlobBuilder(capacity: 17);
            WriteFakeILWithBranches(srcBuilder, flowBuilder, size: 259);

            flowBuilder.CopyCodeAndFixupBranches(srcBuilder, dstBuilder);

            AssertEx.Equal(new byte[] 
            {
                (byte)ILOpCode.Bge, 0xFA, 0x00, 0x00, 0x00,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                (byte)ILOpCode.Bge_un_s, 0xEE,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                (byte)ILOpCode.Ble, 0xD9, 0x00, 0x00, 0x00,
                (byte)ILOpCode.Ble_un_s, 0xD8,
                (byte)ILOpCode.Blt, 0xD2, 0x00, 0x00, 0x00,
                0x01,
                (byte)ILOpCode.Blt_un_s, 0x10,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                (byte)ILOpCode.Brfalse, 0xFD, 0xFE, 0xFF, 0xFF,
            }, dstBuilder.ToArray());
        }

        [Fact]
        public void BranchErrors()
        {
            var codeBuilder = new BlobBuilder();

            var il = new InstructionEncoder(codeBuilder);
            Assert.Throws<InvalidOperationException>(() => il.DefineLabel());

            il = new InstructionEncoder(codeBuilder, new ControlFlowBuilder());
            il.DefineLabel();
            il.DefineLabel();
            var l2 = il.DefineLabel();

            var flowBuilder = new ControlFlowBuilder();
            il = new InstructionEncoder(codeBuilder, flowBuilder);
            var l0 = il.DefineLabel();

            AssertExtensions.Throws<ArgumentException>("opCode", () => il.Branch(ILOpCode.Nop, l0));
            Assert.Throws<ArgumentNullException>(() => il.Branch(ILOpCode.Br, default(LabelHandle)));
            AssertExtensions.Throws<ArgumentException>("label", () => il.Branch(ILOpCode.Br, l2));
        }

        [Fact]
        public void BranchErrors_UnmarkedLabel()
        {
            var streamBuilder = new BlobBuilder();
            var codeBuilder = new BlobBuilder();
            var flowBuilder = new ControlFlowBuilder();

            var il = new InstructionEncoder(codeBuilder, flowBuilder);
            var l = il.DefineLabel();
            il.Branch(ILOpCode.Br_s, l);
            il.OpCode(ILOpCode.Ret);

            Assert.Throws<InvalidOperationException>(() => new MethodBodyStreamEncoder(streamBuilder).AddMethodBody(il));
        }
    }
}
