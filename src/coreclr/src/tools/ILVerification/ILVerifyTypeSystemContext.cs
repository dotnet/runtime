// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILVerify
{
    class ILVerifyTypeSystemContext : MetadataTypeSystemContext
    {
        internal readonly IResolver _resolver;

        private RuntimeInterfacesAlgorithm _arrayOfTRuntimeInterfacesAlgorithm;
        private MetadataRuntimeInterfacesAlgorithm _metadataRuntimeInterfacesAlgorithm = new MetadataRuntimeInterfacesAlgorithm();
        private MetadataVirtualMethodAlgorithm _metadataVirtualMethodAlgorithm = new MetadataVirtualMethodAlgorithm();

        private readonly Dictionary<PEReader, EcmaModule> _modulesCache = new Dictionary<PEReader, EcmaModule>();

        public ILVerifyTypeSystemContext(IResolver resolver)
        {
            _resolver = resolver;
        }

        public override ModuleDesc ResolveAssembly(AssemblyName name, bool throwIfNotFound = true)
        {
            // Note: we use simple names instead of full names to resolve, because we can't get a full name from an assembly without reading it
            string simpleName = name.Name;
            return ResolveAssemblyOrNetmodule(simpleName, simpleName, null, throwIfNotFound);
        }

        internal override ModuleDesc ResolveModule(IAssemblyDesc referencingModule, string fileName, bool throwIfNotFound = true)
        {
            // Referenced modules are stored without their extension (see CommandLineHelpers.cs), so we have to drop
            // the extension here as well to find a match.
            string simpleName = Path.GetFileNameWithoutExtension(fileName);
            // The referencing module is not getting verified currently.
            // However, netmodules are resolved in the context of assembly, not in the global context.
            EcmaModule module = ResolveAssemblyOrNetmodule(simpleName, fileName, referencingModule as IAssemblyDesc, throwIfNotFound);
            if (module.MetadataReader.IsAssembly)
            {
                throw new VerifierException($"The module '{fileName}' is not expected to be an assembly");
            }
            return module;
        }

        private EcmaModule ResolveAssemblyOrNetmodule(string simpleName, string verificationName, IAssemblyDesc containingAssembly, bool throwIfNotFound)
        {
            PEReader peReader = _resolver.Resolve(simpleName);
            if (peReader == null && throwIfNotFound)
            {
                throw new VerifierException("Assembly or module not found: " + simpleName);
            }
            var module = GetModule(peReader, containingAssembly);
            VerifyModuleName(verificationName, module);
            return module;
        }

        private static void VerifyModuleName(string simpleName, EcmaModule module)
        {
            MetadataReader metadataReader = module.MetadataReader;
            StringHandle nameHandle = metadataReader.IsAssembly
                ? metadataReader.GetAssemblyDefinition().Name
                : metadataReader.GetModuleDefinition().Name;

            string actualSimpleName = metadataReader.GetString(nameHandle);
            if (!actualSimpleName.Equals(simpleName, StringComparison.OrdinalIgnoreCase))
            {
                throw new VerifierException($"Actual PE name '{actualSimpleName}' does not match provided name '{simpleName}'");
            }
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForNonPointerArrayType(ArrayType type)
        {
            if (_arrayOfTRuntimeInterfacesAlgorithm == null)
            {
                _arrayOfTRuntimeInterfacesAlgorithm = new SimpleArrayOfTRuntimeInterfacesAlgorithm(SystemModule);
            }
            return _arrayOfTRuntimeInterfacesAlgorithm;
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForDefType(DefType type)
        {
            return _metadataRuntimeInterfacesAlgorithm;
        }

        public override VirtualMethodAlgorithm GetVirtualMethodAlgorithmForType(TypeDesc type)
        {
            return _metadataVirtualMethodAlgorithm;
        }

        internal EcmaModule GetModule(PEReader peReader, IAssemblyDesc containingAssembly = null)
        {
            if (peReader == null)
            {
                return null;
            }

            if (_modulesCache.TryGetValue(peReader, out EcmaModule existingModule))
            {
                if (containingAssembly != null && existingModule.Assembly != containingAssembly)
                {
                    throw new VerifierException($"Containing assembly for module '{existingModule}' must be '{containingAssembly}'");
                }
                return existingModule;
            }

            EcmaModule module = EcmaModule.Create(this, peReader, containingAssembly);
            _modulesCache.Add(peReader, module);
            return module;
        }
    }
}
