// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a symbol that is defined externally and statically linked to the output obj file.
    /// When making a new node, do not derive from this class directly, derive from one of its subclasses
    /// (ExternFunctionSymbolNode / ExternDataSymbolNode) instead.
    /// </summary>
    public abstract class ExternSymbolNode : SortableDependencyNode, ISortableSymbolNode
    {
        private readonly Utf8String _name;
        private readonly bool _isIndirection;

        protected ExternSymbolNode(Utf8String name, bool isIndirection = false)
        {
            _name = name;
            _isIndirection = isIndirection;
        }

        protected override string GetName(NodeFactory factory) => $"ExternSymbol {_name}{(_isIndirection ? " (indirected)" : "")}";

        public Utf8String Name => _name;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(_name);
        }

        public int Offset => 0;
        public virtual bool RepresentsIndirectionCell => _isIndirection;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;

#if !SUPPORT_JIT
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _name.CompareTo(((ExternSymbolNode)other)._name);
        }
#endif

        public override string ToString()
        {
            return _name.ToString();
        }
    }

    /// <summary>
    /// Signature information for an extern function, used to type its import on Wasm.
    /// </summary>
    public readonly record struct ExternalTypeSignature(
        MethodSignature Signature,
        bool IsUnmanagedCallersOnly,
        bool IsAsyncCall,
        bool HasGenericContextArg)
    {
        /// <summary>
        /// Signature for a native target, such as a direct P/Invoke or a native runtime helper.
        /// </summary>
        public static ExternalTypeSignature Unmanaged(MethodSignature signature)
            => new ExternalTypeSignature(signature, IsUnmanagedCallersOnly: true, IsAsyncCall: false, HasGenericContextArg: false);

        /// <summary>
        /// Signature for managed code that is referenced by name.
        /// </summary>
        // Keep aligned with IMethodCodeNodeWithTypeSignature
        public static ExternalTypeSignature FromMethod(MethodDesc method)
            => new ExternalTypeSignature(
                method.Signature,
                method.IsUnmanagedCallersOnly,
                method.IsAsyncCall(),
                method.RequiresInstMethodDescArg() || method.RequiresInstMethodTableArg() || method.IsArrayAddressMethod());
    }

    /// <summary>
    /// Represents a function symbol that is defined externally and statically linked to the output obj file.
    /// </summary>
    public class ExternFunctionSymbolNode : ExternSymbolNode, INodeWithTypeSignature
    {
        private readonly ExternalTypeSignature? _typeSignature;

        public ExternFunctionSymbolNode(Utf8String name, ExternalTypeSignature? typeSignature, bool isIndirection = false)
            : base(name, isIndirection)
        {
            _typeSignature = typeSignature;
        }

        public override int ClassCode => 1452455506;

        /// <summary>
        /// The signature of the function, or null if it has no standard-ABI signature.
        /// </summary>
        public ExternalTypeSignature? TypeSignature => _typeSignature;

        MethodSignature INodeWithTypeSignature.Signature => _typeSignature.Value.Signature;
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => _typeSignature.Value.IsUnmanagedCallersOnly;
        bool INodeWithTypeSignature.IsAsyncCall => _typeSignature.Value.IsAsyncCall;
        bool INodeWithTypeSignature.HasGenericContextArg => _typeSignature.Value.HasGenericContextArg;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            // Indirection cells are data, not functions, so they are not imported as functions
            if (!factory.Target.IsWasm || RepresentsIndirectionCell)
                return null;

            Debug.Assert(_typeSignature is not null, $"Extern function '{this}' has no known signature and cannot be imported on Wasm");
            return [new DependencyListEntry(factory.WasmFunctionImport(this), "Wasm extern functions are imported")];
        }
    }

    public class AddressTakenExternFunctionSymbolNode(Utf8String name) : ExternFunctionSymbolNode(name, typeSignature: null)
    {
        public override int ClassCode => -45645737;
    }

    /// <summary>
    /// Represents a data symbol that is defined externally and statically linked to the output obj file.
    /// </summary>
    public class ExternDataSymbolNode(Utf8String name) : ExternSymbolNode(name)
    {
        public override int ClassCode => 1428609964;

        protected override string GetName(NodeFactory factory) => $"ExternDataSymbolNode {ToString()}";
    }
}
