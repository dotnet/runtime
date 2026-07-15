// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using DependencyListEntry = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyListEntry;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a section of the executable where information about GVM dispatch cells
    /// is stored.
    /// </summary>
    public class GvmDispatchCellInfoSectionNode : ObjectNode, ISymbolDefinitionNode
    {
        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            var cells = new List<DispatchCellNode>();
            foreach (DispatchCellNode node in new SortedSet<DispatchCellNode>(factory.MetadataManager.GetDispatchCells(), new DispatchCellComparer()))
            {
                if (!node.TargetMethod.HasInstantiation)
                    continue;

                node.InitializeOffset(checked(cells.Count * node.Size));
                cells.Add(node);
            }

            var descriptorIndices = new Dictionary<MethodDesc, int>();
            var descriptors = new List<MethodDesc>();
            var cellDescriptorIndices = new int[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                MethodDesc targetMethod = cells[i].TargetMethod;
                if (!descriptorIndices.TryGetValue(targetMethod, out int descriptorIndex))
                {
                    descriptorIndex = descriptors.Count;
                    descriptorIndices.Add(targetMethod, descriptorIndex);
                    descriptors.Add(targetMethod);
                }

                cellDescriptorIndices[i] = descriptorIndex;
            }

            var builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.AddSymbol(this);

            builder.RequireInitialAlignment(factory.Target.PointerSize);

            int pointerSize = factory.Target.SupportsRelativePointers ? sizeof(int) : factory.Target.PointerSize;
            int descriptorSize = checked(2 * pointerSize + sizeof(int));
            int directSize = checked(cells.Count * descriptorSize);
            int dictionarySize = DispatchCellInfoEncoding.GetDictionarySize(cells.Count, descriptors.Count, pointerSize, descriptorSize);

            if (dictionarySize < directSize)
            {
                builder.EmitUInt(checked((uint)descriptors.Count));

                int indexSize = DispatchCellInfoEncoding.GetIndexSize(descriptors.Count);
                foreach (int descriptorIndex in cellDescriptorIndices)
                    DispatchCellInfoEncoding.EmitIndex(ref builder, descriptorIndex, indexSize);

                builder.PadAlignment(pointerSize);

                foreach (MethodDesc targetMethod in descriptors)
                    EmitOwningType(ref builder, factory, targetMethod);

                foreach (MethodDesc targetMethod in descriptors)
                    EmitInstantiation(ref builder, factory, targetMethod);

                foreach (MethodDesc targetMethod in descriptors)
                    EmitFlagsAndToken(ref builder, factory, targetMethod);
            }
            else
            {
                foreach (DispatchCellNode cell in cells)
                    EmitOwningType(ref builder, factory, cell.TargetMethod);

                foreach (DispatchCellNode cell in cells)
                    EmitInstantiation(ref builder, factory, cell.TargetMethod);

                foreach (DispatchCellNode cell in cells)
                    EmitFlagsAndToken(ref builder, factory, cell.TargetMethod);
            }

            return builder.ToObjectData();
        }

        private static void EmitOwningType(ref ObjectDataBuilder builder, NodeFactory factory, MethodDesc targetMethod)
        {
            IEETypeNode owningType = factory.MaximallyConstructableType(targetMethod.OwningType);
            if (factory.Target.SupportsRelativePointers)
                builder.EmitReloc(owningType, RelocType.IMAGE_REL_BASED_RELPTR32);
            else
                builder.EmitPointerReloc(owningType);
        }

        private static void EmitInstantiation(ref ObjectDataBuilder builder, NodeFactory factory, MethodDesc targetMethod)
        {
            ISymbolNode instantiation = factory.ConstructedGenericComposition(targetMethod.Instantiation);
            if (factory.Target.SupportsRelativePointers)
                builder.EmitReloc(instantiation, RelocType.IMAGE_REL_BASED_RELPTR32);
            else
                builder.EmitPointerReloc(instantiation);
        }

        private static void EmitFlagsAndToken(ref ObjectDataBuilder builder, NodeFactory factory, MethodDesc targetMethod)
        {
            int token = factory.MetadataManager.GetMetadataHandleForMethod(factory, GetMethodForMetadata(targetMethod, out bool isAsyncVariant));
            int flags = isAsyncVariant ? GvmDispatchCellFlags.IsAsyncVariant : 0;
            builder.EmitInt((token & MetadataManager.MetadataOffsetMask) | flags);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
            => sb.Append(nameMangler.CompilationUnitPrefix).Append("__GvmDispatchCellInfoSection_Start"u8);
        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.GvmDispatchCellInfoSection;

        public int Offset => 0;

        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;

        public static IEnumerable<DependencyListEntry> GetCellDependencies(NodeFactory factory, MethodDesc targetMethod)
        {
            DependencyList result = new DependencyList();

            MethodDesc canonMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
            result.Add(factory.GVMDependencies(canonMethod), "GVM dependencies");

            // GVM analysis happens on canonical forms, but this is potentially injecting new genericness
            // into the system. Ensure reflection analysis can still see this.
            if (targetMethod.IsAbstract)
                factory.MetadataManager.GetDependenciesDueToMethodCodePresence(ref result, factory, canonMethod, methodIL: null);

            factory.MetadataManager.GetDependenciesDueToVirtualMethodReflectability(ref result, factory, targetMethod);

            factory.MetadataManager.GetNativeLayoutMetadataDependencies(ref result, factory, GetMethodForMetadata(targetMethod, out _));

            result.Add(factory.MaximallyConstructableType(targetMethod.OwningType), "Owning type of GVM decl");
            result.Add(factory.ConstructedGenericComposition(targetMethod.Instantiation), "GVM instantiation info");

            return result;
        }

        public static MethodDesc GetMethodForMetadata(MethodDesc method, out bool isAsyncVariant)
        {
            isAsyncVariant = false;
            MethodDesc targetMethodForMetadata = method.GetTypicalMethodDefinition();
            if (targetMethodForMetadata.IsAsyncVariant())
            {
                targetMethodForMetadata = ((CompilerTypeSystemContext)method.Context).GetTargetOfAsyncVariantMethod(targetMethodForMetadata);
                isAsyncVariant = true;
            }

            return targetMethodForMetadata;
        }

        /// <summary>
        /// Comparer that groups GVM dispatch cells by their callsite.
        /// </summary>
        private sealed class DispatchCellComparer : IComparer<DispatchCellNode>
        {
            private readonly CompilerComparer _comparer = CompilerComparer.Instance;

            public int Compare(DispatchCellNode x, DispatchCellNode y)
            {
                int result = _comparer.Compare(x.CallSiteIdentifier, y.CallSiteIdentifier);
                if (result != 0)
                    return result;

                MethodDesc methodX = x.TargetMethod;
                MethodDesc methodY = y.TargetMethod;

                result = _comparer.Compare(methodX, methodY);
                if (result != 0)
                    return result;

                Debug.Assert(x == y);
                return 0;
            }
        }
    }
}
