// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysisFramework
{
    public sealed class DependencySink<DependencyContextType> :
        IDependencySink<DependencyContextType>,
        IConditionalDependencySink<DependencyContextType>
    {
        internal enum DependencyKind
        {
            Static,
            Conditional,
            Dynamic,
        }

        internal readonly struct Dependency
        {
            public Dependency(
                DependencyNodeCore<DependencyContextType>? source,
                DependencyNodeCore<DependencyContextType> node,
                DependencyNodeCore<DependencyContextType>? otherReasonNode,
                string reason,
                DependencyKind kind)
            {
                Source = source;
                Node = node;
                OtherReasonNode = otherReasonNode;
                Reason = reason;
                Kind = kind;
            }

            public DependencyNodeCore<DependencyContextType>? Source { get; }
            public DependencyNodeCore<DependencyContextType> Node { get; }
            public DependencyNodeCore<DependencyContextType>? OtherReasonNode { get; }
            public string Reason { get; }
            public DependencyKind Kind { get; }
        }

        private readonly List<Dependency> _dependencies = new List<Dependency>();
        private readonly DependencyNodeCore<DependencyContextType>.DependencyList? _staticDependencies;
        private DependencyNodeCore<DependencyContextType>? _source;
        private DependencyNodeCore<DependencyContextType>? _otherReasonNode;
        private DependencyKind _kind;

        public DependencySink()
        {
        }

        public DependencySink(DependencyNodeCore<DependencyContextType>.DependencyList dependencies)
        {
            _staticDependencies = dependencies;
        }

        internal List<Dependency> Dependencies => _dependencies;

        public DependencyNodeCore<DependencyContextType>? SetOtherReasonNode(DependencyNodeCore<DependencyContextType>? otherReasonNode)
        {
            DependencyNodeCore<DependencyContextType>? previousOtherReasonNode = _otherReasonNode;
            _otherReasonNode = otherReasonNode;
            return previousOtherReasonNode;
        }

        internal void BeginNode(DependencyNodeCore<DependencyContextType> source, DependencyKind kind)
        {
            _source = source;
            _kind = kind;
        }

        internal void ClearDependencies()
        {
            Debug.Assert(_staticDependencies is null);
            _dependencies.Clear();
            _source = null;
            _otherReasonNode = null;
        }

        public void Add(DependencyNodeCore<DependencyContextType> node, string reason)
        {
            if (_staticDependencies is not null)
            {
                _staticDependencies.Add(node, reason);
                return;
            }

            _dependencies.Add(new Dependency(_source, node, _otherReasonNode, reason, _kind));
        }

        public void Add(object node, string reason)
        {
            Add((DependencyNodeCore<DependencyContextType>)node, reason);
        }

        public void Add(
            DependencyNodeCore<DependencyContextType> node,
            DependencyNodeCore<DependencyContextType>? otherReasonNode,
            string reason)
        {
            _dependencies.Add(new Dependency(_source, node, otherReasonNode, reason, _kind));
        }

        public void Add(object node, object? otherReasonNode, string reason)
        {
            Add(
                (DependencyNodeCore<DependencyContextType>)node,
                (DependencyNodeCore<DependencyContextType>?)otherReasonNode,
                reason);
        }

        public void Add(DependencyNodeCore<DependencyContextType>.DependencyListEntry dependency)
        {
            Add(dependency.Node, dependency.Reason);
        }

        public void Add(DependencyNodeCore<DependencyContextType>.CombinedDependencyListEntry dependency)
        {
            Add(dependency.Node, dependency.OtherReasonNode, dependency.Reason);
        }

        public void AddRange(params ReadOnlySpan<DependencyNodeCore<DependencyContextType>.DependencyListEntry> dependencies)
        {
            foreach (DependencyNodeCore<DependencyContextType>.DependencyListEntry dependency in dependencies)
            {
                Add(dependency);
            }
        }

        public void AddRange(params ReadOnlySpan<DependencyNodeCore<DependencyContextType>.CombinedDependencyListEntry> dependencies)
        {
            foreach (DependencyNodeCore<DependencyContextType>.CombinedDependencyListEntry dependency in dependencies)
            {
                Add(dependency);
            }
        }
    }
}
