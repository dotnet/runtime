// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILCompiler.DependencyAnalysisFramework
{
    public abstract class DependencyNodeCore<DependencyContextType> : DependencyNode, IDependencyNode<DependencyContextType>
    {
        public struct DependencyListEntry
        {
            public DependencyListEntry(DependencyNodeCore<DependencyContextType> node,
                                       string reason)
            {
                Node = node;
                Reason = reason;
            }

            public DependencyListEntry(object node,
                                       string reason)
            {
                Node = (DependencyNodeCore<DependencyContextType>)node;
                Reason = reason;
            }

            public DependencyNodeCore<DependencyContextType> Node;
            public string Reason;
        }

        public class DependencyList : List<DependencyListEntry>
        {
            public DependencyList() { }

            public DependencyList(IEnumerable<DependencyListEntry> collection)
                : base(collection)
            {
            }

            public void Add(DependencyNodeCore<DependencyContextType> node,
                                       string reason)
            {
                this.Add(new DependencyListEntry(node, reason));
            }

            public void Add(object node, string reason)
            {
                this.Add(new DependencyListEntry((DependencyNodeCore<DependencyContextType>)node, reason));
            }
        }

        public struct CombinedDependencyListEntry : IEquatable<CombinedDependencyListEntry>
        {
            public CombinedDependencyListEntry(DependencyNodeCore<DependencyContextType> node,
                                               DependencyNodeCore<DependencyContextType> otherReasonNode,
                                               string reason)
            {
                Node = node;
                OtherReasonNode = otherReasonNode;
                Reason = reason;
            }

            public CombinedDependencyListEntry(object node,
                                               object otherReasonNode,
                                               string reason)
            {
                Node = (DependencyNodeCore<DependencyContextType>)node;
                OtherReasonNode = (DependencyNodeCore<DependencyContextType>)otherReasonNode;
                Reason = reason;
            }

            // Used by HashSet, so must have good Equals/GetHashCode
            public readonly DependencyNodeCore<DependencyContextType> Node;
            public readonly DependencyNodeCore<DependencyContextType> OtherReasonNode;
            public readonly string Reason;

            public override bool Equals(object obj)
            {
                return obj is CombinedDependencyListEntry && Equals((CombinedDependencyListEntry)obj);
            }

            public override int GetHashCode()
            {
                int hash = 23;
                hash = hash * 31 + Node.GetHashCode();

                if (OtherReasonNode != null)
                    hash = hash * 31 + OtherReasonNode.GetHashCode();

                if (Reason != null)
                    hash = hash * 31 + Reason.GetHashCode();

                return hash;
            }

            public bool Equals(CombinedDependencyListEntry other)
            {
                return ReferenceEquals(Node, other.Node)
                    && ReferenceEquals(OtherReasonNode, other.OtherReasonNode)
                    && Equals(Reason, other.Reason);
            }
        }

        public abstract bool InterestingForDynamicDependencyAnalysis
        {
            get;
        }

        public abstract bool HasDynamicDependencies
        {
            get;
        }

        public abstract bool HasConditionalStaticDependencies
        {
            get;
        }

        public abstract bool StaticDependenciesAreComputed
        {
            get;
        }

        public virtual int DependencyPhaseForDeferredStaticComputation { get; }

        public abstract IEnumerable<DependencyListEntry> GetStaticDependencies(DependencyContextType context);

        public abstract IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(DependencyContextType context);

        public abstract IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<DependencyContextType>> markedNodes, int firstNode, DependencyContextType context);

        internal void CallOnMarked(DependencyContextType context)
        {
            OnMarked(context);
        }

        /// <summary>
        /// Overrides of this method allow a node to perform actions when said node becomes
        /// marked.
        /// </summary>
        /// <param name="context"></param>
        protected virtual void OnMarked(DependencyContextType context)
        {
            // Do nothing by default
        }

        // Force all non-abstract nodes to provide a name
        protected abstract string GetName(DependencyContextType context);

        // We would prefer GetName to be "protected internal", but that will break people who want to source
        // include the dependency analysis framework. When nobody does that, maybe we can get rid of this method.
        internal string GetNameInternal(DependencyContextType context)
        {
            return GetName(context);
        }
    }
}
