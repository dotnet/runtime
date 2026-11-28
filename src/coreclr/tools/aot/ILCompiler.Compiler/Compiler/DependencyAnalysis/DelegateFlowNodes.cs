// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Base class for the nodes used by the delegate flow analysis. These nodes carry no dependencies of their own.
    /// They only exist so that other nodes can be made conditional on them.
    /// </summary>
    internal abstract class DelegateFlowNode : DependencyNodeCore<NodeFactory>
    {
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override void AddStaticDependencies(DependencySink sink, NodeFactory factory) { }
        public override void AddConditionalDependencies(DependencySink sink, NodeFactory factory) { }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory factory) { }
    }

    /// <summary>
    /// Marked when a delegate that was stored into the given field may be observed, that is, when the program may
    /// read the field in a way the delegate flow analysis does not understand.
    /// </summary>
    internal sealed class DelegateFieldReadNode : DelegateFlowNode
    {
        public FieldDesc Field { get; }

        public DelegateFieldReadNode(FieldDesc field)
        {
            Debug.Assert(field.IsTypicalFieldDefinition);
            Field = field;
        }

        protected override string GetName(NodeFactory factory) => "Delegate stored in field may be observed: " + Field.ToString();
    }

    /// <summary>
    /// Marked when the delegate returned by the given trivial getter may be observed by something other
    /// than a call site the delegate flow analysis understands.
    /// </summary>
    internal sealed class DelegateReturnObservedNode : DelegateFlowNode
    {
        public MethodDesc Method { get; }

        public DelegateReturnObservedNode(MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            Method = method;
        }

        protected override string GetName(NodeFactory factory) => "Delegate returned by method may be observed: " + Method.ToString();
    }

    /// <summary>
    /// Marked for each delegate creation site that the delegate flow analysis understands.
    /// A site is identified by the method containing it and the method the delegate points to.
    /// </summary>
    internal sealed class DelegateCreationSiteNode : DelegateFlowNode
    {
        public MethodDesc Owner { get; }
        public MethodDesc Target { get; }

        public DelegateCreationSiteNode(MethodDesc owner, MethodDesc target)
        {
            Debug.Assert(owner.IsTypicalMethodDefinition && target.IsTypicalMethodDefinition);
            Owner = owner;
            Target = target;
        }

        protected override string GetName(NodeFactory factory) => $"Delegate creation site in {Owner} targeting {Target}";
    }

    /// <summary>
    /// Marked when a delegate created at the given site may be observed, and for every delegate creation of the same
    /// target in the same method that the delegate flow analysis does not understand. A site that has a
    /// <see cref="DelegateCreationSiteNode"/> but no <see cref="DelegateCreationSiteUseNode"/> in the scanned
    /// graph creates delegates that the program never observes.
    /// </summary>
    internal sealed class DelegateCreationSiteUseNode : DelegateFlowNode
    {
        public MethodDesc Owner { get; }
        public MethodDesc Target { get; }

        public DelegateCreationSiteUseNode(MethodDesc owner, MethodDesc target)
        {
            Debug.Assert(owner.IsTypicalMethodDefinition && target.IsTypicalMethodDefinition);
            Owner = owner;
            Target = target;
        }

        protected override string GetName(NodeFactory factory) => $"Delegate created in {Owner} targeting {Target} may be observed";
    }
}
