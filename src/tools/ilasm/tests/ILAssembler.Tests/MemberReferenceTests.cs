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
    public class MemberReferenceTests
    {
        [Fact]
        public void UnqualifiedGlobalMethodReference_ResolvesToMethodDefinition()
        {
            string source = """
                .assembly test { }
                .method public static int32 Helper() cil managed
                {
                    ldc.i4.s 100
                    ret
                }
                .class public auto ansi Program
                {
                    .method public static int32 Main() cil managed
                    {
                        call int32 Helper()
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Main", ILOpcode.call);
            EntityHandle handle = MetadataTokens.EntityHandle(token);

            Assert.Equal(HandleKind.MethodDefinition, handle.Kind);
            Assert.Equal("Helper", reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name));
        }

        [Fact]
        public void UnqualifiedGlobalFieldReference_ResolvesToFieldDefinition()
        {
            string source = """
                .assembly test { }
                .field public static int32 Value
                .class public auto ansi Program
                {
                    .method public static void Main() cil managed
                    {
                        ldsfld int32 Value
                        pop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Main", ILOpcode.ldsfld);

            DocumentCompilerTestHelpers.AssertFieldDefToken(reader, token, "Value");
        }

        [Fact]
        public void PrivateScopeDisplaySuffix_IsNotStoredInMemberNames()
        {
            string source = """
                .assembly test { }
                .field privatescope static int32 'Value$PST04000001'
                .method privatescope static void 'Helper$PST06000001'() cil managed
                {
                    ret
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));

            Assert.Equal("Value", reader.GetString(field.Name));
            Assert.Equal("Helper", reader.GetString(method.Name));
        }

        [Fact]
        public void PrivateScopeDisplaySuffix_DisambiguatesMethodReferences()
        {
            string source = """
                .assembly test { }
                .method privatescope static void 'Helper$PST06000001'() cil managed
                {
                    ret
                }
                .method privatescope static void 'Helper$PST06000002'() cil managed
                {
                    ret
                }
                .method public static void Caller() cil managed
                {
                    call void 'Helper$PST06000002'()
                    ret
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            int token = DocumentCompilerTestHelpers.GetFirstTokenOperand(pe, reader, "Caller", ILOpcode.call);

            Assert.Equal(MetadataTokens.GetToken(MetadataTokens.MethodDefinitionHandle(2)), token);
            Assert.All(reader.MethodDefinitions.Take(2), handle => Assert.Equal("Helper", reader.GetString(reader.GetMethodDefinition(handle).Name)));
        }

        [Fact]
        public void NonPrivateScopeMember_PreservesPrivateScopeLikeSuffix()
        {
            string source = """
                .assembly test { }
                .field public static int32 'Value$PST04000001'
                .method public static void 'Helper$PST06000001'() cil managed
                {
                    ret
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            var method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));

            Assert.Equal("Value$PST04000001", reader.GetString(field.Name));
            Assert.Equal("Helper$PST06000001", reader.GetString(method.Name));
        }

        [Fact]
        public void PrivateScopeVarArgReference_PreservesExplicitThisAndStripsDisplaySuffix()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method privatescope explicit instance vararg void 'Helper$PST06000001'(int32 value) cil managed
                    {
                        ret
                    }
                    .method public static void Caller() cil managed
                    {
                        ldnull
                        ldc.i4.0
                        ldstr ""
                        call explicit instance vararg void Test::'Helper$PST06000001'(int32, ..., string)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var memberReference = reader.GetMemberReference(Assert.Single(reader.MemberReferences));

            Assert.Equal(HandleKind.MethodDefinition, memberReference.Parent.Kind);
            Assert.Equal("Helper", reader.GetString(memberReference.Name));
            Assert.Equal(0x65, reader.GetBlobBytes(memberReference.Signature)[0]);
        }
    }
}
