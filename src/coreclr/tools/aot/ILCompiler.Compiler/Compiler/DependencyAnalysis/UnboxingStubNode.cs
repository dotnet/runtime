// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an unboxing stub that supports calling instance methods on boxed valuetypes.
    /// </summary>
    public partial class UnboxingStubNode : AssemblyStubNode, IMethodNode, ISymbolDefinitionNode
    {
        private readonly TargetDetails _targetDetails;

        public MethodDesc Method { get; }

        public override ObjectNodeSection Section
        {
            get
            {
                return _targetDetails.IsWindows ?
                    ObjectNodeSection.UnboxingStubWindowsContentSection :
                    ObjectNodeSection.UnboxingStubUnixContentSection;
            }
        }
        public override bool IsShareable => true;

        public UnboxingStubNode(MethodDesc target, TargetDetails targetDetails)
        {
            Debug.Assert(target.GetCanonMethodTarget(CanonicalFormKind.Specific) == target);
            Debug.Assert(target.OwningType.IsValueType);
            Method = target;
            _targetDetails = targetDetails;
        }

        private ISymbolNode GetUnderlyingMethodEntrypoint(NodeFactory factory)
        {
            ISymbolNode node = factory.MethodEntrypoint(Method);
            return node;
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("unbox_").Append(nameMangler.GetMangledMethodName(Method));
        }

        public static string GetMangledName(NameMangler nameMangler, MethodDesc method)
        {
            return "unbox_" + nameMangler.GetMangledMethodName(method);
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override int ClassCode => -1846923013;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(Method, ((UnboxingStubNode)other).Method);
        }
    }
}
