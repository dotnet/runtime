// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Internal.TypeSystem.Ecma
{
    public sealed partial class EcmaAssembly : EcmaModule, IAssemblyDesc
    {
        private AssemblyName _assemblyName;
        private AssemblyDefinition _assemblyDefinition;

        public AssemblyDefinition AssemblyDefinition
        {
            get
            {
                return _assemblyDefinition;
            }
        }

        public override IAssemblyDesc Assembly
        {
            get
            {
                return this;
            }
        }

        public EcmaAssembly(TypeSystemContext context, PEReader peReader, MetadataReader metadataReader, IModuleResolver customModuleResolver)
            : base(context, peReader, metadataReader, containingAssembly: null, customModuleResolver: customModuleResolver)
        {
            if (!metadataReader.IsAssembly)
            {
                ThrowHelper.ThrowBadImageFormatException();
            }

            _assemblyDefinition = metadataReader.GetAssemblyDefinition();
        }

        // Returns cached copy of the name. Caller has to create a clone before mutating the name.
        public AssemblyName GetName()
        {
            if (_assemblyName == null)
            {
                MetadataReader metadataReader = this.MetadataReader;

                AssemblyName an = new AssemblyName();
                an.Name = metadataReader.GetString(_assemblyDefinition.Name);
                an.Version = _assemblyDefinition.Version;
                an.SetPublicKey(metadataReader.GetBlobBytes(_assemblyDefinition.PublicKey));

                an.CultureName = metadataReader.GetString(_assemblyDefinition.Culture);
                an.ContentType = GetContentTypeFromAssemblyFlags(_assemblyDefinition.Flags);

                _assemblyName = an;
            }

            return _assemblyName;
        }

        public override string ToString()
        {
            return GetName().Name;
        }

        public bool HasAssemblyCustomAttribute(string attributeNamespace, string attributeName)
        {
            return !_metadataReader.GetCustomAttributeHandle(_assemblyDefinition.GetCustomAttributes(),
                attributeNamespace, attributeName).IsNil;
        }
    }
}
