// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Reflection.Emit
{
    public sealed partial class AssemblyBuilder : Assembly
    {
        [RequiresAssemblyFiles(Message = "The code will throw for assemblies embedded in a single-file app")]
        public override string? CodeBase => throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        public override string Location => throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        public override MethodInfo? EntryPoint => null;
        public override bool IsDynamic => true;

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetExportedTypes() =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);

        public override FileStream GetFile(string name) =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);

        public override FileStream[] GetFiles(bool getResourceModules) =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);

        public override ManifestResourceInfo? GetManifestResourceInfo(string resourceName) =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);

        public override string[] GetManifestResourceNames() =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);

        public override Stream? GetManifestResourceStream(string name) =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);

        public override Stream? GetManifestResourceStream(Type type, string name) =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
    }
}
