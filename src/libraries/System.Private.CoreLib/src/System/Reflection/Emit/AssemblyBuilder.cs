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

        // The following methods are abstract in reference assembly. We keep them as virtual to maintain backward compatibility.
        // They should be overriden in concrete AssemblyBuilder implementations. They should be only used for non-virtual calls
        // on the original non-abstract AssemblyBuilder. The implementation of these methods simply forwards to the overriden virtual method
        // with actual implementation.

        public virtual ModuleBuilder DefineDynamicModule(string name) => DefineDynamicModule(name);
        public virtual ModuleBuilder? GetDynamicModule(string name) => GetDynamicModule(name);

        public virtual void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute) => SetCustomAttribute(con, binaryAttribute);
        public virtual void SetCustomAttribute(CustomAttributeBuilder customBuilder) => SetCustomAttribute(customBuilder);

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
