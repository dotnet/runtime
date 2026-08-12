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
        public void TryBlock_WithOffsetLabels_UsesInstructionEncoderExtensions()
        {
            // Test offset-based labels in try/catch blocks (exercises InstructionEncoderExtensions.MarkLabel)
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
        public void ScopeBlock_WithOffsetLabels_UsesInstructionEncoderExtensions()
        {
            // Test offset-based scope blocks (exercises InstructionEncoderExtensions.MarkLabel)
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

        [Fact]
        public void OffsetBasedCatchRegion_EmitsExactExceptionRegionBounds()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .try 0 to 5 catch [mscorlib]System.Exception handler 5 to 8
                        nop
                        nop
                        nop
                        leave.s IL_8
                    IL_5:
                        pop
                        leave.s IL_8
                    IL_8:
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == "TestMethod");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var region = Assert.Single(body.ExceptionRegions);

            Assert.Equal(ExceptionRegionKind.Catch, region.Kind);
            Assert.Equal(0, region.TryOffset);
            Assert.Equal(5, region.TryLength);
            Assert.Equal(5, region.HandlerOffset);
            Assert.Equal(3, region.HandlerLength);
            Assert.Equal("Exception", reader.GetString(reader.GetTypeReference((TypeReferenceHandle)region.CatchType).Name));
        }

        [Fact]
        public void OffsetBasedFinallyRegion_EmitsExactExceptionRegionBounds()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .try 0 to 3 finally handler 3 to 4
                        nop
                        leave.s IL_4
                    IL_3:
                        endfinally
                    IL_4:
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == "TestMethod");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var region = Assert.Single(body.ExceptionRegions);

            Assert.Equal(ExceptionRegionKind.Finally, region.Kind);
            Assert.Equal(0, region.TryOffset);
            Assert.Equal(3, region.TryLength);
            Assert.Equal(3, region.HandlerOffset);
            Assert.Equal(1, region.HandlerLength);
        }

        [Fact]
        public void FilterHandler_EmitsFilterExceptionRegion()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .try
                        {
                            ldnull
                            throw
                        }
                        filter
                        {
                            pop
                            ldc.i4.1
                            endfilter
                        }
                        {
                            pop
                            leave.s DONE
                        }
                    DONE:
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(definition => reader.GetString(definition.Name) == "TestMethod");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var region = Assert.Single(body.ExceptionRegions);

            Assert.Equal(ExceptionRegionKind.Filter, region.Kind);
            Assert.True(region.TryLength > 0);
            Assert.True(region.FilterOffset >= 0);
            Assert.True(region.HandlerOffset >= 0);
            Assert.True(region.HandlerLength > 0);
        }

        [Fact]
        public void FaultHandler_EmitsFaultExceptionRegion()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .maxstack 1
                        .try
                        {
                            nop
                            leave.s DONE
                        }
                        fault
                        {
                            endfinally
                        }
                    DONE:
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(definition => reader.GetString(definition.Name) == "TestMethod");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var region = Assert.Single(body.ExceptionRegions);

            Assert.Equal(ExceptionRegionKind.Fault, region.Kind);
            Assert.True(region.TryLength > 0);
            Assert.True(region.HandlerOffset >= region.TryOffset + region.TryLength);
            Assert.True(region.HandlerLength > 0);
        }

        [Fact]
        public void CatchBlock_EmitsCatchExceptionRegion()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .maxstack 1
                        .try
                        {
                            nop
                            leave.s DONE
                        }
                        catch [mscorlib]System.Exception
                        {
                            pop
                            leave.s DONE
                        }
                    DONE:
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == "M");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var region = Assert.Single(body.ExceptionRegions);

            Assert.Equal(ExceptionRegionKind.Catch, region.Kind);
            Assert.True(region.TryLength > 0);
            Assert.True(region.HandlerLength > 0);
            Assert.Equal("Exception", reader.GetString(reader.GetTypeReference((TypeReferenceHandle)region.CatchType).Name));
        }

        [Fact]
        public void FinallyBlock_EmitsFinallyExceptionRegion()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .maxstack 1
                        .try
                        {
                            nop
                            leave.s DONE
                        }
                        finally
                        {
                            endfinally
                        }
                    DONE:
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == "M");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var region = Assert.Single(body.ExceptionRegions);

            Assert.Equal(ExceptionRegionKind.Finally, region.Kind);
            Assert.True(region.TryLength > 0);
            Assert.True(region.HandlerLength > 0);
        }

        [Fact]
        public void MultipleCatchClauses_ResolveCatchTypesBeforeTheirHandlerBodies()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .maxstack 1
                        .try
                        {
                            leave.s DONE
                        }
                        catch [mscorlib]System.ArgumentException
                        {
                            castclass [mscorlib]System.IO.Stream
                            pop
                            leave.s DONE
                        }
                        catch [mscorlib]System.NotSupportedException
                        {
                            castclass [mscorlib]System.Text.StringBuilder
                            pop
                            leave.s DONE
                        }
                    DONE:
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Native ilasm resolves a catch type as soon as the clause is parsed, so each catch type
            // precedes every type its handler body references.
            Assert.Equal(
                ["System.Object", "System.ValueType", "System.ArgumentException", "System.IO.Stream", "System.NotSupportedException", "System.Text.StringBuilder"],
                reader.TypeReferences
                    .Select(reader.GetTypeReference)
                    .Select(reference => reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name))
                    .ToArray());

            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == "M");
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);

            Assert.Equal(2, body.ExceptionRegions.Length);
            Assert.All(body.ExceptionRegions, region => Assert.Equal(ExceptionRegionKind.Catch, region.Kind));
            Assert.Equal(
                ["System.ArgumentException", "System.NotSupportedException"],
                body.ExceptionRegions
                    .Select(region => reader.GetTypeReference((TypeReferenceHandle)region.CatchType))
                    .Select(reference => reader.GetString(reference.Namespace) + "." + reader.GetString(reference.Name))
                    .ToArray());
        }

    }
}
