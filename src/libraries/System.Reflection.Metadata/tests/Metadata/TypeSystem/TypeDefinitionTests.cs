// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class TypeDefinitionTests
    {
        [Fact]
        public void ValidateTypeDefinitionIsNestedNoProjection()
        {
            var reader = MetadataReaderTests.GetMetadataReader(Namespace.NamespaceTests, options: MetadataReaderOptions.None);

            foreach (var typeDefHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeDefHandle);

                Assert.Equal(typeDef.Attributes.IsNested(), typeDef.IsNested);
            }
        }

        [Fact]
        public void ValidateTypeDefinitionIsNestedWindowsProjection()
        {
            var reader = MetadataReaderTests.GetMetadataReader(Namespace.NamespaceTests, options: MetadataReaderOptions.ApplyWindowsRuntimeProjections);

            foreach (var typeDefHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeDefHandle);

                Assert.Equal(typeDef.Attributes.IsNested(), typeDef.IsNested);
            }
        }

        [Fact]
        public void ValidateMemberDeclaringType()
        {
            var reader = MetadataReaderTests.GetMetadataReader(Misc.Members);

            Assert.NotEmpty(reader.GetTypesWithEvents());
            Assert.NotEmpty(reader.GetTypesWithProperties());

            foreach (var typeDefHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeDefHandle);
                foreach (var fieldHandle in typeDef.GetFields())
                {
                    var field = reader.GetFieldDefinition(fieldHandle);
                    Assert.Equal(typeDefHandle, field.GetDeclaringType());
                }
                foreach (var methodHandle in typeDef.GetMethods())
                {
                    var method = reader.GetMethodDefinition(methodHandle);
                    Assert.Equal(typeDefHandle, method.GetDeclaringType());
                }
                foreach (var eventHandle in typeDef.GetEvents())
                {
                    var eventDef = reader.GetEventDefinition(eventHandle);
                    Assert.Equal(typeDefHandle, eventDef.GetDeclaringType());
                }
                foreach (var propertyHandle in typeDef.GetProperties())
                {
                    var property = reader.GetPropertyDefinition(propertyHandle);
                    Assert.Equal(typeDefHandle, property.GetDeclaringType());
                }
            }
        }

#if NET
        [ActiveIssue("https://github.com/dotnet/runtime/pull/111642", TestRuntimes.Mono)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
        public unsafe void ValidateTypeDeclaringType()
        {
            var asm = typeof(TypeDefinitionTests).Assembly;
            Assert.True(asm.TryGetRawMetadata(out byte* metadataBlob, out int length));
            var reader = new MetadataReader(metadataBlob, length);

            int nestedClassToken = typeof(TestNestedClass).MetadataToken;
            int parentClassToken = typeof(TypeDefinitionTests).MetadataToken;

            TypeDefinition nestedClassDef = reader.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.EntityHandle(nestedClassToken));
            Assert.Equal(MetadataTokens.EntityHandle(parentClassToken), nestedClassDef.GetDeclaringType());
        }

        private class TestNestedClass;
#endif
    }
}
