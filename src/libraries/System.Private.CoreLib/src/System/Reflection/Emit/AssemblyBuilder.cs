// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Reflection.Emit
{
    public abstract partial class AssemblyBuilder : Assembly
    {
        [ThreadStatic]
        private static bool t_allowDynamicCode;

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

        /// <summary>
        /// Defines an <see cref="AssemblyBuilder"/> that can be saved to a file or stream.
        /// </summary>
        /// <param name="name">The name of the assembly.</param>
        /// <param name="coreAssembly">The assembly that denotes the "system assembly" that houses the well-known types such as <see cref="object"/></param>
        /// <param name="assemblyAttributes">A collection that contains the attributes of the assembly.</param>
        /// <returns>An <see cref="AssemblyBuilder"/> that can be persisted.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> or <paramref name="name.Name"/> or <paramref name="coreAssembly"/> is null.</exception>
        /// <remarks>Currently the persisted assembly doesn't support running, need to save it and load back to run.</remarks>
        public static AssemblyBuilder DefinePersistedAssembly(AssemblyName name, Assembly coreAssembly, IEnumerable<CustomAttributeBuilder>? assemblyAttributes = null)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentException.ThrowIfNullOrEmpty(name.Name, "AssemblyName.Name");
            ArgumentNullException.ThrowIfNull(coreAssembly);

            Type assemblyType = Type.GetType("System.Reflection.Emit.AssemblyBuilderImpl, System.Reflection.Emit", throwOnError: true)!;
            ConstructorInfo con = assemblyType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(AssemblyName), typeof(Assembly), typeof(IEnumerable<CustomAttributeBuilder>)])!;
            return (AssemblyBuilder)con.Invoke([name, coreAssembly, assemblyAttributes]);
        }

        protected abstract ModuleBuilder? GetDynamicModuleCore(string name);

        /// <summary>
        /// Serializes the assembly to <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which the assembly serialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="NotSupportedException">The AssemblyBuilder instance doesn't support saving.</exception>
        public void Save(Stream stream) => SaveCore(stream);

        /// <summary>
        /// Saves the assembly to disk.
        /// </summary>
        /// <param name="assemblyFileName">The file name of the assembly.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyFileName"/> is null.</exception>
        /// <exception cref="NotSupportedException">The AssemblyBuilder instance doesn't support saving.</exception>
        public void Save(string assemblyFileName)
        {
            ArgumentNullException.ThrowIfNull(assemblyFileName);

            using var peStream = new FileStream(assemblyFileName, FileMode.Create, FileAccess.Write);
            SaveCore(peStream);
        }

        /// <summary>
        /// When implemented in a derived type, serializes the assembly to a stream.
        /// </summary>
        /// <param name="stream">The stream to which the assembly serialized.</param>
        protected virtual void SaveCore(Stream stream) => throw new NotSupportedException(SR.NotSupported_AssemblySave);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
        }

        [Obsolete("Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location instead.", DiagnosticId = "SYSLIB0012", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
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
            if (!RuntimeFeature.IsDynamicCodeSupported && !t_allowDynamicCode)
            {
                ThrowDynamicCodeNotSupported();
            }
        }

        /// <summary>
        /// Allows dynamic code even though RuntimeFeature.IsDynamicCodeSupported is false.
        /// </summary>
        /// <returns>An object that, when disposed, will revert allowing dynamic code back to its initial state.</returns>
        /// <remarks>
        /// This is useful for cases where RuntimeFeature.IsDynamicCodeSupported returns false, but
        /// the runtime is still capable of emitting dynamic code. For example, when generating delegates
        /// in System.Linq.Expressions while PublishAot=true is set in the project. At debug time, the app
        /// uses the non-AOT runtime with the IsDynamicCodeSupported feature switch set to false.
        /// </remarks>
        internal static IDisposable ForceAllowDynamicCode() => new ForceAllowDynamicCodeScope();

        private sealed class ForceAllowDynamicCodeScope : IDisposable
        {
            private readonly bool _previous;

            public ForceAllowDynamicCodeScope()
            {
                _previous = t_allowDynamicCode;
                t_allowDynamicCode = true;
            }

            public void Dispose()
            {
                t_allowDynamicCode = _previous;
            }
        }

        private static void ThrowDynamicCodeNotSupported() =>
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ReflectionEmit);
    }
}
