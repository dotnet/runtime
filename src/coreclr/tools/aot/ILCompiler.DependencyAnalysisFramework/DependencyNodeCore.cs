// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

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

        public class DependencyList : List<DependencyListEntry>, IDependencySink<DependencyContextType>
        {
            public DependencyList() { }

            public DependencyList(IEnumerable<DependencyListEntry> collection)
                : base(collection)
            {
            }

            public virtual void Add(DependencyNodeCore<DependencyContextType> node,
                                    string reason)
            {
                Add(new DependencyListEntry(node, reason));
            }

            public virtual void Add(object node, string reason)
            {
                Add(new DependencyListEntry((DependencyNodeCore<DependencyContextType>)node, reason));
            }

            public new virtual void Add(DependencyListEntry dependency)
            {
                base.Add(dependency);
            }

            public virtual void AddRange(params ReadOnlySpan<DependencyListEntry> dependencies)
            {
                foreach (DependencyListEntry dependency in dependencies)
                {
                    Add(dependency);
                }
            }
        }

        public class CombinedDependencyList : List<CombinedDependencyListEntry>, IConditionalDependencySink<DependencyContextType>
        {
            public new virtual void Add(CombinedDependencyListEntry dependency)
            {
                base.Add(dependency);
            }

            public virtual void AddRange(params ReadOnlySpan<CombinedDependencyListEntry> dependencies)
            {
                foreach (CombinedDependencyListEntry dependency in dependencies)
                {
                    Add(dependency);
                }
            }
        }

        public struct CombinedDependencyListEntry : IEquatable<CombinedDependencyListEntry>
        {
            public CombinedDependencyListEntry(DependencyNodeCore<DependencyContextType> node,
                                               DependencyNodeCore<DependencyContextType>? otherReasonNode,
                                               string reason)
            {
                Node = node;
                OtherReasonNode = otherReasonNode;
                Reason = reason;
            }

            public CombinedDependencyListEntry(object node,
                                               object? otherReasonNode,
                                               string reason)
            {
                Node = (DependencyNodeCore<DependencyContextType>)node;
                OtherReasonNode = (DependencyNodeCore<DependencyContextType>?)otherReasonNode;
                Reason = reason;
            }

            // Used by HashSet, so must have good Equals/GetHashCode
            public readonly DependencyNodeCore<DependencyContextType> Node;
            public readonly DependencyNodeCore<DependencyContextType>? OtherReasonNode;
            public readonly string Reason;

            public override bool Equals(object? obj)
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

        public abstract void AddStaticDependencies(DependencySink<DependencyContextType> sink, DependencyContextType context);

        public virtual void AddConditionalDependencies(DependencySink<DependencyContextType> sink, DependencyContextType context)
        {
        }

        public virtual void SearchDynamicDependencies(List<DependencyNodeCore<DependencyContextType>> markedNodes, int firstNode, DependencySink<DependencyContextType> sink, DependencyContextType context)
        {
        }

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

        string IDependencyNode<DependencyContextType>.GetName(DependencyContextType context) => GetName(context);

        // We would prefer GetName to be "protected internal", but that will break people who want to source
        // include the dependency analysis framework. When nobody does that, maybe we can get rid of this method.
        internal string GetNameInternal(DependencyContextType context)
        {
            return GetName(context);
        }

        public static string GetNodeName(DependencyNodeCore<DependencyContextType> node, DependencyContextType context)
            => node.GetName(context);
    }
}
