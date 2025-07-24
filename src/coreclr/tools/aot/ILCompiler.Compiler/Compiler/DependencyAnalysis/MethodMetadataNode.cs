// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Logging;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using EcmaMethod = Internal.TypeSystem.Ecma.EcmaMethod;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method that has metadata generated in the current compilation.
    /// This corresponds to a ECMA-335 MethodDef record. It is however not a 1:1
    /// mapping because a method could be used in the AOT compiled program without generating
    /// the reflection metadata for it (which would not be possible in IL terms).
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class MethodMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaMethod _method;
        private readonly bool _isMinimal;

        public MethodMetadataNode(MethodDesc method, bool isMinimal)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            _method = (EcmaMethod)method;
            _isMinimal = isMinimal;
        }

        public MethodDesc Method => _method;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();
            dependencies.Add(factory.TypeMetadata((MetadataType)_method.OwningType), "Owning type metadata");

            if (!_isMinimal)
            {
                CustomAttributeBasedDependencyAlgorithm.AddDependenciesDueToCustomAttributes(ref dependencies, factory, _method);

                foreach (var parameterHandle in _method.MetadataReader.GetMethodDefinition(_method.Handle).GetParameters())
                {
                    dependencies.Add(factory.MethodParameterMetadata(new ReflectableParameter(_method.Module, parameterHandle)), "Parameter is visible");
                }
            }

            MethodSignature sig = _method.Signature;
            const string reason = "Method signature metadata";
            TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, sig.ReturnType, reason);
            foreach (TypeDesc paramType in sig)
            {
                TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, paramType, reason);
            }

            if (sig.HasEmbeddedSignatureData)
            {
                foreach (var sigData in sig.GetEmbeddedSignatureData())
                    if (sigData.type != null)
                        TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, sigData.type, "Modifier in a method signature");
            }

            if (!_isMinimal)
            {
                DynamicDependencyAttributesOnEntityNode.AddDependenciesDueToDynamicDependencyAttribute(ref dependencies, factory, _method);

                // On a reflectable method, perform generic data flow for the return type and all the parameter types
                // This is a compensation for the DI issue described in https://github.com/dotnet/runtime/issues/81358
                GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(ref dependencies, factory, new MessageOrigin(_method), _method.Signature.ReturnType, _method);

                foreach (TypeDesc parameterType in _method.Signature)
                {
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(ref dependencies, factory, new MessageOrigin(_method), parameterType, _method);
                }
            }

            return dependencies;
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Method metadata: " + _method.ToString();
        }

        protected override void OnMarked(NodeFactory factory)
        {
            Debug.Assert(!factory.MetadataManager.IsReflectionBlocked(_method));
            Debug.Assert(factory.MetadataManager.CanGenerateMetadata(_method));
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
