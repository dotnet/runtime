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
        public void ValidateDeclaringType()
        {
            var reader = MetadataReaderTests.GetMetadataReader(Misc.Members);

            Assert.NotEmpty(reader.GetTypesWithEvents());
            Assert.NotEmpty(reader.GetTypesWithProperties());

            foreach (var typeDefHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeDefHandle);
                foreach (var nestedTypeHandle in typeDef.GetNestedTypes())
                {
                    var nestedType = reader.GetTypeDefinition(nestedTypeHandle);
                    Assert.Equal(typeDefHandle, nestedType.GetDeclaringType());
                }
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
    }
}
