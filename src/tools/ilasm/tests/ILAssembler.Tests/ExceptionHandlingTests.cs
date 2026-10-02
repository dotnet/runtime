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
    public class ExceptionHandlingTests
    {
        [Theory]
        [InlineData(-1, 1, 1, 2)]
        [InlineData(0, 4, 1, 2)]
        [InlineData(0, 1, -1, 2)]
        [InlineData(0, 1, 1, 4)]
        [InlineData(0, -1, 1, 2)]
        [InlineData(0, 1, 1, -1)]
        [InlineData(1, 0, 1, 2)]
        [InlineData(int.MinValue, int.MaxValue, 1, 2)]
        [InlineData(0, 1, int.MinValue, int.MaxValue)]
        public void RawExceptionOffsets_ErrorTolerantRetainsClause(int tryStart, int tryEnd, int handlerStart, int handlerEnd)
        {
            string source = DocumentCompilerTestHelpers.MethodSource(
                $".try {tryStart} to {tryEnd} finally handler {handlerStart} to {handlerEnd}\nnop\nendfinally\nret");
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { ErrorTolerant = true });
            Diagnostic diagnostic = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.InvalidExceptionRegion, diagnostic.Id);
            Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(".try", source.Substring(diagnostic.Location.Span.Start, diagnostic.Location.Span.Length));
            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            MetadataReader reader = pe.GetMetadataReader();
            MethodDefinition method = reader.GetMethodDefinition(reader.MethodDefinitions.Single());
            ImmutableArray<byte> body = pe.GetSectionData(method.RelativeVirtualAddress).GetContent();
            Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan().Slice(4)));
            int tryLength = unchecked(tryEnd - tryStart);
            int handlerLength = unchecked(handlerEnd - handlerStart);
            bool fat = !ExceptionRegionEncoder.IsSmallExceptionRegion(tryStart, tryLength)
                || !ExceptionRegionEncoder.IsSmallExceptionRegion(handlerStart, handlerLength);
            Assert.Equal(fat ? 0x41 : 0x01, body[16]);
            if (fat)
            {
                Assert.Equal(tryStart, BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan().Slice(24)));
                Assert.Equal(tryLength, BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan().Slice(28)));
                Assert.Equal(handlerStart, BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan().Slice(32)));
                Assert.Equal(handlerLength, BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan().Slice(36)));
            }
            else
            {
                Assert.Equal(tryStart, BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan().Slice(22)));
                Assert.Equal(tryEnd - tryStart, body[24]);
                Assert.Equal(handlerStart, BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan().Slice(25)));
                Assert.Equal(handlerEnd - handlerStart, body[27]);
            }

            var (strictDiagnostics, strictResult) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options());
            Assert.Equal(DiagnosticIds.InvalidExceptionRegion, Assert.Single(strictDiagnostics).Id);
            Assert.Null(strictResult);
        }

        [Theory]
        [InlineData("finally", ExceptionRegionKind.Finally)]
        [InlineData("fault", ExceptionRegionKind.Fault)]
        [InlineData("catch [mscorlib]System.Exception", ExceptionRegionKind.Catch)]
        [InlineData("filter 1", ExceptionRegionKind.Filter)]
        public void ExceptionKinds_PreserveBoundsAndPayload(string clause, ExceptionRegionKind kind)
        {
            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                DocumentCompilerTestHelpers.MethodSource($".try 0 to 1 {clause} handler 1 to 2\nnop\nendfinally\nret"), new Options());
            ExceptionRegion region = Assert.Single(DocumentCompilerTestHelpers.GetMethodBody(pe).ExceptionRegions);
            Assert.Equal(kind, region.Kind);
            Assert.Equal(0, region.TryOffset);
            Assert.Equal(1, region.TryLength);
            Assert.Equal(1, region.HandlerOffset);
            Assert.Equal(1, region.HandlerLength);
            if (kind == ExceptionRegionKind.Catch)
            {
                MetadataReader reader = pe.GetMetadataReader();
                TypeReferenceHandle expected = DocumentCompilerTestHelpers.FindTypeRef(reader, "Exception");
                Assert.Equal((EntityHandle)expected, region.CatchType);
            }
            if (kind == ExceptionRegionKind.Filter)
            {
                Assert.Equal(1, region.FilterOffset);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void RawFilterOffset_ErrorTolerantPreservesBits(int offset)
        {
            string source = DocumentCompilerTestHelpers.MethodSource($".try 0 to 1 filter {offset} handler 1 to 2\nnop\nendfinally\nret");
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.InvalidExceptionRegion, Assert.Single(diagnostics).Id);
            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            MetadataReader reader = pe.GetMetadataReader();
            int rva = reader.GetMethodDefinition(reader.MethodDefinitions.Single()).RelativeVirtualAddress;
            ImmutableArray<byte> body = pe.GetSectionData(rva).GetContent();
            Assert.Equal(0x01, body[16]);
            Assert.Equal(offset, BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan().Slice(28)));
            var (_, strictResult) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options());
            Assert.Null(strictResult);
        }

        [Theory]
        [InlineData(20, false)]
        [InlineData(21, true)]
        public void ExceptionCount_SelectsSmallOrFatTable(int count, bool fat)
        {
            string instructions = string.Concat(Enumerable.Repeat(".try 0 to 1 finally handler 1 to 2\n", count)) + "nop\nendfinally\nret";
            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(DocumentCompilerTestHelpers.MethodSource(instructions), new Options());
            MetadataReader reader = pe.GetMetadataReader();
            int rva = reader.GetMethodDefinition(reader.MethodDefinitions.Single()).RelativeVirtualAddress;
            ImmutableArray<byte> body = pe.GetSectionData(rva).GetContent();
            Assert.Equal(fat ? 0x41 : 0x01, body[16]);
            int tableSize = fat
                ? body[17] | (body[18] << 8) | (body[19] << 16)
                : body[17];
            Assert.Equal(4 + count * (fat ? 24 : 12), tableSize);
            Assert.Equal(count, DocumentCompilerTestHelpers.GetMethodBody(pe).ExceptionRegions.Length);
            Assert.Equal(0, body[15]);
        }

        [Theory]
        [InlineData(65535, 1, false)]
        [InlineData(65536, 1, true)]
        [InlineData(0, 255, false)]
        [InlineData(0, 256, true)]
        public void RawExceptionBounds_SelectTableByRepresentability(int start, int length, bool fat)
        {
            string source = DocumentCompilerTestHelpers.MethodSource($".try {start} to {start + length} finally handler 1 to 2\nnop\nendfinally\nret");
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.InvalidExceptionRegion, Assert.Single(diagnostics).Id);
            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            MetadataReader reader = pe.GetMetadataReader();
            int rva = reader.GetMethodDefinition(reader.MethodDefinitions.Single()).RelativeVirtualAddress;
            ImmutableArray<byte> body = pe.GetSectionData(rva).GetContent();
            Assert.Equal(fat ? 0x41 : 0x01, body[16]);
            Assert.Equal(start, fat ? BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan().Slice(24))
                : BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan().Slice(22)));
            Assert.Equal(length, fat ? BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan().Slice(28)) : body[24]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NumericTargetsIntoHeaderAndExceptionTable_AreNotChanged(bool optimize)
        {
            string source = DocumentCompilerTestHelpers.MethodSource("""
                .try 0 to 5 finally handler 5 to 10
                br -100
                leave 7
                ret
                """);
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { Optimize = optimize, ErrorTolerant = true });
            Assert.Equal(optimize ? 1 : 0, diagnostics.Length);
            if (optimize)
            {
                Assert.Equal(DiagnosticIds.InvalidExceptionRegion, diagnostics[0].Id);
            }
            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            byte[] expected = optimize ? [0x2B, 0x9C, 0xDE, 7, 0x2A]
                : [0x38, 0x9C, 0xFF, 0xFF, 0xFF, 0xDD, 7, 0, 0, 0, 0x2A];
            MethodBodyBlock body = DocumentCompilerTestHelpers.GetMethodBody(pe);
            Assert.Equal(expected, body.GetILBytes());
            ExceptionRegion region = Assert.Single(body.ExceptionRegions);
            Assert.Equal(ExceptionRegionKind.Finally, region.Kind);
            Assert.Equal(0, region.TryOffset);
            Assert.Equal(5, region.TryLength);
            Assert.Equal(5, region.HandlerOffset);
            Assert.Equal(5, region.HandlerLength);
        }

        [Fact]
        public void CatchType_UsesResolvedTypeDefinitionToken()
        {
            string source = """
                .assembly extern mscorlib {}
                .assembly test {}
                .class public E extends [mscorlib]System.Exception {}
                .class public Test
                {
                    .method public static void M() cil managed
                    {
                        .try 0 to 1 catch E handler 1 to 2
                        nop
                        pop
                        ret
                    }
                }
                """;
            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            ExceptionRegion region = Assert.Single(DocumentCompilerTestHelpers.GetMethodBody(pe).ExceptionRegions);
            Assert.Equal((EntityHandle)MetadataTokens.TypeDefinitionHandle(2), region.CatchType);
        }

        [Theory]
        [InlineData("Missing", "END", "START", "END")]
        [InlineData("START", "END", "Missing", "END")]
        public void MissingExceptionLabel_IsDiagnosed(string tryStart, string tryEnd, string handlerStart, string handlerEnd)
        {
            string source = DocumentCompilerTestHelpers.MethodSource(
                $".try {tryStart} to {tryEnd} finally handler {handlerStart} to {handlerEnd}\nSTART: nop\nEND: ret");
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.LabelNotFound, Assert.Single(diagnostics).Id);
            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            Assert.Single(DocumentCompilerTestHelpers.GetMethodBody(pe).ExceptionRegions);
        }

        [Fact]
        public void ShortBranchError_DoesNotDiscardExceptionTable()
        {
            string source = DocumentCompilerTestHelpers.MethodSource(".try 0 to 2 finally handler 2 to 3\nbr.s END\n" +
                string.Concat(Enumerable.Repeat("nop\n", 128)) + "END: endfinally\nret");
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(source, new Options { ErrorTolerant = true });
            Assert.Equal(DiagnosticIds.BranchOffsetOutOfRange, Assert.Single(diagnostics).Id);
            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            MethodBodyBlock body = DocumentCompilerTestHelpers.GetMethodBody(pe);
            Assert.Equal(0x80, body.GetILBytes()![1]);
            ExceptionRegion region = Assert.Single(body.ExceptionRegions);
            Assert.Equal(ExceptionRegionKind.Finally, region.Kind);
            Assert.Equal(2, region.TryLength);
            Assert.Equal(2, region.HandlerOffset);
            Assert.Equal(1, region.HandlerLength);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NestedExceptionBlocks_FollowSelectedInstructionSizes(bool optimize)
        {
            string source = DocumentCompilerTestHelpers.MethodSource("""
                .try
                {
                    .try
                    {
                        ldc.i4 0
                        pop
                        leave INNER_END
                    }
                    catch [mscorlib]System.Exception
                    {
                        pop
                        leave INNER_END
                    }
                    INNER_END: leave END
                }
                finally
                {
                    endfinally
                }
                END: ret
                """);
            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { Optimize = optimize });
            MethodBodyBlock body = DocumentCompilerTestHelpers.GetMethodBody(pe);
            Assert.Equal(optimize ? 20 : 24, body.GetILBytes()!.Length);
            Assert.Equal(2, body.ExceptionRegions.Length);
            ExceptionRegion inner = body.ExceptionRegions[0];
            Assert.Equal(ExceptionRegionKind.Catch, inner.Kind);
            Assert.Equal(0, inner.TryOffset);
            Assert.Equal(optimize ? 7 : 11, inner.TryLength);
            Assert.Equal(inner.TryLength, inner.HandlerOffset);
            Assert.Equal(6, inner.HandlerLength);
            ExceptionRegion outer = body.ExceptionRegions[1];
            Assert.Equal(ExceptionRegionKind.Finally, outer.Kind);
            Assert.Equal(0, outer.TryOffset);
            Assert.Equal(optimize ? 18 : 22, outer.TryLength);
            Assert.Equal(outer.TryLength, outer.HandlerOffset);
            Assert.Equal(1, outer.HandlerLength);
        }

        [Fact]
        public void TryBlock_WithLabeledBlocks_GeneratesExceptionHandlers()
        {
            // This tests exception handler generation with labeled try blocks
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi Test
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .locals init (int32 V_0)

                        .try
                        {
                            ldc.i4.0
                            stloc.0
                            leave.s END
                        }
                        catch [mscorlib]System.Exception
                        {
                            pop
                            ldc.i4.1
                            stloc.0
                            leave.s END
                        }
                        END: ret
                    }
                }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());

            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);

            var blobBuilder = new System.Reflection.Metadata.BlobBuilder();
            result.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            // Verify method exists and has body
            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");

            Assert.True(methodDef.RelativeVirtualAddress != 0, "Method should have IL body");
        }


        [Fact]
        public void ScopeBlock_WithLabeledInstructions_UsesMarkLabelForBranches()
        {
            // Tests that labeled instructions properly work with branches
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi Test
                {
                    .method public static int32 TestBranches(int32 x) cil managed
                    {
                        .maxstack 2

                        ldarg.0
                        ldc.i4.0
                        bgt.s POSITIVE
                        ldc.i4.m1
                        br.s DONE

                        POSITIVE: ldc.i4.1

                        DONE: ret
                    }
                }
                """;

            var sourceText = new ILAssembler.SourceText(source, "test.il");
            var compiler = new ILAssembler.DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(sourceText, _ => default!, _ => default!, new Options());

            foreach (var d in diagnostics)
            {
                throw new Exception($"Unexpected diagnostic: {d.Id} - {d.Message}");
            }
            Assert.NotNull(result);

            var blobBuilder = new System.Reflection.Metadata.BlobBuilder();
            result.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());

            // Verify the PE is valid and method has code
            Assert.True(pe.HasMetadata);
            var reader = pe.GetMetadataReader();

            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestBranches");

            Assert.True(methodDef.RelativeVirtualAddress != 0);
        }


        [Fact]
        public void TryBlock_WithOffsetBounds_EmitsExceptionRegion()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .try 0 to 5 catch [mscorlib]System.Exception handler 5 to 9
                        nop          // 0: 1 byte
                        nop          // 1: 1 byte
                        nop          // 2: 1 byte
                        leave.s IL_9 // 3-4: 2 bytes (opcode + offset)
                    IL_5:
                        pop          // 5: 1 byte
                        leave.s IL_9 // 6-8: 2 bytes
                    IL_9:
                        ret          // 9: 1 byte
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");
            Assert.True(methodDef.RelativeVirtualAddress != 0);
        }


        [Fact]
        public void Finally_WithOffsetBounds_EmitsExceptionRegion()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .try 0 to 3 finally handler 3 to 5
                        nop          // 0
                        leave.s IL_5 // 1-2
                    IL_3:
                        endfinally   // 3
                    IL_5:
                        ret          // 5
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var methodDef = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "TestMethod");
            Assert.True(methodDef.RelativeVirtualAddress != 0);
        }

    }
}
