// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public struct GenericLookupResultContext
    {
        private readonly TypeSystemEntity _canonicalOwner;

        public readonly Instantiation TypeInstantiation;

        public readonly Instantiation MethodInstantiation;

        public TypeSystemEntity Context
        {
            get
            {
                if (_canonicalOwner is TypeDesc)
                {
                    var owningTypeDefinition = (MetadataType)((TypeDesc)_canonicalOwner).GetTypeDefinition();
                    Debug.Assert(owningTypeDefinition.Instantiation.Length == TypeInstantiation.Length);
                    Debug.Assert(MethodInstantiation.IsNull || MethodInstantiation.Length == 0);

                    return owningTypeDefinition.MakeInstantiatedType(TypeInstantiation);
                }

                Debug.Assert(_canonicalOwner is MethodDesc);
                MethodDesc owningMethodDefinition = ((MethodDesc)_canonicalOwner).GetTypicalMethodDefinition();
                Debug.Assert(owningMethodDefinition.Instantiation.Length == MethodInstantiation.Length);

                MethodDesc concreteMethod = owningMethodDefinition;
                if (!TypeInstantiation.IsNull && TypeInstantiation.Length > 0)
                {
                    TypeDesc owningType = owningMethodDefinition.OwningType;
                    Debug.Assert(owningType.Instantiation.Length == TypeInstantiation.Length);
                    concreteMethod = owningType.Context.GetMethodForInstantiatedType(owningMethodDefinition, ((MetadataType)owningType).MakeInstantiatedType(TypeInstantiation));
                }
                else
                {
                    Debug.Assert(owningMethodDefinition.OwningType.Instantiation.IsNull
                        || owningMethodDefinition.OwningType.Instantiation.Length == 0);
                }

                return concreteMethod.MakeInstantiatedMethod(MethodInstantiation);
            }
        }

        public GenericLookupResultContext(TypeSystemEntity canonicalOwner, Instantiation typeInst, Instantiation methodInst)
        {
            _canonicalOwner = canonicalOwner;
            TypeInstantiation = typeInst;
            MethodInstantiation = methodInst;
        }
    }

    /// <summary>
    /// Represents the result of a generic lookup within a canonical method body.
    /// The concrete artifact the generic lookup will result in can only be determined after substituting
    /// runtime determined types with a concrete generic context. Use
    /// <see cref="GetTarget(NodeFactory, Instantiation, Instantiation, GenericDictionaryNode)"/> to obtain the concrete
    /// node the result points to.
    /// </summary>
    public abstract class GenericLookupResult
    {
        protected abstract int ClassCode { get; }
        public abstract ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary);
        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);
        public abstract override string ToString();
        protected abstract int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer);
        protected abstract bool EqualsImpl(GenericLookupResult obj);
        protected abstract int GetHashCodeImpl();

        public sealed override bool Equals(object obj)
        {
            GenericLookupResult other = obj as GenericLookupResult;
            if (obj == null)
                return false;

            return ClassCode == other.ClassCode && EqualsImpl(other);
        }

        public sealed override int GetHashCode()
        {
            return ClassCode * 31 + GetHashCodeImpl();
        }

        public virtual void EmitDictionaryEntry(ref ObjectDataBuilder builder, NodeFactory factory, GenericLookupResultContext dictionary, GenericDictionaryNode dictionaryNode)
        {
            ISymbolNode target;
            try
            {
                target = GetTarget(factory, dictionary);
            }
            catch (TypeSystemException)
            {
                target = null;
            }

            if (target == null)
            {
                builder.EmitZeroPointer();
            }
            else
            {
                builder.EmitPointerReloc(target);
            }
        }

        public abstract NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory);

        // Call this api to get non-reloc dependencies that arise from use of a dictionary lookup
        public virtual IEnumerable<DependencyNodeCore<NodeFactory>> NonRelocDependenciesFromUsage(NodeFactory factory)
        {
            return Array.Empty<DependencyNodeCore<NodeFactory>>();
        }

        public class Comparer : IComparer<GenericLookupResult>
        {
            private TypeSystemComparer _comparer;

            public Comparer(TypeSystemComparer comparer)
            {
                _comparer = comparer;
            }

            public int Compare(GenericLookupResult x, GenericLookupResult y)
            {
                if (x == y)
                {
                    return 0;
                }

                int codeX = x.ClassCode;
                int codeY = y.ClassCode;
                if (codeX == codeY)
                {
                    Debug.Assert(x.GetType() == y.GetType());

                    int result = x.CompareToImpl(y, _comparer);

                    // We did a reference equality check above so an "Equal" result is not expected
                    Debug.Assert(result != 0);

                    return result;
                }
                else
                {
                    Debug.Assert(x.GetType() != y.GetType());
                    return codeX > codeY ? -1 : 1;
                }
            }
        }
    }

    /// <summary>
    /// Generic lookup result that points to an MethodTable.
    /// </summary>
    public sealed class TypeHandleGenericLookupResult : GenericLookupResult
    {
        private TypeDesc _type;

        protected override int ClassCode => 1623839081;

        public TypeHandleGenericLookupResult(TypeDesc type)
        {
            Debug.Assert(type.IsRuntimeDeterminedSubtype, "Concrete type in a generic dictionary?");
            _type = type;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            // We are getting a maximally constructable type symbol because this might be something passed to newobj.
            TypeDesc instantiatedType = _type.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);

            factory.TypeSystemContext.DetectGenericCycles(dictionary.Context, instantiatedType);

            return factory.MaximallyConstructableType(instantiatedType);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("TypeHandle_");
            sb.Append(nameMangler.GetMangledTypeName(_type));
        }

        public TypeDesc Type => _type;
        public override string ToString() => $"TypeHandle: {_type}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.TypeHandleDictionarySlot(_type);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_type, ((TypeHandleGenericLookupResult)other)._type);
        }

        protected override int GetHashCodeImpl()
        {
            return _type.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((TypeHandleGenericLookupResult)obj)._type == _type;
        }
    }


    /// <summary>
    /// Generic lookup result that points to an MethodTable where if the type is Nullable&lt;X&gt; the MethodTable is X
    /// </summary>
    public sealed class UnwrapNullableTypeHandleGenericLookupResult : GenericLookupResult
    {
        private TypeDesc _type;

        protected override int ClassCode => 53521918;

        public UnwrapNullableTypeHandleGenericLookupResult(TypeDesc type)
        {
            Debug.Assert(type.IsRuntimeDeterminedSubtype, "Concrete type in a generic dictionary?");
            _type = type;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            TypeDesc instantiatedType = _type.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);

            // Unwrap the nullable type if necessary
            if (instantiatedType.IsNullable)
                instantiatedType = instantiatedType.Instantiation[0];

            // We are getting a constructed type symbol because this might be something passed to newobj.
            return factory.ConstructedTypeSymbol(instantiatedType);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("UnwrapNullable_");
            sb.Append(nameMangler.GetMangledTypeName(_type));
        }

        public TypeDesc Type => _type;
        public override string ToString() => $"UnwrapNullable: {_type}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.UnwrapNullableTypeDictionarySlot(_type);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_type, ((UnwrapNullableTypeHandleGenericLookupResult)other)._type);
        }

        protected override int GetHashCodeImpl()
        {
            return _type.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((UnwrapNullableTypeHandleGenericLookupResult)obj)._type == _type;
        }
    }

    /// <summary>
    /// Generic lookup result that points to a RuntimeMethodHandle.
    /// </summary>
    internal sealed class MethodHandleGenericLookupResult : GenericLookupResult
    {
        private MethodDesc _method;

        protected override int ClassCode => 394272689;

        public MethodHandleGenericLookupResult(MethodDesc method)
        {
            Debug.Assert(method.IsRuntimeDeterminedExactMethod, "Concrete method in a generic dictionary?");
            _method = method;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            MethodDesc instantiatedMethod = _method.GetNonRuntimeDeterminedMethodFromRuntimeDeterminedMethodViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            return factory.RuntimeMethodHandle(instantiatedMethod);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("MethodHandle_");
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        public override string ToString() => $"MethodHandle: {_method}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.MethodLdTokenDictionarySlot(_method);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_method, ((MethodHandleGenericLookupResult)other)._method);
        }

        protected override int GetHashCodeImpl()
        {
            return _method.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((MethodHandleGenericLookupResult)obj)._method == _method;
        }
    }

    /// <summary>
    /// Generic lookup result that points to a RuntimeFieldHandle.
    /// </summary>
    internal sealed class FieldHandleGenericLookupResult : GenericLookupResult
    {
        private FieldDesc _field;

        protected override int ClassCode => -196995964;

        public FieldHandleGenericLookupResult(FieldDesc field)
        {
            Debug.Assert(field.OwningType.IsRuntimeDeterminedSubtype, "Concrete field in a generic dictionary?");
            _field = field;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            FieldDesc instantiatedField = _field.GetNonRuntimeDeterminedFieldFromRuntimeDeterminedFieldViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            return factory.RuntimeFieldHandle(instantiatedField);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("FieldHandle_");
            sb.Append(nameMangler.GetMangledFieldName(_field));
        }

        public override string ToString() => $"FieldHandle: {_field}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.FieldLdTokenDictionarySlot(_field);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_field, ((FieldHandleGenericLookupResult)other)._field);
        }

        protected override int GetHashCodeImpl()
        {
            return _field.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((FieldHandleGenericLookupResult)obj)._field == _field;
        }
    }

    /// <summary>
    /// Generic lookup result that points to a method dictionary.
    /// </summary>
    public sealed class MethodDictionaryGenericLookupResult : GenericLookupResult
    {
        private MethodDesc _method;

        protected override int ClassCode => -467418176;

        public MethodDictionaryGenericLookupResult(MethodDesc method)
        {
            Debug.Assert(method.IsRuntimeDeterminedExactMethod, "Concrete method in a generic dictionary?");
            _method = method;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            MethodDesc instantiatedMethod = _method.GetNonRuntimeDeterminedMethodFromRuntimeDeterminedMethodViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);

            factory.TypeSystemContext.DetectGenericCycles(dictionary.Context, instantiatedMethod);

            return factory.MethodGenericDictionary(instantiatedMethod);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("MethodDictionary_");
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        public MethodDesc Method => _method;
        public override string ToString() => $"MethodDictionary: {_method}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.MethodDictionaryDictionarySlot(_method);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_method, ((MethodDictionaryGenericLookupResult)other)._method);
        }

        protected override int GetHashCodeImpl()
        {
            return _method.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((MethodDictionaryGenericLookupResult)obj)._method == _method;
        }
    }

    /// <summary>
    /// Generic lookup result that is a function pointer.
    /// </summary>
    internal sealed class MethodEntryGenericLookupResult : GenericLookupResult
    {
        private MethodDesc _method;
        private bool _isUnboxingThunk;

        protected override int ClassCode => 1572293098;

        public MethodEntryGenericLookupResult(MethodDesc method, bool isUnboxingThunk)
        {
            Debug.Assert(method.IsRuntimeDeterminedExactMethod);
            _method = method;
            _isUnboxingThunk = isUnboxingThunk;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            MethodDesc instantiatedMethod = _method.GetNonRuntimeDeterminedMethodFromRuntimeDeterminedMethodViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            return factory.FatFunctionPointer(instantiatedMethod, _isUnboxingThunk);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            if (!_isUnboxingThunk)
                sb.Append("MethodEntry_");
            else
                sb.Append("UnboxMethodEntry_");

            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        public override string ToString() => $"MethodEntry: {_method}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            MethodDesc canonMethod = _method.GetCanonMethodTarget(CanonicalFormKind.Specific);

            //
            // For universal canonical methods, we don't need the unboxing stub really, because
            // the calling convention translation thunk will handle the unboxing (and we can avoid having a double thunk here)
            // We just need the flag in the native layout info signature indicating that we needed an unboxing stub
            //
            bool getUnboxingStubNode = _isUnboxingThunk && !canonMethod.IsCanonicalMethod(CanonicalFormKind.Universal);

            return factory.NativeLayout.MethodEntrypointDictionarySlot(
                _method,
                _isUnboxingThunk,
                factory.MethodEntrypoint(canonMethod, getUnboxingStubNode));
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            var otherEntry = (MethodEntryGenericLookupResult)other;
            int result = (_isUnboxingThunk ? 1 : 0) - (otherEntry._isUnboxingThunk ? 1 : 0);
            if (result != 0)
                return result;

            return comparer.Compare(_method, otherEntry._method);
        }

        protected override int GetHashCodeImpl()
        {
            return _method.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((MethodEntryGenericLookupResult)obj)._method == _method &&
                ((MethodEntryGenericLookupResult)obj)._isUnboxingThunk == _isUnboxingThunk;
        }
    }

    /// <summary>
    /// Generic lookup result that points to a dispatch cell.
    /// </summary>
    internal sealed class VirtualDispatchCellGenericLookupResult : GenericLookupResult
    {
        private MethodDesc _method;

        protected override int ClassCode => 643566930;

        public VirtualDispatchCellGenericLookupResult(MethodDesc method)
        {
            Debug.Assert(method.IsRuntimeDeterminedExactMethod);
            Debug.Assert(method.IsVirtual);
            Debug.Assert(method.OwningType.IsInterface);

            _method = method;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext context)
        {
            MethodDesc instantiatedMethod = _method.GetNonRuntimeDeterminedMethodFromRuntimeDeterminedMethodViaSubstitution(context.TypeInstantiation, context.MethodInstantiation);

            TypeSystemEntity contextOwner = context.Context;
            GenericDictionaryNode dictionary =
                contextOwner is TypeDesc ?
                (GenericDictionaryNode)factory.TypeGenericDictionary((TypeDesc)contextOwner) :
                (GenericDictionaryNode)factory.MethodGenericDictionary((MethodDesc)contextOwner);

            return factory.InterfaceDispatchCell(instantiatedMethod, dictionary);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("DispatchCell_");
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        public override string ToString() => $"DispatchCell: {_method}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.InterfaceCellDictionarySlot(_method);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_method, ((VirtualDispatchCellGenericLookupResult)other)._method);
        }

        protected override int GetHashCodeImpl()
        {
            return _method.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((VirtualDispatchCellGenericLookupResult)obj)._method == _method;
        }
    }

    /// <summary>
    /// Generic lookup result that points to the non-GC static base of a type.
    /// </summary>
    internal sealed class TypeNonGCStaticBaseGenericLookupResult : GenericLookupResult
    {
        private MetadataType _type;

        protected override int ClassCode => -328863267;

        public TypeNonGCStaticBaseGenericLookupResult(TypeDesc type)
        {
            Debug.Assert(type.IsRuntimeDeterminedSubtype, "Concrete static base in a generic dictionary?");
            Debug.Assert(type is MetadataType);
            _type = (MetadataType)type;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            var instantiatedType = (MetadataType)_type.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            return factory.TypeNonGCStaticsSymbol(instantiatedType);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("NonGCStaticBase_");
            sb.Append(nameMangler.GetMangledTypeName(_type));
        }

        public override string ToString() => $"NonGCStaticBase: {_type}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.NonGcStaticDictionarySlot(_type);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_type, ((TypeNonGCStaticBaseGenericLookupResult)other)._type);
        }

        protected override int GetHashCodeImpl()
        {
            return _type.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((TypeNonGCStaticBaseGenericLookupResult)obj)._type == _type;
        }
    }

    /// <summary>
    /// Generic lookup result that points to the threadstatic base index of a type.
    /// </summary>
    internal sealed class TypeThreadStaticBaseIndexGenericLookupResult : GenericLookupResult
    {
        private MetadataType _type;

        protected override int ClassCode => -177446371;

        public TypeThreadStaticBaseIndexGenericLookupResult(TypeDesc type)
        {
            Debug.Assert(type.IsRuntimeDeterminedSubtype, "Concrete static base in a generic dictionary?");
            Debug.Assert(type is MetadataType);
            _type = (MetadataType)type;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            var instantiatedType = (MetadataType)_type.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            return factory.TypeThreadStaticIndex(instantiatedType);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ThreadStaticBase_");
            sb.Append(nameMangler.GetMangledTypeName(_type));
        }

        public override string ToString() => $"ThreadStaticBase: {_type}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.ThreadStaticBaseIndexDictionarySlotNode(_type);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_type, ((TypeThreadStaticBaseIndexGenericLookupResult)other)._type);
        }

        protected override int GetHashCodeImpl()
        {
            return _type.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((TypeThreadStaticBaseIndexGenericLookupResult)obj)._type == _type;
        }
    }

    /// <summary>
    /// Generic lookup result that points to the GC static base of a type.
    /// </summary>
    public sealed class TypeGCStaticBaseGenericLookupResult : GenericLookupResult
    {
        private MetadataType _type;

        protected override int ClassCode => 429225829;

        public TypeGCStaticBaseGenericLookupResult(TypeDesc type)
        {
            Debug.Assert(type.IsRuntimeDeterminedSubtype, "Concrete static base in a generic dictionary?");
            Debug.Assert(type is MetadataType);
            _type = (MetadataType)type;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            var instantiatedType = (MetadataType)_type.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            return factory.TypeGCStaticsSymbol(instantiatedType);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("GCStaticBase_");
            sb.Append(nameMangler.GetMangledTypeName(_type));
        }

        public MetadataType Type => _type;
        public override string ToString() => $"GCStaticBase: {_type}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.GcStaticDictionarySlot(_type);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_type, ((TypeGCStaticBaseGenericLookupResult)other)._type);
        }

        protected override int GetHashCodeImpl()
        {
            return _type.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((TypeGCStaticBaseGenericLookupResult)obj)._type == _type;
        }
    }

    /// <summary>
    /// Generic lookup result that points to an object allocator.
    /// </summary>
    internal sealed class ObjectAllocatorGenericLookupResult : GenericLookupResult
    {
        private TypeDesc _type;

        protected override int ClassCode => -1671431655;

        public ObjectAllocatorGenericLookupResult(TypeDesc type)
        {
            Debug.Assert(type.IsRuntimeDeterminedSubtype, "Concrete type in a generic dictionary?");
            _type = type;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            TypeDesc instantiatedType = _type.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            return factory.ExternSymbol(JitHelper.GetNewObjectHelperForType(instantiatedType));
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("AllocObject_");
            sb.Append(nameMangler.GetMangledTypeName(_type));
        }

        public override string ToString() => $"AllocObject: {_type}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.AllocateObjectDictionarySlot(_type);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_type, ((ObjectAllocatorGenericLookupResult)other)._type);
        }

        protected override int GetHashCodeImpl()
        {
            return _type.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((ObjectAllocatorGenericLookupResult)obj)._type == _type;
        }
    }

    internal sealed class DefaultConstructorLookupResult : GenericLookupResult
    {
        private TypeDesc _type;

        protected override int ClassCode => -1391112482;

        public DefaultConstructorLookupResult(TypeDesc type)
        {
            Debug.Assert(type.IsRuntimeDeterminedSubtype, "Concrete type in a generic dictionary?");
            _type = type;
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            TypeDesc instantiatedType = _type.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            MethodDesc defaultCtor = Compilation.GetConstructorForCreateInstanceIntrinsic(instantiatedType);
            return factory.CanonicalEntrypoint(defaultCtor);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("DefaultCtor_");
            sb.Append(nameMangler.GetMangledTypeName(_type));
        }

        public override string ToString() => $"DefaultConstructor: {_type}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.DefaultConstructorDictionarySlot(_type);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_type, ((DefaultConstructorLookupResult)other)._type);
        }

        protected override int GetHashCodeImpl()
        {
            return _type.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            return ((DefaultConstructorLookupResult)obj)._type == _type;
        }
    }

    internal sealed class ConstrainedMethodUseLookupResult : GenericLookupResult
    {
        private MethodDesc _constrainedMethod;
        private TypeDesc _constraintType;
        private bool _directCall;

        protected override int ClassCode => -1525377658;

        public ConstrainedMethodUseLookupResult(MethodDesc constrainedMethod, TypeDesc constraintType, bool directCall)
        {
            _constrainedMethod = constrainedMethod;
            _constraintType = constraintType;
            _directCall = directCall;

            Debug.Assert(_constraintType.IsRuntimeDeterminedSubtype || _constrainedMethod.IsRuntimeDeterminedExactMethod, "Concrete type in a generic dictionary?");
            Debug.Assert(!_constrainedMethod.HasInstantiation || !_directCall, "Direct call to constrained generic method isn't supported");
        }

        public override IEnumerable<DependencyNodeCore<NodeFactory>> NonRelocDependenciesFromUsage(NodeFactory factory)
        {
            MethodDesc canonMethod = _constrainedMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);

            // If we're producing a full vtable for the type, we don't need to report virtual method use.
            // We also don't report virtual method use for generic virtual methods - tracking those is orthogonal.
            if (!factory.VTable(canonMethod.OwningType).HasFixedSlots && !canonMethod.HasInstantiation)
            {
                // Report the method as virtually used so that types that could be used here at runtime
                // have the appropriate implementations generated.
                // This covers instantiations created at runtime (MakeGeneric*). The statically present generic dictionaries
                // are already covered by the dependency analysis within the compiler because we call GetTarget for those.
                yield return factory.VirtualMethodUse(canonMethod);
            }
        }

        public override ISymbolNode GetTarget(NodeFactory factory, GenericLookupResultContext dictionary)
        {
            MethodDesc instantiatedConstrainedMethod = _constrainedMethod.GetNonRuntimeDeterminedMethodFromRuntimeDeterminedMethodViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            TypeDesc instantiatedConstraintType = _constraintType.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(dictionary.TypeInstantiation, dictionary.MethodInstantiation);
            MethodDesc implMethod;

            if (instantiatedConstrainedMethod.OwningType.IsInterface)
            {
                if (instantiatedConstrainedMethod.Signature.IsStatic)
                {
                    implMethod = instantiatedConstraintType.GetClosestDefType().ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(instantiatedConstrainedMethod);
                    if (implMethod == null)
                    {
                        DefaultInterfaceMethodResolution resolution =
                            instantiatedConstraintType.GetClosestDefType().ResolveVariantInterfaceMethodToDefaultImplementationOnType(instantiatedConstrainedMethod, out implMethod);
                        if (resolution != DefaultInterfaceMethodResolution.DefaultImplementation)
                        {
                            // TODO: diamond/reabstraction
                            ThrowHelper.ThrowInvalidProgramException();
                        }
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                implMethod = instantiatedConstraintType.GetClosestDefType().FindVirtualFunctionTargetMethodOnObjectType(instantiatedConstrainedMethod);
            }

            // AOT use of this generic lookup is restricted to finding methods on valuetypes (runtime usage of this slot in universal generics is more flexible)
            Debug.Assert(instantiatedConstraintType.IsValueType || (instantiatedConstrainedMethod.OwningType.IsInterface && instantiatedConstrainedMethod.Signature.IsStatic));

            factory.MetadataManager.NoteOverridingMethod(_constrainedMethod, implMethod);

            if (implMethod.Signature.IsStatic)
            {
                if (implMethod.GetCanonMethodTarget(CanonicalFormKind.Specific).IsSharedByGenericInstantiations)
                    return factory.ExactCallableAddress(implMethod);
                else
                    return factory.MethodEntrypoint(implMethod);
            }
            else if (implMethod.HasInstantiation)
            {
                return factory.ExactCallableAddress(implMethod);
            }
            else
            {
                return factory.CanonicalEntrypoint(implMethod);
            }
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ConstrainedMethodUseLookupResult_");
            sb.Append(nameMangler.GetMangledTypeName(_constraintType));
            sb.Append(nameMangler.GetMangledMethodName(_constrainedMethod));
            if (_directCall)
                sb.Append("Direct");
        }

        public override string ToString() => $"ConstrainedMethodUseLookupResult: {_constraintType} {_constrainedMethod} {_directCall}";

        public override NativeLayoutVertexNode TemplateDictionaryNode(NodeFactory factory)
        {
            return factory.NativeLayout.ConstrainedMethodUse(_constrainedMethod, _constraintType, _directCall);
        }

        protected override int CompareToImpl(GenericLookupResult other, TypeSystemComparer comparer)
        {
            var otherResult = (ConstrainedMethodUseLookupResult)other;
            int result = (_directCall ? 1 : 0) - (otherResult._directCall ? 1 : 0);
            if (result != 0)
                return result;

            result = comparer.Compare(_constraintType, otherResult._constraintType);
            if (result != 0)
                return result;

            return comparer.Compare(_constrainedMethod, otherResult._constrainedMethod);
        }

        protected override int GetHashCodeImpl()
        {
            return _constrainedMethod.GetHashCode() * 13 + _constraintType.GetHashCode();
        }

        protected override bool EqualsImpl(GenericLookupResult obj)
        {
            var other = (ConstrainedMethodUseLookupResult)obj;
            return _constrainedMethod == other._constrainedMethod &&
                _constraintType == other._constraintType &&
                _directCall == other._directCall;
        }
    }
}
