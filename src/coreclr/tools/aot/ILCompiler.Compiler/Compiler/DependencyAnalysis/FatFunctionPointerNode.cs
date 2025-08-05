// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a fat function pointer - a data structure that captures a pointer to a canonical
    /// method body along with the instantiation context the canonical body requires.
    /// Pointers to these structures can be created by e.g. ldftn/ldvirtftn of a method with a canonical body.
    /// </summary>
    public class FatFunctionPointerNode : DehydratableObjectNode, IMethodNode, ISymbolDefinitionNode
    {
        private readonly bool _isUnboxingStub;
        private readonly bool _isAddressTaken;

        public bool IsUnboxingStub => _isUnboxingStub;

        public FatFunctionPointerNode(MethodDesc methodRepresented, bool isUnboxingStub, bool addressTaken)
        {
            // We should not create these for methods that don't have a canonical method body
            Debug.Assert(methodRepresented.GetCanonMethodTarget(CanonicalFormKind.Specific) != methodRepresented);

            Method = methodRepresented;
            _isUnboxingStub = isUnboxingStub;
            _isAddressTaken = addressTaken;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            string prefix = $"__fat{(_isUnboxingStub ? "unbox" : "")}{(_isAddressTaken ? "addresstaken" : "")}pointer_";
            sb.Append(prefix).Append(nameMangler.GetMangledMethodName(Method));
        }

        int ISymbolDefinitionNode.Offset => 0;
        int ISymbolNode.Offset => Method.Context.Target.Architecture == TargetArchitecture.Wasm32 ? 1 << 31 : 2;

        public override bool IsShareable => true;

        public MethodDesc Method { get; }

        protected override ObjectNodeSection GetDehydratedSection(NodeFactory factory)
        {
            if (factory.Target.IsWindows)
                return ObjectNodeSection.ReadOnlyDataSection;
            else
                return ObjectNodeSection.DataSection;
        }

        public override bool StaticDependenciesAreComputed => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        protected override ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly = false)
        {
            var builder = new ObjectDataBuilder(factory, relocsOnly);

            // These need to be aligned the same as method bodies because they show up in same contexts
            // (macOS ARM64 has even stricter alignment requirement for the linker, so round up to pointer size)
            Debug.Assert(factory.Target.MinimumFunctionAlignment <= factory.Target.PointerSize);
            builder.RequireInitialAlignment(factory.Target.PointerSize);

            builder.AddSymbol(this);

            MethodDesc canonMethod = Method.GetCanonMethodTarget(CanonicalFormKind.Specific);

            // Pointer to the canonical body of the method
            ISymbolNode target = _isAddressTaken
                ? factory.AddressTakenMethodEntrypoint(canonMethod, _isUnboxingStub)
                : factory.MethodEntrypoint(canonMethod, _isUnboxingStub);
            builder.EmitPointerReloc(target);

            // Find out what's the context to use
            ISortableSymbolNode contextParameter;
            if (canonMethod.RequiresInstMethodDescArg())
            {
                contextParameter = factory.MethodGenericDictionary(Method);
            }
            else
            {
                Debug.Assert(canonMethod.RequiresInstMethodTableArg());

                // Ask for a constructed type symbol because we need the vtable to get to the dictionary
                contextParameter = factory.ConstructedTypeSymbol(Method.OwningType);
            }

            // The next entry is a pointer to the context to be used for the canonical method
            builder.EmitPointerReloc(contextParameter);

            return builder.ToObjectData();
        }

        public override int ClassCode => 190463489;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            var compare = _isUnboxingStub.CompareTo(((FatFunctionPointerNode)other)._isUnboxingStub);
            if (compare != 0)
                return compare;

            compare = _isAddressTaken.CompareTo(((FatFunctionPointerNode)other)._isAddressTaken);
            if (compare != 0)
                return compare;

            return comparer.Compare(Method, ((FatFunctionPointerNode)other).Method);
        }
    }
}
