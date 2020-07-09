// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderAssemblyQualifiedName
    {
        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [InlineData("TypeName", "ad df df")]
        [InlineData("TypeName", "assemblyname")]
        [InlineData("type name  ", "assembly name  ")]
        [InlineData("-", "assembly name  ")]
        public void AssemblyQualifiedName(string typeName, string assemblyName)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic, typeName: typeName, assemblyName: assemblyName);
            Assert.Equal($"{typeName}, {assemblyName.Trim()}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", type.AssemblyQualifiedName);
        }
    }
}
