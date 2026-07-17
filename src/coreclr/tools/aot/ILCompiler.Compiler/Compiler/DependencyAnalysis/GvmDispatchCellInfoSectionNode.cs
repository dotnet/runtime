// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.NativeFormat;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;

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
        private readonly ExternalReferencesTableNode _externalReferences;

        public GvmDispatchCellInfoSectionNode(ExternalReferencesTableNode externalReferences)
        {
            _externalReferences = externalReferences;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            if (relocsOnly)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, Array.Empty<ISymbolDefinitionNode>());

            var cells = new List<DispatchCellNode>();
            foreach (DispatchCellNode node in new SortedSet<DispatchCellNode>(factory.MetadataManager.GetDispatchCells(), new DispatchCellInfoComparer()))
            {
                if (!node.TargetMethod.HasInstantiation)
                    continue;

                node.InitializeOffset(checked(cells.Count * node.Size));
                cells.Add(node);
            }

            if (cells.Count == 0)
                return new ObjectData(Array.Empty<byte>(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });

            var writer = new NativeWriter();
            Section section = writer.NewSection();
            var entries = new VertexArray(section);
            section.Place(entries);

            int entryIndex = 0;
            for (int firstCell = 0; firstCell < cells.Count;)
            {
                MethodDesc targetMethod = cells[firstCell].TargetMethod;
                int nextCell = firstCell + 1;
                while (nextCell < cells.Count && cells[nextCell].TargetMethod == targetMethod)
                    nextCell++;

                uint owningTypeIndex = _externalReferences.GetIndex(factory.MaximallyConstructableType(targetMethod.OwningType));
                uint instantiationIndex = _externalReferences.GetIndex(factory.ConstructedGenericComposition(targetMethod.Instantiation));

                int token = factory.MetadataManager.GetMetadataHandleForMethod(factory, GetMethodForMetadata(targetMethod, out bool isAsyncVariant));

                entries.Set(entryIndex++, writer.GetTuple(
                    writer.GetUnsignedConstant(checked((uint)nextCell)),
                    writer.GetUnsignedConstant(owningTypeIndex),
                    writer.GetTuple(
                        writer.GetUnsignedConstant(instantiationIndex),
                        writer.GetTuple(
                            writer.GetUnsignedConstant(checked((uint)(token & MetadataManager.MetadataOffsetMask))),
                            writer.GetUnsignedConstant(isAsyncVariant ? 1u : 0u)))));

                firstCell = nextCell;
            }

            entries.ExpandLayout();

            return new ObjectData(writer.Save(), Array.Empty<Relocation>(), 1, new ISymbolDefinitionNode[] { this });
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

    }
}
