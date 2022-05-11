// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public abstract partial class SortableDependencyNode : DependencyNodeCore<NodeFactory>, ISortableNode
    {
#if !SUPPORT_JIT
        /// <summary>
        /// Allows grouping of <see cref="ObjectNode"/> instances such that all nodes in a lower phase
        /// will be ordered before nodes in a later phase.
        /// </summary>
        protected internal virtual int Phase => (int)ObjectNodePhase.Unordered;

        /// <summary>
        /// Gets an identifier that is the same for all instances of this <see cref="ObjectNode"/>
        /// descendant, but different from the <see cref="ClassCode"/> of any other descendant.
        /// </summary>
        /// <remarks>
        /// This is really just a number, ideally produced by "new Random().Next(int.MinValue, int.MaxValue)".
        /// If two manage to conflict (which is pretty unlikely), just make a new one...
        /// </remarks>
        public abstract int ClassCode { get; }

        // Note to implementers: the type of `other` is actually the same as the type of `this`.
        public virtual int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            throw new NotImplementedException("Multiple nodes of this type are not supported");
        }

        protected enum ObjectNodePhase
        {
            /// <summary>
            /// Nodes should only be placed in this phase if they have strict output ordering requirements that
            /// affect compiler correctness. Today that includes native layout tables.
            /// </summary>
            Ordered,
            Unordered
        }

        protected enum ObjectNodeOrder
        {
            //
            // The ordering of this sequence of nodes is deliberate and currently required for 
            // compiler correctness.
            //

            //
            // ReadyToRun Nodes
            //
            Win32ResourcesNode,
            CorHeaderNode,
            ReadyToRunHeaderNode,
            ReadyToRunAssemblyHeaderNode,
            ImportSectionsTableNode,
            ImportSectionNode,
            MethodEntrypointTableNode,


            //
            // NativeAOT Nodes
            //
            MetadataNode,
            ResourceDataNode,
            ResourceIndexNode,
            TypeMetadataMapNode,
            ClassConstructorContextMap,
            DynamicInvokeTemplateDataNode,
            ReflectionInvokeMapNode,
            DelegateMarshallingStubMapNode,
            StructMarshallingStubMapNode,
            ArrayMapNode,
            ReflectionFieldMapNode,
            NativeLayoutInfoNode,
            ExactMethodInstantiationsNode,
            GenericTypesHashtableNode,
            GenericMethodsHashtableNode,
            GenericVirtualMethodTableNode,
            InterfaceGenericVirtualMethodTableNode,
            GenericMethodsTemplateMap,
            GenericTypesTemplateMap,
            BlockReflectionTypeMapNode,
            StaticsInfoHashtableNode,
            ReflectionVirtualInvokeMapNode,
            ArrayOfEmbeddedPointersNode,
            DefaultConstructorMapNode,
            ExternalReferencesTableNode,
            StackTraceEmbeddedMetadataNode,
            StackTraceMethodMappingNode,
            ArrayOfEmbeddedDataNode,
        }

        public class EmbeddedObjectNodeComparer : IComparer<EmbeddedObjectNode>
        {
            private CompilerComparer _comparer;

            public EmbeddedObjectNodeComparer(CompilerComparer comparer)
            {
                _comparer = comparer;
            }

            public int Compare(EmbeddedObjectNode x, EmbeddedObjectNode y)
            {
                return CompareImpl(x, y, _comparer);
            }
        }

        /// <summary>
        /// This comparer is used to sort the marked node list. We only care about ordering ObjectNodes
        /// for emission into the binary, so any EmbeddedObjectNode or DependencyNodeCore objects are
        /// skipped for efficiency.
        /// </summary>
        public class ObjectNodeComparer : IComparer<DependencyNodeCore<NodeFactory>>
        {
            private CompilerComparer _comparer;

            public ObjectNodeComparer(CompilerComparer comparer)
            {
                _comparer = comparer;
            }

            public int Compare(DependencyNodeCore<NodeFactory> x1, DependencyNodeCore<NodeFactory> y1)
            {
                ObjectNode x = x1 as ObjectNode;
                ObjectNode y = y1 as ObjectNode;

                if (x == y)
                {
                    return 0;
                }

                // Sort non-object nodes after ObjectNodes
                if (x == null)
                    return 1;

                if (y == null)
                    return -1;

                return CompareImpl(x, y, _comparer);
            }
        }

        static partial void ApplyCustomSort(SortableDependencyNode x, SortableDependencyNode y, ref int result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareImpl(SortableDependencyNode x, SortableDependencyNode y, CompilerComparer comparer)
        {
            int phaseX = x.Phase;
            int phaseY = y.Phase;

            if (phaseX == phaseY)
            {
                int customSort = 0;
                ApplyCustomSort(x, y, ref customSort);
                if (customSort != 0)
                    return customSort;

                int codeX = x.ClassCode;
                int codeY = y.ClassCode;
                if (codeX == codeY)
                {
                    Debug.Assert(x.GetType() == y.GetType() ||
                        (x.GetType().IsConstructedGenericType && y.GetType().IsConstructedGenericType
                        && x.GetType().GetGenericTypeDefinition() == y.GetType().GetGenericTypeDefinition()));

                    int result = x.CompareToImpl(y, comparer);

                    // We did a reference equality check above so an "Equal" result is not expected
                    Debug.Assert(result != 0 || x == y);

                    return result;
                }
                else
                {
                    Debug.Assert(x.GetType() != y.GetType());
                    return codeY > codeX ? -1 : 1;
                }
            }
            else
            {
                return phaseX - phaseY;
            }
        }
#endif
    }
}
