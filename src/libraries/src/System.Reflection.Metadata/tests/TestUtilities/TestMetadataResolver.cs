// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Metadata.Tests
{
    internal static class TestMetadataResolver
    {
        public static TypeDefinitionHandle FindTestType(MetadataReader reader, Type type)
        {
            if (type.DeclaringType == null)
            {
                foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
                {
                    TypeDefinition definition = reader.GetTypeDefinition(handle);
                    if (reader.StringComparer.Equals(definition.Namespace, type.Namespace) &&
                        reader.StringComparer.Equals(definition.Name, type.Name))
                    {
                        return handle;
                    }
                }
            }
            else
            {
                TypeDefinitionHandle declaringHandle = FindTestType(reader, type.DeclaringType);
                TypeDefinition declaringDefinition = reader.GetTypeDefinition(declaringHandle);
                foreach (TypeDefinitionHandle handle in declaringDefinition.GetNestedTypes())
                {
                    TypeDefinition definition = reader.GetTypeDefinition(handle);
                    if (reader.StringComparer.Equals(definition.Name, type.Name))
                    {
                        return handle;
                    }
                }
            }

            Assert.True(false, "Cannot find test type:" + type);
            return default(TypeDefinitionHandle);
        }
    }
}
