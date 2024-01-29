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
        private readonly AssemblyName _assemblyName;
        private readonly Assembly _coreAssembly;
        private readonly MetadataBuilder _metadataBuilder;
        private ModuleBuilderImpl? _module;
        private bool _previouslySaved;

        internal List<CustomAttributeWrapper>? _customAttributes;

        internal AssemblyBuilderImpl(AssemblyName name, Assembly coreAssembly, IEnumerable<CustomAttributeBuilder>? assemblyAttributes = null)
        {
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

        protected override void SaveCore(Stream stream)
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

            var ilBuilder = new BlobBuilder();
            var fieldDataBuilder = new BlobBuilder();
            MethodBodyStreamEncoder methodBodyEncoder = new MethodBodyStreamEncoder(ilBuilder);
            _module.AppendMetadata(methodBodyEncoder, fieldDataBuilder);

            WritePEImage(stream, ilBuilder, fieldDataBuilder);
            _previouslySaved = true;
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
