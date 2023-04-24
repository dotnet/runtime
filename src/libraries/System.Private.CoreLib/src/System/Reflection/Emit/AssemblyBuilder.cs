// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Reflection.Emit
{
    public abstract partial class AssemblyBuilder : Assembly
    {
        protected AssemblyBuilder()
        {
        }

        public ModuleBuilder DefineDynamicModule(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineDynamicModuleCore(name);
        }

        protected abstract ModuleBuilder DefineDynamicModuleCore(string name);

        public ModuleBuilder? GetDynamicModule(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return GetDynamicModuleCore(name);
        }

        protected abstract ModuleBuilder? GetDynamicModuleCore(string name);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder);
        }

        protected abstract void SetCustomAttributeCore(CustomAttributeBuilder customBuilder);

        [System.ObsoleteAttribute("Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location instead.", DiagnosticId = "SYSLIB0012", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public override string? CodeBase => throw new NotSupportedException(SR.NotSupported_DynamicAssembly);
        public override string Location => string.Empty;
        public override MethodInfo? EntryPoint => null;
        public override bool IsDynamic => true;

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetExportedTypes() =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);

        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public override FileStream GetFile(string name) =>
            throw new NotSupportedException(SR.NotSupported_DynamicAssembly);

        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
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

        internal static void EnsureDynamicCodeSupported()
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                ThrowDynamicCodeNotSupported();
            }
        }

        private static void ThrowDynamicCodeNotSupported() =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionEmit);
    }
}
