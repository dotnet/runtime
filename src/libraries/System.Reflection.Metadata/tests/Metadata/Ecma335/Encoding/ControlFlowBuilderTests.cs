// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata.Tests;
using Xunit;

namespace System.Reflection.Metadata.Ecma335.Tests
{
    public class ControlFlowBuilderTests
    {
        [Fact]
        public void AddFinallyFaultFilterRegions()
        {
            var code = new BlobBuilder();
            var flow = new ControlFlowBuilder();
            var il = new InstructionEncoder(code, flow);

            var l1 = il.DefineLabel();
            var l2 = il.DefineLabel();
            var l3 = il.DefineLabel();
            var l4 = il.DefineLabel();
            var l5 = il.DefineLabel();

            il.MarkLabel(l1);
            Assert.Equal(0, il.Offset);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l2);
            Assert.Equal(1, il.Offset);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l3);
            Assert.Equal(3, il.Offset);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l4);
            Assert.Equal(6, il.Offset);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l5);
            Assert.Equal(10, il.Offset);

            flow.AddFaultRegion(l1, l2, l3, l4);
            flow.AddFinallyRegion(l1, l2, l3, l4);
            flow.AddFilterRegion(l1, l2, l3, l4, l5);
            
            var builder = new BlobBuilder();
            builder.WriteByte(0xff);
            flow.SerializeExceptionTable(builder);

            AssertEx.Equal(new byte[] 
            {
                0xFF, 0x00, 0x00, 0x00,    // padding
                0x01,                      // flag
                (byte)(builder.Count - 4), // size
                0x00, 0x00,                // reserved

                0x04, 0x00,                // kind
                0x00, 0x00,                // try offset
                0x01,                      // try length
                0x03, 0x00,                // handler offset
                0x03,                      // handler length
                                           
                0x00, 0x00, 0x00, 0x00,    // catch type or filter offset
                                           
                0x02, 0x00,                // kind
                0x00, 0x00,                // try offset
                0x01,                      // try length
                0x03, 0x00,                // handler offset
                0x03,                      // handler length
                                           
                0x00, 0x00, 0x00, 0x00,    // catch type or filter offset

                0x01, 0x00,                // kind
                0x00, 0x00,                // try offset
                0x01,                      // try length
                0x03, 0x00,                // handler offset
                0x03,                      // handler length
                                           
                0x0A, 0x00, 0x00, 0x00     // catch type or filter offset
            }, builder.ToArray());
        }

        [Fact]
        public void AddCatchRegions()
        {
            var code = new BlobBuilder();
            var flow = new ControlFlowBuilder();
            var il = new InstructionEncoder(code, flow);

            var l1 = il.DefineLabel();
            var l2 = il.DefineLabel();
            var l3 = il.DefineLabel();
            var l4 = il.DefineLabel();
            var l5 = il.DefineLabel();

            il.MarkLabel(l1);
            Assert.Equal(0, il.Offset);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l2);
            Assert.Equal(1, il.Offset);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l3);
            Assert.Equal(3, il.Offset);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l4);
            Assert.Equal(6, il.Offset);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l5);
            Assert.Equal(10, il.Offset);

            flow.AddCatchRegion(l1, l2, l3, l4, MetadataTokens.TypeDefinitionHandle(1));
            flow.AddCatchRegion(l1, l2, l3, l4, MetadataTokens.TypeSpecificationHandle(2));
            flow.AddCatchRegion(l1, l2, l3, l4, MetadataTokens.TypeReferenceHandle(3));

            var builder = new BlobBuilder();
            flow.SerializeExceptionTable(builder);

            AssertEx.Equal(new byte[]
            {
                0x01,                      // flag
                (byte)builder.Count,       // size
                0x00, 0x00,                // reserved

                0x00, 0x00,                // kind
                0x00, 0x00,                // try offset
                0x01,                      // try length
                0x03, 0x00,                // handler offset
                0x03,                      // handler length
                                           
                0x01, 0x00, 0x00, 0x02,    // catch type or filter offset
                                           
                0x00, 0x00,                // kind
                0x00, 0x00,                // try offset
                0x01,                      // try length
                0x03, 0x00,                // handler offset
                0x03,                      // handler length
                                           
                0x02, 0x00, 0x00, 0x1B,    // catch type or filter offset

                0x00, 0x00,                // kind
                0x00, 0x00,                // try offset
                0x01,                      // try length
                0x03, 0x00,                // handler offset
                0x03,                      // handler length
                                           
                0x03, 0x00, 0x00, 0x01,    // catch type or filter offset
            }, builder.ToArray());
        }

        [Fact]
        public void AddRegion_Errors1()
        {
            var code = new BlobBuilder();
            var flow = new ControlFlowBuilder();
            var il = new InstructionEncoder(code, flow);
            var ilx = new InstructionEncoder(code, new ControlFlowBuilder());

            var l1 = il.DefineLabel();
            var l2 = il.DefineLabel();
            var l3 = il.DefineLabel();
            var l4 = il.DefineLabel();
            var l5 = il.DefineLabel();

            ilx.DefineLabel();
            ilx.DefineLabel();
            ilx.DefineLabel();
            ilx.DefineLabel();
            ilx.DefineLabel();
            ilx.DefineLabel();
            var lx = ilx.DefineLabel();

            il.MarkLabel(l1);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l2);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l3);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l4);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l5);

            AssertExtensions.Throws<ArgumentException>("catchType", () => flow.AddCatchRegion(l1, l2, l3, l4, default(TypeDefinitionHandle)));
            AssertExtensions.Throws<ArgumentException>("catchType", () => flow.AddCatchRegion(l1, l2, l3, l4, MetadataTokens.MethodDefinitionHandle(1)));
            
            Assert.Throws<ArgumentNullException>(() => flow.AddCatchRegion(default(LabelHandle), l2, l3, l4, MetadataTokens.TypeReferenceHandle(1)));
            Assert.Throws<ArgumentNullException>(() => flow.AddCatchRegion(l1, default(LabelHandle), l3, l4, MetadataTokens.TypeReferenceHandle(1)));
            Assert.Throws<ArgumentNullException>(() => flow.AddCatchRegion(l1, l2, default(LabelHandle), l4, MetadataTokens.TypeReferenceHandle(1)));
            Assert.Throws<ArgumentNullException>(() => flow.AddCatchRegion(l1, l2, l3, default(LabelHandle), MetadataTokens.TypeReferenceHandle(1)));

            AssertExtensions.Throws<ArgumentException>("tryStart", () => flow.AddCatchRegion(lx, l2, l3, l4, MetadataTokens.TypeReferenceHandle(1)));
            AssertExtensions.Throws<ArgumentException>("tryEnd", () => flow.AddCatchRegion(l1, lx, l3, l4, MetadataTokens.TypeReferenceHandle(1)));
            AssertExtensions.Throws<ArgumentException>("handlerStart", () => flow.AddCatchRegion(l1, l2, lx, l4, MetadataTokens.TypeReferenceHandle(1)));
            AssertExtensions.Throws<ArgumentException>("handlerEnd", () => flow.AddCatchRegion(l1, l2, l3, lx, MetadataTokens.TypeReferenceHandle(1)));
        }

        [Fact]
        public void AddRegion_Errors2()
        {
            var code = new BlobBuilder();
            var flow = new ControlFlowBuilder();
            var il = new InstructionEncoder(code, flow);

            var l1 = il.DefineLabel();
            var l2 = il.DefineLabel();
            var l3 = il.DefineLabel();
            var l4 = il.DefineLabel();
            var l5 = il.DefineLabel();

            il.MarkLabel(l1);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l2);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l3);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l4);

            var streamBuilder = new BlobBuilder();
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            flow.AddFaultRegion(l2, l1, l3, l4);
            Assert.Throws<InvalidOperationException>(() => encoder.AddMethodBody(il));
        }

        [Fact]
        public void AddRegion_Errors3()
        {
            var code = new BlobBuilder();
            var flow = new ControlFlowBuilder();
            var il = new InstructionEncoder(code, flow);

            var l1 = il.DefineLabel();
            var l2 = il.DefineLabel();
            var l3 = il.DefineLabel();
            var l4 = il.DefineLabel();
            var l5 = il.DefineLabel();

            il.MarkLabel(l1);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l2);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l3);
            il.OpCode(ILOpCode.Nop);
            il.MarkLabel(l4);

            var streamBuilder = new BlobBuilder();
            var encoder = new MethodBodyStreamEncoder(streamBuilder);

            flow.AddFaultRegion(l1, l2, l4, l3);
            Assert.Throws<InvalidOperationException>(() => encoder.AddMethodBody(il));
        }
    }
}
