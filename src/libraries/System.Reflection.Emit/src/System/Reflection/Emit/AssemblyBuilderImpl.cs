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
            _metadataBuilder = new MetadataBuilder();

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

        private void WritePEImage(Stream peStream, BlobBuilder ilBuilder)
        {
            // Create executable with the managed metadata from the specified MetadataBuilder.
            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.Dll // Start off with a simple DLL
                );

            var peBuilder = new ManagedPEBuilder(
                peHeaderBuilder,
                new MetadataRootBuilder(_metadataBuilder),
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
            // Add module's metadata
            _module.AppendMetadata();

            var ilBuilder = new BlobBuilder();
            WritePEImage(stream, ilBuilder);
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

            _module = new ModuleBuilderImpl(name, _coreAssembly, _metadataBuilder);
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
    }
}
