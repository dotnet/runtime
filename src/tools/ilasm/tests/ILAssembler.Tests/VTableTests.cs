// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class VTableTests
    {
        [Fact]
        public void VtfixupDecl_CompilesSuccessfully()
        {
            // .vtfixup directive should compile successfully with VTable fixup support
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void ExportedMethod() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1]
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            // Verify COR flags don't have ILOnly (mixed mode for vtfixups)
            var corHeader = pe.PEHeaders.CorHeader;
            Assert.NotNull(corHeader);
            Assert.False(corHeader!.Flags.HasFlag(CorFlags.ILOnly),
                "VTable fixups require mixed-mode assembly (ILOnly should be cleared)");

            // Verify .sdata section exists
            var sdataSection = pe.PEHeaders.SectionHeaders.FirstOrDefault(s => s.Name == ".sdata");
            Assert.False(sdataSection.Equals(default), "Expected .sdata section for vtable fixups");

            // Read and verify the .sdata section contains valid VTableFixup directory structure
            // Structure: IMAGE_COR_VTABLEFIXUP { DWORD RVA, WORD Count, WORD Type }
            var sdataBytes = pe.GetSectionData(sdataSection.VirtualAddress).GetContent();
            Assert.True(sdataBytes.Length >= 8, "VTableFixup directory should be at least 8 bytes");

            // Read the first VTableFixup entry
            int slotDataRva = BinaryPrimitives.ReadInt32LittleEndian(sdataBytes.AsSpan(0, 4));
            ushort slotCount = BinaryPrimitives.ReadUInt16LittleEndian(sdataBytes.AsSpan(4, 2));
            ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(sdataBytes.AsSpan(6, 2));

            Assert.Equal(1, slotCount);
            Assert.True((flags & 0x01) != 0, "Expected COR_VTABLE_32BIT flag");
            Assert.True((flags & 0x04) != 0, "Expected COR_VTABLE_FROM_UNMANAGED flag");

            // The slot data RVA should point within the .sdata section (after the directory)
            Assert.True(slotDataRva >= sdataSection.VirtualAddress,
                $"Slot data RVA {slotDataRva} should be >= section start {sdataSection.VirtualAddress}");

            // Verify the method token in the slot data (should be a valid MethodDef token)
            int slotDataOffset = slotDataRva - sdataSection.VirtualAddress;
            int methodToken = BinaryPrimitives.ReadInt32LittleEndian(sdataBytes.AsSpan(slotDataOffset, 4));
            Assert.True((methodToken & 0xFF000000) == 0x06000000,
                $"Expected MethodDef token (0x06xxxxxx), got 0x{methodToken:X8}");
        }


        [Fact]
        public void VtfixupDecl_64bit_CompilesSuccessfully()
        {
            // .vtfixup with int64 (64-bit slots) - used for 64-bit platforms
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int64(0)
                .vtfixup [1] int64 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void ExportedMethod() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1]
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            // Verify .sdata section exists
            var sdataSection = pe.PEHeaders.SectionHeaders.FirstOrDefault(s => s.Name == ".sdata");
            Assert.False(sdataSection.Equals(default), "Expected .sdata section for vtable fixups");

            // Read VTableFixup directory entry and verify 64-bit flag
            var sdataBytes = pe.GetSectionData(sdataSection.VirtualAddress).GetContent();
            ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(sdataBytes.AsSpan(6, 2));
            Assert.True((flags & 0x02) != 0, "Expected COR_VTABLE_64BIT flag (0x02)");

            // Verify slot data is 8 bytes (64-bit token)
            int slotDataRva = BinaryPrimitives.ReadInt32LittleEndian(sdataBytes.AsSpan(0, 4));
            int slotDataOffset = slotDataRva - sdataSection.VirtualAddress;

            // 64-bit slot should have method token in lower 32 bits, zeros in upper 32 bits
            long slotValue = BinaryPrimitives.ReadInt64LittleEndian(sdataBytes.AsSpan(slotDataOffset, 8));
            int methodToken = (int)(slotValue & 0xFFFFFFFF);
            Assert.True((methodToken & 0xFF000000) == 0x06000000,
                $"Expected MethodDef token (0x06xxxxxx), got 0x{methodToken:X8}");
        }


        [Fact]
        public void VtfixupDecl_MultipleSlots_CompilesSuccessfully()
        {
            // .vtfixup with multiple slots - each method gets its own slot
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0) int32(0)
                .vtfixup [2] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Method1() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1] as Export1
                        ret
                    }
                    .method public static void Method2() cil managed
                    {
                        .vtentry 1 : 2
                        .export [2] as Export2
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            // Verify .sdata section exists
            var sdataSection = pe.PEHeaders.SectionHeaders.FirstOrDefault(s => s.Name == ".sdata");
            Assert.False(sdataSection.Equals(default), "Expected .sdata section for vtable fixups");

            var sdataBytes = pe.GetSectionData(sdataSection.VirtualAddress).GetContent();

            // Verify VTableFixup directory entry has count of 2
            ushort slotCount = BinaryPrimitives.ReadUInt16LittleEndian(sdataBytes.AsSpan(4, 2));
            Assert.Equal(2, slotCount);

            // Read both method tokens from slot data
            int slotDataRva = BinaryPrimitives.ReadInt32LittleEndian(sdataBytes.AsSpan(0, 4));
            int slotDataOffset = slotDataRva - sdataSection.VirtualAddress;

            int token1 = BinaryPrimitives.ReadInt32LittleEndian(sdataBytes.AsSpan(slotDataOffset, 4));
            int token2 = BinaryPrimitives.ReadInt32LittleEndian(sdataBytes.AsSpan(slotDataOffset + 4, 4));

            // Both should be valid MethodDef tokens
            Assert.True((token1 & 0xFF000000) == 0x06000000,
                $"Slot 1: Expected MethodDef token, got 0x{token1:X8}");
            Assert.True((token2 & 0xFF000000) == 0x06000000,
                $"Slot 2: Expected MethodDef token, got 0x{token2:X8}");

            // Tokens should be different (different methods)
            Assert.NotEqual(token1, token2);

            // Verify the methods exist in metadata with expected names
            var reader = pe.GetMetadataReader();
            var methodNames = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Select(m => reader.GetString(m.Name))
                .ToHashSet();
            Assert.Contains("Method1", methodNames);
            Assert.Contains("Method2", methodNames);
        }
    }
}
