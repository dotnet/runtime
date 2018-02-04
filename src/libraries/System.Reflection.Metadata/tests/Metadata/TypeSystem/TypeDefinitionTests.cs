// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
