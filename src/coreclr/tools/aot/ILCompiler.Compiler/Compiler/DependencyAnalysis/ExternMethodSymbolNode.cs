// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a symbol that is defined externally but modelled as a method
    /// in the DependencyAnalysis infrastructure during compilation
    /// </summary>
    public sealed class ExternMethodSymbolNode : ExternSymbolNode, IMethodNode
    {
        private MethodDesc _method;

        public ExternMethodSymbolNode(NodeFactory factory, MethodDesc method, bool isUnboxing = false)
            : base(isUnboxing ? UnboxingStubNode.GetMangledName(factory.NameMangler, method) :
                  factory.NameMangler.GetMangledMethodName(method))
        {
            _method = method;
        }

        public MethodDesc Method
        {
            get
            {
                return _method;
            }
        }

        public override int ClassCode => -729061105;
    }
}
