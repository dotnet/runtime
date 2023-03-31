// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Emit
{
    internal sealed class AssemblyBuilderImpl : AssemblyBuilder
    {
        private bool _previouslySaved;
        private readonly AssemblyName _assemblyName;
        private readonly Assembly _coreAssembly;
        private ModuleBuilderImpl? _module;

        internal AssemblyBuilderImpl(AssemblyName name, Assembly coreAssembly, IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
        {
            ArgumentNullException.ThrowIfNull(name);

            name = (AssemblyName)name.Clone();

            if (string.IsNullOrEmpty(name.Name))
            {
                throw new ArgumentException(SR.Argument_NullOrEmptyAssemblyName);
            }

            _assemblyName = name;
            _coreAssembly = coreAssembly;

            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
                {
                    SetCustomAttribute(assemblyAttribute);
                }
            }
        }

        internal static AssemblyBuilderImpl DefinePersistedAssembly(AssemblyName name, Assembly coreAssembly, IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
                => new AssemblyBuilderImpl(name, coreAssembly, assemblyAttributes);

        private static void WritePEImage(Stream peStream, MetadataBuilder metadataBuilder, BlobBuilder ilBuilder)
        {
            // Create executable with the managed metadata from the specified MetadataBuilder.
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.Dll // Start off with a simple DLL
                );

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder);

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
        }

        internal void Save(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (_module == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_AModuleRequired);
            }

            if (_previouslySaved) // Cannot save an assembly multiple times. This is consistent with Save() in .Net Framework.
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotSaveMultipleTimes);
            }

            // Add assembly metadata
            var metadata = new MetadataBuilder();

            metadata.AddAssembly(
               metadata.GetOrAddString(value: _assemblyName.Name!),
               version: _assemblyName.Version ?? new Version(0, 0, 0, 0),
               culture: _assemblyName.CultureName == null ? default : metadata.GetOrAddString(value: _assemblyName.CultureName),
               publicKey: _assemblyName.GetPublicKey() is byte[] publicKey ? metadata.GetOrAddBlob(value: publicKey) : default,
               flags: AddContentType((AssemblyFlags)_assemblyName.Flags, _assemblyName.ContentType),
#pragma warning disable SYSLIB0037 // Type or member is obsolete
               hashAlgorithm: (AssemblyHashAlgorithm)_assemblyName.HashAlgorithm
#pragma warning restore SYSLIB0037
               );

            // Add module's metadata
            _module.AppendMetadata(metadata);

            var ilBuilder = new BlobBuilder();
            WritePEImage(stream, metadata, ilBuilder);
            _previouslySaved = true;
        }

        private static AssemblyFlags AddContentType(AssemblyFlags flags, AssemblyContentType contentType)
            => (AssemblyFlags)((int)contentType << 9) | flags;

        internal void Save(string assemblyFileName)
        {
            ArgumentNullException.ThrowIfNull(assemblyFileName);

            using var peStream = new FileStream(assemblyFileName, FileMode.Create, FileAccess.Write);
            Save(peStream);
        }

        protected override ModuleBuilder DefineDynamicModuleCore(string name)
        {
            if (_module != null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_NoMultiModuleAssembly);
            }

            _module = new ModuleBuilderImpl(name, _coreAssembly);
            return _module;
        }

        protected override ModuleBuilder? GetDynamicModuleCore(string name)
        {
            if (_module != null && _module.ScopeName.Equals(name))
            {
                return _module;
            }

            return null;
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();

        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();
    }
}
