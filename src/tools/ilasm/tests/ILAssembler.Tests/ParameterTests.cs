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
    public class ParameterTests
    {
        [Fact]
        public void Diagnostic_ArgumentNotFound()
        {
            // Reference an argument that doesn't exist
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }

                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public void TestMethod(int32 x) cil managed
                    {
                        ldarg NonExistentArg
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ArgumentNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_ParameterIndexOutOfRange()
        {
            // Referencing parameter index that doesn't exist
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod(int32 x) cil managed
                    {
                        .param [99]
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ParameterIndexOutOfRange, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void ParamInitOpt_Int32Constant_CreatesConstantEntry()
        {
            // Test that .param with int32 initOpt creates a constant entry
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void TestMethod(int32 x) cil managed
                    {
                        .param [1] = int32(42)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the method
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(m => reader.GetString(m.Name) == "TestMethod");

            // Get parameters
            var parameters = method.GetParameters().ToArray();
            Assert.True(parameters.Length >= 1, $"Expected at least 1 parameter, got {parameters.Length}");

            // Find the parameter by sequence number
            var param1 = parameters.Select(reader.GetParameter).FirstOrDefault(p => p.SequenceNumber == 1);
            var param1Handle = parameters.FirstOrDefault(h => reader.GetParameter(h).SequenceNumber == 1);
            Assert.False(param1Handle.IsNil, "Parameter with sequence 1 not found");

            // Check constant for first param (int32)
            var intConstantHandle = param1.GetDefaultValue();
            Assert.False(intConstantHandle.IsNil, "No constant for parameter 1");
            var intConstant = reader.GetConstant(intConstantHandle);
            Assert.Equal(ConstantTypeCode.Int32, intConstant.TypeCode);
            var intValue = reader.GetBlobReader(intConstant.Value).ReadInt32();
            Assert.Equal(42, intValue);
        }

        [Fact]
        public void ParamInitOpt_ReturnParam_CreatesConstantEntry()
        {
            // Test that .param [0] (return value) with initOpt works
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static int32 GetValue() cil managed
                    {
                        .param [0] = int32(100)
                        ldc.i4 100
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the method
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(m => reader.GetString(m.Name) == "GetValue");

            // Get parameters - param [0] is the return value
            var parameters = method.GetParameters().ToArray();
            Assert.Single(parameters);

            var param = reader.GetParameter(parameters[0]);
            Assert.Equal(0, param.SequenceNumber); // Return value has sequence 0

            var constantHandle = param.GetDefaultValue();
            Assert.False(constantHandle.IsNil);
            var constant = reader.GetConstant(constantHandle);
            Assert.Equal(ConstantTypeCode.Int32, constant.TypeCode);
            var value = reader.GetBlobReader(constant.Value).ReadInt32();
            Assert.Equal(100, value);
        }

        [Fact]
        public void NamedArgument_CanBeReferencedByLdarg()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M(int32 myArg) cil managed
                    {
                        ldarg myArg
                        pop
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ParamWithInAttribute_EmitsParamRow()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly Test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public hidebysig static void M([in] int32& x) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            int paramCount = reader.GetTableRowCount(TableIndex.Param);
            Assert.True(paramCount >= 1, "Should have at least one Param row for [in] parameter");

            var param = reader.GetParameter(MetadataTokens.ParameterHandle(1));
            Assert.True(param.Attributes.HasFlag(ParameterAttributes.In));
        }

        [Fact]
        public void UnnamedInstanceParam_EmitsParamRow()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public instance void .ctor(int32) cil managed
                    {
                        ldarg.0
                        call instance void [mscorlib]System.Object::.ctor()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Should have 1 Param row for the unnamed int32 parameter (sequence 1)
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.Param));
            var param = reader.GetParameter(MetadataTokens.ParameterHandle(1));
            Assert.Equal(1, param.SequenceNumber);
        }

        [Fact]
        public void LdargByName_CorrectIndexInLongMethod()
        {
            // Regression test for NaN comp32 IL corruption: ldarg.s by parameter name
            // emitted wrong index (0 instead of 3) after ~512 bytes of IL, causing
            // the IL body to be garbled from that point forward.
            // Generate enough instructions to cross the 512-byte IL boundary,
            // then verify ldarg.s with the 4th parameter name emits index 3.
            var sb = new StringBuilder();
            sb.AppendLine("""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Run(float32 a, float32 b, float32 c, float32 d) cil managed
                    {
                        .maxstack 8
                """);
            // Each block is ~18 bytes: ldarg.s(2) + ldarg.s(2) + ceq(2) + brfalse.s(2) + ldstr(5) + br(5)
            // 30 blocks = ~540 bytes, crossing the 512-byte boundary
            for (int i = 0; i < 30; i++)
            {
                sb.AppendLine($"        ldarg.s 'd'");
                sb.AppendLine($"        ldarg.s 'a'");
                sb.AppendLine($"        ceq");
                sb.AppendLine($"        brfalse.s LBL_{i}");
                sb.AppendLine($"        ldstr \"block {i}\"");
                sb.AppendLine($"        br DONE");
                sb.AppendLine($"        LBL_{i}:");
            }
            sb.AppendLine("""
                        DONE:
                        ret
                    }
                }
                """);

            string source = sb.ToString();
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Run");

            // Read the IL body and verify ldarg.s instructions have correct indices
            var body = pe.GetMethodBody(method.RelativeVirtualAddress);
            var ilBytes = body.GetILBytes()!;

            // Walk the IL and check all ldarg.s (0x0E) instructions
            int pos = 0;
            int ldargCount = 0;
            while (pos < ilBytes.Length)
            {
                byte op = ilBytes[pos];
                if (op == 0x0E) // ldarg.s
                {
                    byte argIndex = ilBytes[pos + 1];
                    ldargCount++;
                    // Odd ldarg.s (1st, 3rd, 5th...) should load 'd' = index 3
                    // Even ldarg.s (2nd, 4th, 6th...) should load 'a' = index 0
                    if (ldargCount % 2 == 1)
                    {
                        Assert.True(argIndex == 3, $"ldarg.s #{ldargCount} at IL offset {pos} should load 'd' (index 3) but got index {argIndex}");
                    }
                    else
                    {
                        Assert.True(argIndex == 0, $"ldarg.s #{ldargCount} at IL offset {pos} should load 'a' (index 0) but got index {argIndex}");
                    }
                    pos += 2;
                }
                else if (op == 0xFE) // two-byte opcode prefix
                {
                    pos += 2; // skip prefix + opcode
                }
                else if (op == 0x72) // ldstr
                {
                    pos += 5; // opcode + 4-byte token
                }
                else if (op == 0x38) // br
                {
                    pos += 5;
                }
                else if (op == 0x2C) // brfalse.s
                {
                    pos += 2;
                }
                else if (op == 0x2A) // ret
                {
                    pos += 1;
                }
                else
                {
                    pos += 1; // unknown, advance 1
                }
            }
            Assert.Equal(60, ldargCount); // 30 blocks * 2 ldarg.s each
        }

        [Fact]
        public void MultiDimArrayParam_PreservedAfterSignatureRewrite()
        {
            // Multi-dimensional array types in method signatures must survive
            // the TypeRef→TypeDef signature rewriting pass.
            // Regression: GetArrayType was missing the ELEMENT_TYPE_ARRAY (0x14) prefix byte.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(int32[0...,0...] arr) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var sigBytes = reader.GetBlobBytes(method.Signature);

            // Method sig: 0x00 (DEFAULT), 0x01 (1 param), 0x01 (void ret),
            // then ELEMENT_TYPE_ARRAY (0x14), ELEMENT_TYPE_I4 (0x08), shape...
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT
            Assert.Equal(0x01, sigBytes[1]); // 1 param
            Assert.Equal(0x01, sigBytes[2]); // void return
            Assert.Equal(0x14, sigBytes[3]); // ELEMENT_TYPE_ARRAY
            Assert.Equal(0x08, sigBytes[4]); // ELEMENT_TYPE_I4 (int32)
            Assert.Equal(0x02, sigBytes[5]); // rank = 2
        }

        [Fact]
        public void SZArrayParam_PreservedAfterSignatureRewrite()
        {
            // SZ arrays (char[], int32[]) must preserve their element type
            // through the signature rewriting pass.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(char[] chars, int32[] ints) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "M");
            var sigBytes = reader.GetBlobBytes(method.Signature);

            // Method sig: 0x00 (DEFAULT), 0x02 (2 params), 0x01 (void ret),
            // param 1: SZARRAY (0x1D) + CHAR (0x03)
            // param 2: SZARRAY (0x1D) + I4 (0x08)
            Assert.Equal(0x00, sigBytes[0]); // DEFAULT
            Assert.Equal(0x02, sigBytes[1]); // 2 params
            Assert.Equal(0x01, sigBytes[2]); // void return
            Assert.Equal(0x1D, sigBytes[3]); // ELEMENT_TYPE_SZARRAY
            Assert.Equal(0x03, sigBytes[4]); // ELEMENT_TYPE_CHAR
            Assert.Equal(0x1D, sigBytes[5]); // ELEMENT_TYPE_SZARRAY
            Assert.Equal(0x08, sigBytes[6]); // ELEMENT_TYPE_I4
        }
    }
}
