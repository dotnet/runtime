// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using EcmaType = Internal.TypeSystem.Ecma.EcmaType;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a type that has metadata generated in the current compilation.
    /// This node corresponds to an ECMA-335 TypeDef record. It is however not a 1:1
    /// mapping because IL could be compiled into machine code without generating a record
    /// in the reflection metadata (which would not be possible in IL terms).
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class TypeMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MetadataType _type;

        public TypeMetadataNode(MetadataType type)
        {
            Debug.Assert(type.IsTypeDefinition);
            _type = type;
        }

        public MetadataType Type => _type;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();

            CustomAttributeBasedDependencyAlgorithm.AddDependenciesDueToCustomAttributes(ref dependencies, factory, ((EcmaType)_type));

            DefType containingType = _type.ContainingType;
            if (containingType != null)
                dependencies.Add(factory.TypeMetadata((MetadataType)containingType), "Containing type of a reflectable type");
            else
                dependencies.Add(factory.ModuleMetadata(_type.Module), "Containing module of a reflectable type");

            MetadataType baseType = _type.MetadataBaseType;
            if (baseType != null)
                GetMetadataDependencies(ref dependencies, factory, baseType, "Base type of a reflectable type");

            // TODO-SIZE: if we start trimming interface lists, we can probably trim here
            foreach (DefType interfaceType in _type.ExplicitlyImplementedInterfaces)
                GetMetadataDependencies(ref dependencies, factory, interfaceType, "Interface of a reflectable type");

            var mdManager = (UsageBasedMetadataManager)factory.MetadataManager;

            if (_type.IsEnum)
            {
                // A lot of the enum reflection actually happens on top of the respective MethodTable (e.g. getting the underlying type),
                // so for enums also include their MethodTable.
                dependencies.Add(factory.ReflectedType(_type), "Reflectable enum");

                // Enums are not useful without their literal fields. The literal fields are not referenced
                // from anywhere (source code reference to enums compiles to the underlying numerical constants in IL).
                foreach (FieldDesc enumField in _type.GetFields())
                {
                    if (enumField.IsLiteral)
                    {
                        dependencies.Add(factory.FieldMetadata(enumField), "Value of a reflectable enum");
                    }
                }
            }

            // If the user asked for complete metadata to be generated for all types that are getting metadata, ensure that.
            if ((mdManager._generationOptions & UsageBasedMetadataGenerationOptions.CompleteTypesOnly) != 0)
            {
                foreach (MethodDesc method in _type.GetMethods())
                {
                    if (!mdManager.IsReflectionBlocked(method))
                    {
                        try
                        {
                            // Make sure we're not adding a method to the dependency graph that is going to
                            // cause trouble down the line. This entire type would not actually load on CoreCLR anyway.
                            LibraryRootProvider.CheckCanGenerateMethod(method);
                        }
                        catch (TypeSystemException)
                        {
                            continue;
                        }

                        dependencies.Add(factory.MethodMetadata(method), "Complete metadata for type");
                    }
                }

                foreach (FieldDesc field in _type.GetFields())
                {
                    if (!mdManager.IsReflectionBlocked(field))
                        dependencies.Add(factory.FieldMetadata(field), "Complete metadata for type");
                }
            }

            return dependencies;
        }

        /// <summary>
        /// Decomposes a constructed type into individual <see cref="TypeMetadataNode"/> units that will be needed to
        /// express the constructed type in metadata.
        /// </summary>
        public static void GetMetadataDependencies(ref DependencyList dependencies, NodeFactory nodeFactory, TypeDesc type, string reason)
        {
            MetadataManager mdManager = nodeFactory.MetadataManager;

            switch (type.Category)
            {
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.ByRef:
                case TypeFlags.Pointer:
                    GetMetadataDependencies(ref dependencies, nodeFactory, ((ParameterizedType)type).ParameterType, reason);
                    break;
                case TypeFlags.FunctionPointer:
                    var pointerType = (FunctionPointerType)type;
                    GetMetadataDependencies(ref dependencies, nodeFactory, pointerType.Signature.ReturnType, reason);
                    foreach (TypeDesc paramType in pointerType.Signature)
                        GetMetadataDependencies(ref dependencies, nodeFactory, paramType, reason);
                    break;

                case TypeFlags.SignatureMethodVariable:
                case TypeFlags.SignatureTypeVariable:
                    break;

                default:
                    Debug.Assert(type.IsDefType);

                    TypeDesc typeDefinition = type.GetTypeDefinition();
                    if (typeDefinition != type)
                    {
                        if (mdManager.CanGenerateMetadata((MetadataType)typeDefinition))
                        {
                            dependencies ??= new DependencyList();
                            dependencies.Add(nodeFactory.TypeMetadata((MetadataType)typeDefinition), reason);
                        }

                        foreach (TypeDesc typeArg in type.Instantiation)
                        {
                            GetMetadataDependencies(ref dependencies, nodeFactory, typeArg, reason);
                        }
                    }
                    else
                    {
                        if (mdManager.CanGenerateMetadata((MetadataType)type))
                        {
                            dependencies ??= new DependencyList();
                            dependencies.Add(nodeFactory.TypeMetadata((MetadataType)type), reason);
                        }
                    }
                    break;
            }
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Reflectable type: " + _type.ToString();
        }

        protected override void OnMarked(NodeFactory factory)
        {
            Debug.Assert(!factory.MetadataManager.IsReflectionBlocked(_type));
            Debug.Assert(factory.MetadataManager.CanGenerateMetadata(_type));
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
