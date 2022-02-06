// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Emit
{
    public sealed class RuntimeAssemblyBuilder : AssemblyBuilder
    {
        private RuntimeAssemblyBuilder()
        {
            // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.metadata.ecma335.metadatabuilder

        var peHeaderBuilder = new PEHeaderBuilder(
        imageCharacteristics: Characteristics.ExecutableImage
        );

peHeaderBuilder.ToString();

        }

        public override ModuleBuilder DefineDynamicModule(string name) => DefineDynamicModule(name);
        public override ModuleBuilder? GetDynamicModule(string name) => GetDynamicModule(name);

        public override void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute) => SetCustomAttribute(con, binaryAttribute);
        public override void SetCustomAttribute(CustomAttributeBuilder customBuilder) => SetCustomAttribute(customBuilder);
    }
}
