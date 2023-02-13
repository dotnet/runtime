// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Metadata.Experiment
{
    internal sealed class PersistableAssemblyBuilder : AssemblyBuilder
    {
        private bool _previouslySaved;
        private AssemblyName _assemblyName;

        #region Internal Data Members

        internal readonly AssemblyBuilderAccess _access;
        private PersistableModuleBuilder? _module;

        #endregion

        internal PersistableAssemblyBuilder(AssemblyName name,
                                 AssemblyBuilderAccess access)
        {
            ArgumentNullException.ThrowIfNull(name);

            _assemblyName = name;
            _access = access;
        }

        public static new PersistableAssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
            => new PersistableAssemblyBuilder(name, access);

        public static new AssemblyBuilder DefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            IEnumerable<CustomAttributeBuilder>? _)
                => new PersistableAssemblyBuilder(name, access);

        private static void WritePEImage(Stream peStream, MetadataBuilder metadataBuilder, BlobBuilder ilBuilder) // MethodDefinitionHandle entryPointHandle when we have main method.
        {
            //Create executable with the managed metadata from the specified MetadataBuilder.
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.Dll //Start off with a simple DLL
                );

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                flags: CorFlags.ILOnly,
                deterministicIdProvider: content => new BlobContentId(Guid.NewGuid(), 0x04030201));//Const ID, will reexamine as project progresses.

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            BlobContentId contentId = peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
        }

        public void Save(string assemblyFileName)
        {
            if (_previouslySaved) // You cannot save an assembly multiple times. This is consistent with Save() in .Net Framework.
            {
                throw new InvalidOperationException("Cannot save an assembly multiple times");
            }

            ArgumentNullException.ThrowIfNull(assemblyFileName);

            if (_assemblyName == null || _assemblyName.Name == null)
            {
                throw new InvalidOperationException();
            }

            if (_module == null)
            {
                throw new InvalidOperationException("Assembly needs at least one module defined");
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

            using var peStream = new FileStream(assemblyFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            var ilBuilder = new BlobBuilder();
            WritePEImage(peStream, metadata, ilBuilder);
            _previouslySaved = true;
        }

        protected override ModuleBuilder DefineDynamicModuleCore(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (_module != null)
            {
                throw new InvalidOperationException("Multi-module assemblies are not supported");
            }

            PersistableModuleBuilder moduleBuilder = new PersistableModuleBuilder(name, this);
            _module = moduleBuilder;
            return moduleBuilder;
        }
        protected override ModuleBuilder? GetDynamicModuleCore(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

#pragma warning disable IL3002 // Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file
            if (_module == null)
            {
                return null;
            }

            else if (_module.Name.Equals(name))
            {
                return _module;
            }
#pragma warning restore IL3002 // Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file

            return null;
        }
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();
    }
}
