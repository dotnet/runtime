// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Emit
{
    public sealed class AssemblyBuilderImpl : AssemblyBuilder
    {
        private bool _previouslySaved;
        private AssemblyName _assemblyName;
        private ModuleBuilderImpl? _module;

        internal AssemblyBuilderImpl(AssemblyName name, IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
        {
            ArgumentNullException.ThrowIfNull(name);

            _assemblyName = name;

            _module = new ModuleBuilderImpl(ModuleBuilderImpl.ManifestModuleName, this);

            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
                {
                    SetCustomAttribute(assemblyAttribute);
                }
            }
        }

        public static AssemblyBuilderImpl DefineDynamicAssembly(AssemblyName name)
            => new AssemblyBuilderImpl(name, null);

        public static AssemblyBuilderImpl DefineDynamicAssembly(
            AssemblyName name,
            IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
                => new AssemblyBuilderImpl(name, assemblyAttributes);

        private static void WritePEImage(Stream peStream, MetadataBuilder metadataBuilder, BlobBuilder ilBuilder) // MethodDefinitionHandle entryPointHandle when we have main method.
        {
            // Create executable with the managed metadata from the specified MetadataBuilder.
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.Dll // Start off with a simple DLL
                );

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: content => new BlobContentId(Guid.NewGuid(), 0x04030201)); // Const ID, will reexamine as project progresses.

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            BlobContentId contentId = peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
        }

        public void Save(Stream stream)
        {
            if (_previouslySaved) // Cannot save an assembly multiple times. This is consistent with Save() in .Net Framework.
            {
                throw new InvalidOperationException(SR.CannotSaveMultipleTimes);
            }

            ArgumentNullException.ThrowIfNull(stream);

            if (_assemblyName == null || _assemblyName.Name == null)
            {
                throw new InvalidOperationException();
            }

            if (_module == null)
            {
                throw new InvalidOperationException(SR.AModuleRequired);
            }

            // Add assembly metadata
            var metadata = new MetadataBuilder();
            metadata.AddAssembly( // Metadata is added for the new assembly - Current design - metadata generated only when Save method is called.
               metadata.GetOrAddString(value: _assemblyName.Name),
               version: _assemblyName.Version ?? new Version(0, 0, 0, 0),
               culture: _assemblyName.CultureName == null ? default : metadata.GetOrAddString(value: _assemblyName.CultureName),
               publicKey: _assemblyName.GetPublicKey() is byte[] publicKey ? metadata.GetOrAddBlob(value: publicKey) : default,
               flags: (AssemblyFlags)_assemblyName.Flags,
               hashAlgorithm: AssemblyHashAlgorithm.None); // AssemblyName.HashAlgorithm is obsolete so default value used.

            // Add module's metadata
            _module.AppendMetadata(metadata);

            var ilBuilder = new BlobBuilder();
            WritePEImage(stream, metadata, ilBuilder);
            _previouslySaved = true;
        }

        public void Save(string assemblyFileName)
        {
            ArgumentNullException.ThrowIfNull(assemblyFileName);

            using var peStream = new FileStream(assemblyFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            Save(peStream);
        }

        protected override ModuleBuilder DefineDynamicModuleCore(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (_module == null)
            {
                throw new InvalidOperationException(SR.ModuleNotFound);
            }

            return _module;
        }

        protected override ModuleBuilder? GetDynamicModuleCore(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (ModuleBuilderImpl.ManifestModuleName.Equals(name))
            {
                return _module;
            }

            return null;
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotSupportedException();

        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotSupportedException();
    }
}
