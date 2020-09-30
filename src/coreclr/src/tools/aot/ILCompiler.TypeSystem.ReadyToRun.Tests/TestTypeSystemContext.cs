// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.IO;

namespace TypeSystemTests
{
    public enum CanonicalizationMode
    {
        Standard,
        RuntimeDetermined,
    }

    class TestTypeSystemContext : MetadataTypeSystemContext
    {
        Dictionary<string, ModuleDesc> _modules = new Dictionary<string, ModuleDesc>(StringComparer.OrdinalIgnoreCase);

        MetadataFieldLayoutAlgorithm _metadataFieldLayout = new TestMetadataFieldLayoutAlgorithm();
        MetadataRuntimeInterfacesAlgorithm _metadataRuntimeInterfacesAlgorithm = new MetadataRuntimeInterfacesAlgorithm();
        ArrayOfTRuntimeInterfacesAlgorithm _arrayOfTRuntimeInterfacesAlgorithm;
        VirtualMethodAlgorithm _virtualMethodAlgorithm = new MetadataVirtualMethodAlgorithm();
        
        public CanonicalizationMode CanonMode { get; set; } = CanonicalizationMode.RuntimeDetermined;

        public TestTypeSystemContext(TargetArchitecture arch)
            : base(new TargetDetails(arch, TargetOS.Unknown, TargetAbi.Unknown))
        {
        }

        public ModuleDesc GetModuleForSimpleName(string simpleName)
        {
            ModuleDesc existingModule;
            if (_modules.TryGetValue(simpleName, out existingModule))
                return existingModule;

            return CreateModuleForSimpleName(simpleName);
        }

        public ModuleDesc CreateModuleForSimpleName(string simpleName)
        {
            string bindingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(bindingDirectory, simpleName + ".dll");
            ModuleDesc module = Internal.TypeSystem.Ecma.EcmaModule.Create(this, new PEReader(File.OpenRead(filePath)), containingAssembly: null);
            _modules.Add(simpleName, module);
            return module;
        }

        public override ModuleDesc ResolveAssembly(System.Reflection.AssemblyName name, bool throwIfNotFound)
        {
            return GetModuleForSimpleName(name.Name);
        }

        public override FieldLayoutAlgorithm GetLayoutAlgorithmForType(DefType type)
        {
            if (type == UniversalCanonType)
                return UniversalCanonLayoutAlgorithm.Instance;

            return _metadataFieldLayout;
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForNonPointerArrayType(ArrayType type)
        {
            if (_arrayOfTRuntimeInterfacesAlgorithm == null)
            {
                _arrayOfTRuntimeInterfacesAlgorithm = new ArrayOfTRuntimeInterfacesAlgorithm(SystemModule.GetType("System", "Array`1"));
            }
            return _arrayOfTRuntimeInterfacesAlgorithm;
        }

        protected override RuntimeInterfacesAlgorithm GetRuntimeInterfacesAlgorithmForDefType(DefType type)
        {
            return _metadataRuntimeInterfacesAlgorithm;
        }

        public override VirtualMethodAlgorithm GetVirtualMethodAlgorithmForType(TypeDesc type)
        {
            return _virtualMethodAlgorithm;
        }

        protected override Instantiation ConvertInstantiationToCanonForm(Instantiation instantiation, CanonicalFormKind kind, out bool changed)
        {
            if (CanonMode == CanonicalizationMode.Standard)
                return StandardCanonicalizationAlgorithm.ConvertInstantiationToCanonForm(instantiation, kind, out changed);
            else
                return RuntimeDeterminedCanonicalizationAlgorithm.ConvertInstantiationToCanonForm(instantiation, kind, out changed);
        }

        protected override TypeDesc ConvertToCanon(TypeDesc typeToConvert, CanonicalFormKind kind)
        {
            if (CanonMode == CanonicalizationMode.Standard)
                return StandardCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, kind);
            else
                return RuntimeDeterminedCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, kind);
        }

        protected override TypeDesc ConvertToCanon(TypeDesc typeToConvert, ref CanonicalFormKind kind)
        {
            if (CanonMode == CanonicalizationMode.Standard)
                return StandardCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, kind);
            else
                return RuntimeDeterminedCanonicalizationAlgorithm.ConvertToCanon(typeToConvert, ref kind);
        }

        protected override bool ComputeHasGCStaticBase(FieldDesc field)
        {
            Debug.Assert(field.IsStatic);

            if (field.IsThreadStatic)
                return true;

            TypeDesc fieldType = field.FieldType;
            if (fieldType.IsValueType)
                return ((DefType)fieldType).ContainsGCPointers;
            else
                return fieldType.IsGCPointer;

        }

        public override bool SupportsUniversalCanon => true;
        public override bool SupportsCanon => true;
    }
}
