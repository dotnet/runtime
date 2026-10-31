// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a method that is imported from the runtime library.
    /// </summary>
    public class RuntimeImportMethodNode : ExternFunctionSymbolNode, IMethodNode
    {
        private MethodDesc _method;

        public RuntimeImportMethodNode(MethodDesc method, NameMangler nameMangler)
            : this(method, nameMangler, new LinkageMetadata(new Utf8String(((EcmaMethod)method).GetRuntimeImportName())))
        { }

        private RuntimeImportMethodNode(MethodDesc method, NameMangler nameMangler, LinkageMetadata linkageMetadata)
            : base(nameMangler.NodeMangler.ExternMethod(linkageMetadata.Symbol, method),
                  method, linkageMetadata)
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

        public override int ClassCode => -1173492615;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_method, ((RuntimeImportMethodNode)other)._method);
        }
    }
}
