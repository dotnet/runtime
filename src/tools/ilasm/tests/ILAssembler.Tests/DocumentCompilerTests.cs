// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ILAssembler.Tests
{
    public class DocumentCompilerTests
    {
        [Fact]
        public void SingleTypeNoMembers()
        {
            string source = """
                .class public auto ansi sealed beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // One for the <Module> type, one for Test.
            Assert.Equal(2, reader.TypeDefinitions.Count);
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal(string.Empty, reader.GetString(typeDef.Namespace));
            Assert.Equal("Test", reader.GetString(typeDef.Name));
        }

        private static PEReader CompileAndGetReader(string source, Options options)
        {
            var sourceText = new SourceText(source, "test.il");
            var documentCompiler = new DocumentCompiler();
            var (diagnostics, result) = documentCompiler.Compile(sourceText, _ =>
            {
                Assert.Fail("Expected no includes");
                return default;
            }, _ => { Assert.Fail("Expected no resources"); return default; }, options);
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            var blobBuilder = new BlobBuilder();
            result!.Serialize(blobBuilder);
            return new PEReader(blobBuilder.ToImmutableArray());
        }
    }
}
