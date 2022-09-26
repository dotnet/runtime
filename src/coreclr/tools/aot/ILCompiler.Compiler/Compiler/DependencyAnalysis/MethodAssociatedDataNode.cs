// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    [Flags]
    public enum AssociatedDataFlags : byte
    {
        None = 0,
        HasUnboxingStubTarget = 1,
    }

    /// <summary>
    /// This node contains any custom data that we'd like to associated with a method. The unwind info of the method
    /// will have a reloc to this custom data if it exists. Not all methods need custom data to be emitted.
    /// This custom data excludes gcinfo and ehinfo (they are written by ObjectWriter during obj file emission).
    /// </summary>
    public class MethodAssociatedDataNode : ObjectNode, ISymbolDefinitionNode
    {
        private IMethodNode _methodNode;

        public MethodAssociatedDataNode(IMethodNode methodNode)
        {
            Debug.Assert(!methodNode.Method.IsAbstract);
            Debug.Assert(methodNode.Method.GetCanonMethodTarget(CanonicalFormKind.Specific) == methodNode.Method);
            _methodNode = methodNode;
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection Section => ObjectNodeSection.ReadOnlyDataSection;
        public override bool StaticDependenciesAreComputed => true;
        public int Offset => 0;
        public override bool IsShareable => _methodNode.Method is InstantiatedMethod || EETypeNode.IsTypeNodeShareable(_methodNode.Method.OwningType);

        public override int ClassCode => 1055183914;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_methodNode, ((MethodAssociatedDataNode)other)._methodNode);
        }

        public virtual void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("_associatedData_").Append(nameMangler.GetMangledMethodName(_methodNode.Method));
        }

        public static bool MethodHasAssociatedData(NodeFactory factory, IMethodNode methodNode)
        {
            // Instantiating unboxing stubs. We need to store their non-unboxing target pointer (looked up by runtime)
            ISpecialUnboxThunkNode unboxThunk = methodNode as ISpecialUnboxThunkNode;
            if(unboxThunk != null && unboxThunk.IsSpecialUnboxingThunk)
                return true;

            return false;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            Debug.Assert(MethodHasAssociatedData(factory, _methodNode));

            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialAlignment(1);
            objData.AddSymbol(this);

            AssociatedDataFlags flags = AssociatedDataFlags.None;

            var flagsReservation = objData.ReserveByte();

            ISpecialUnboxThunkNode unboxThunkNode = _methodNode as ISpecialUnboxThunkNode;
            if (unboxThunkNode != null && unboxThunkNode.IsSpecialUnboxingThunk)
            {
                flags |= AssociatedDataFlags.HasUnboxingStubTarget;
                objData.EmitReloc(unboxThunkNode.GetUnboxingThunkTarget(factory), RelocType.IMAGE_REL_BASED_RELPTR32);
            }

            objData.EmitByte(flagsReservation, (byte)flags);

            return objData.ToObjectData();
        }
    }
}
