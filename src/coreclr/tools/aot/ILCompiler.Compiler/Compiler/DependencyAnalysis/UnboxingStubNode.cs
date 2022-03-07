// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents an unboxing stub that supports calling instance methods on boxed valuetypes.
    /// </summary>
    public partial class UnboxingStubNode : AssemblyStubNode, IMethodNode, ISymbolDefinitionNode
    {
        // Section name on Windows has to be alphabetically less than the ending WindowsUnboxingStubsRegionNode node, and larger than
        // the begining WindowsUnboxingStubsRegionNode node, in order to have proper delimiters to the begining/ending of the
        // stubs region, in order for the runtime to know where the region starts and ends.
        internal static readonly string WindowsSectionName = ".unbox$M";
        internal static readonly string UnixSectionName = "__unbox";

        private readonly TargetDetails _targetDetails;

        public MethodDesc Method { get; }

        public override ObjectNodeSection Section
        {
            get
            {
                string sectionName = _targetDetails.IsWindows ? WindowsSectionName : UnixSectionName;
                return new ObjectNodeSection(sectionName, SectionType.Executable);
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
