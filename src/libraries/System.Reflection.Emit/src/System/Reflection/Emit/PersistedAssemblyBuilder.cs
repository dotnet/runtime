// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace System.Reflection.Emit
{
    /// <summary>
    /// AssemblyBuilder implementation that can persist assembly to a disk or stream.
    /// </summary>
    public sealed class PersistedAssemblyBuilder : AssemblyBuilder
    {
        private readonly AssemblyName _assemblyName;
        private readonly Assembly _coreAssembly;
        private readonly MetadataBuilder _metadataBuilder;
        private ModuleBuilderImpl? _module;
        private bool _isMetadataPopulated;

        internal List<CustomAttributeWrapper>? _customAttributes;

        /// <summary>
        /// Creates a <see cref="PersistedAssemblyBuilder"/> instance that can be saved to a file or stream.
        /// </summary>
        /// <param name="name">The name of the assembly.</param>
        /// <param name="coreAssembly">The assembly that denotes the "system assembly" that houses the well-known types such as <see cref="object"/></param>
        /// <param name="assemblyAttributes">A collection that contains the attributes of the assembly.</param>
        /// <returns>An <see cref="PersistedAssemblyBuilder"/> that can be persisted.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> or <paramref name="name.Name"/> or <paramref name="coreAssembly"/> is null.</exception>
        /// <remarks>Currently the persisted assembly doesn't support running, need to save it and load back to run.</remarks>
        public PersistedAssemblyBuilder(AssemblyName name, Assembly coreAssembly, IEnumerable<CustomAttributeBuilder>? assemblyAttributes = null)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentException.ThrowIfNullOrEmpty(name.Name, "AssemblyName.Name");
            ArgumentNullException.ThrowIfNull(coreAssembly);

            _assemblyName = (AssemblyName)name.Clone();
            _coreAssembly = coreAssembly;
            _metadataBuilder = new MetadataBuilder();

            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
                {
                    SetCustomAttribute(assemblyAttribute);
                }
            }
        }

        private void WritePEImage(Stream peStream, BlobBuilder ilBuilder, BlobBuilder fieldData)
        {
            var peHeaderBuilder = new PEHeaderBuilder(
                // For now only support DLL, DLL files are considered executable files
                // for almost all purposes, although they cannot be directly run.
                imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll);

            var peBuilder = new ManagedPEBuilder(
                header: peHeaderBuilder,
                metadataRootBuilder: new MetadataRootBuilder(_metadataBuilder),
                ilStream: ilBuilder,
                mappedFieldData: fieldData,
                strongNameSignatureSize: 0);

            // Write executable into the specified stream.
            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob);
            peBlob.WriteContentTo(peStream);
        }

        /// <summary>
        /// Serializes the assembly to <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to which the assembly serialized.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A module not defined for the assembly.</exception>
        /// <exception cref="InvalidOperationException">The metadata already populated for the assembly before.</exception>
        public void Save(Stream stream) => SaveInternal(stream);

        /// <summary>
        /// Saves the assembly to disk.
        /// </summary>
        /// <param name="assemblyFileName">The file name of the assembly.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyFileName"/> is null.</exception>
        /// <exception cref="InvalidOperationException">A module not defined for the assembly.</exception>
        /// <exception cref="InvalidOperationException">The metadata already populated for the assembly before.</exception>
        public void Save(string assemblyFileName)
        {
            ArgumentNullException.ThrowIfNull(assemblyFileName);

            using var peStream = new FileStream(assemblyFileName, FileMode.Create, FileAccess.Write);
            SaveInternal(peStream);
        }

        private void SaveInternal(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            PopulateAssemblyMetadata(out BlobBuilder ilStream, out BlobBuilder fieldData, out _);
            WritePEImage(stream, ilStream, fieldData);
        }

        /// <summary>
        /// Generates the metadata for the <see cref="PersistedAssemblyBuilder"/>.
        /// </summary>
        /// <param name="ilStream">Outputs <see cref="BlobBuilder"/> bytes that includes all method's IL (body) emitted.</param>
        /// <param name="mappedFieldData">Outputs <see cref="BlobBuilder"/> bytes that includes all field RVA data defined in the assembly.</param>
        /// <returns>A <see cref="MetadataBuilder"/> that includes all members defined in the Assembly.</returns>
        /// <exception cref="InvalidOperationException">A module not defined for the assembly.</exception>
        /// <exception cref="InvalidOperationException">The metadata already populated for the assembly previously.</exception>
        [CLSCompliant(false)]
        public MetadataBuilder GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder mappedFieldData)
        {
            PopulateAssemblyMetadata(out ilStream, out mappedFieldData, out _);

            return _metadataBuilder;
        }

        /// <summary>
        /// Generates the metadata for the <see cref="PersistedAssemblyBuilder"/>.
        /// </summary>
        /// <param name="ilStream">Outputs <see cref="BlobBuilder"/> bytes that includes all method's IL (body) emitted.</param>
        /// <param name="mappedFieldData">Outputs <see cref="BlobBuilder"/> bytes that includes all field RVA data defined in the assembly.</param>
        /// <param name="pdbBuilder">Outputs <see cref="MetadataBuilder"/> that includes PDB metadata.</param>
        /// <returns>A <see cref="MetadataBuilder"/> that includes all members defined in the Assembly.</returns>
        /// <exception cref="InvalidOperationException">A module not defined for the assembly.</exception>
        /// <exception cref="InvalidOperationException">The metadata already populated for the assembly previously.</exception>
        [CLSCompliant(false)]
        public MetadataBuilder GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder mappedFieldData, out MetadataBuilder pdbBuilder)
        {
            PopulateAssemblyMetadata(out ilStream, out mappedFieldData, out pdbBuilder);

            return _metadataBuilder;
        }

        private void PopulateAssemblyMetadata(out BlobBuilder ilStream, out BlobBuilder fieldData, out MetadataBuilder pdbBuilder)
        {
            if (_module == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_AModuleRequired);
            }

            if (_isMetadataPopulated) // Cannot populate assembly metadata multiple times. This is consistent with Save() in .Net Framework.
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotPopulateMultipleTimes);
            }

            ilStream = new BlobBuilder();
            fieldData = new BlobBuilder();

            // Add assembly metadata
            AssemblyDefinitionHandle assemblyHandle = _metadataBuilder.AddAssembly(
               _metadataBuilder.GetOrAddString(value: _assemblyName.Name!),
               version: _assemblyName.Version ?? new Version(0, 0, 0, 0),
               culture: _assemblyName.CultureName == null ? default : _metadataBuilder.GetOrAddString(value: _assemblyName.CultureName),
               publicKey: _assemblyName.GetPublicKey() is byte[] publicKey ? _metadataBuilder.GetOrAddBlob(value: publicKey) : default,
               flags: AddContentType((AssemblyFlags)_assemblyName.Flags, _assemblyName.ContentType),
#pragma warning disable SYSLIB0037 // Type or member is obsolete
               hashAlgorithm: (AssemblyHashAlgorithm)_assemblyName.HashAlgorithm
#pragma warning restore SYSLIB0037
               );

            _module.WriteCustomAttributes(_customAttributes, assemblyHandle);
            _module.AppendMetadata(new MethodBodyStreamEncoder(ilStream), fieldData, out pdbBuilder);
            _isMetadataPopulated = true;
        }

        private static AssemblyFlags AddContentType(AssemblyFlags flags, AssemblyContentType contentType)
            => (AssemblyFlags)((int)contentType << 9) | flags;

        protected override ModuleBuilder DefineDynamicModuleCore(string name)
        {
            if (_module != null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_NoMultiModuleAssembly);
            }

            _module = new ModuleBuilderImpl(name, _coreAssembly, _metadataBuilder, this);
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

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }

        public override string? FullName => _assemblyName.FullName;

        public override Module ManifestModule => _module ?? throw new InvalidOperationException(SR.InvalidOperation_AModuleRequired);

        public override AssemblyName GetName(bool copiedName) => (AssemblyName)_assemblyName.Clone();
    }
}
