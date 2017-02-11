// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class CType : ITypeOrNamespace
    {
        private TypeKind _typeKind;
        private Name _pName;

        private bool _fHasErrors;  // Whether anyituents have errors. This is immutable.
        private bool _fUnres;      // Whether anyituents are unresolved. This is immutable.
        private bool _isBogus;     // can't be used in our language -- unsupported type(s)
        private bool _checkedBogus; // Have we checked a method args/return for bogus types

        // Is and As methods.
        public AggregateType AsAggregateType() { return this as AggregateType; }
        public ErrorType AsErrorType() { return this as ErrorType; }
        public ArrayType AsArrayType() { return this as ArrayType; }
        public PointerType AsPointerType() { return this as PointerType; }
        public ParameterModifierType AsParameterModifierType() { return this as ParameterModifierType; }
        public NullableType AsNullableType() { return this as NullableType; }
        public TypeParameterType AsTypeParameterType() { return this as TypeParameterType; }

        public bool IsAggregateType() { return this is AggregateType; }
        public bool IsVoidType() { return this is VoidType; }
        public bool IsNullType() { return this is NullType; }
        public bool IsOpenTypePlaceholderType() { return this is OpenTypePlaceholderType; }
        public bool IsBoundLambdaType() { return this is BoundLambdaType; }
        public bool IsMethodGroupType() { return this is MethodGroupType; }
        public bool IsErrorType() { return this is ErrorType; }
        public bool IsArrayType() { return this is ArrayType; }
        public bool IsPointerType() { return this is PointerType; }
        public bool IsParameterModifierType() { return this is ParameterModifierType; }
        public bool IsNullableType() { return this is NullableType; }
        public bool IsTypeParameterType() { return this is TypeParameterType; }

        public bool IsWindowsRuntimeType()
        {
            return (AssociatedSystemType.GetTypeInfo().Attributes & TypeAttributes.WindowsRuntime) == TypeAttributes.WindowsRuntime;
        }

        public bool IsCollectionType()
        {
            if ((AssociatedSystemType.GetTypeInfo().IsGenericType &&
                 (AssociatedSystemType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IList<>) ||
                  AssociatedSystemType.GetTypeInfo().GetGenericTypeDefinition() == typeof(ICollection<>) ||
                  AssociatedSystemType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                  AssociatedSystemType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
                  AssociatedSystemType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>) ||
                  AssociatedSystemType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                  AssociatedSystemType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))) ||
                AssociatedSystemType == typeof(System.Collections.IList) ||
                AssociatedSystemType == typeof(System.Collections.ICollection) ||
                AssociatedSystemType == typeof(System.Collections.IEnumerable) ||
                AssociatedSystemType == typeof(System.Collections.Specialized.INotifyCollectionChanged) ||
                AssociatedSystemType == typeof(System.ComponentModel.INotifyPropertyChanged))
            {
                return true;
            }
            return false;
        }

        // API similar to System.Type
        public bool IsGenericParameter
        {
            get { return IsTypeParameterType(); }
        }

        private Type _associatedSystemType;
        public Type AssociatedSystemType
        {
            get
            {
                if (_associatedSystemType == null)
                {
                    _associatedSystemType = CalculateAssociatedSystemType(this);
                }

                return _associatedSystemType;
            }
        }

        private static Type CalculateAssociatedSystemType(CType src)
        {
            Type result = null;

            switch (src.GetTypeKind())
            {
                case TypeKind.TK_ArrayType:
                    ArrayType a = src.AsArrayType();
                    Type elementType = a.GetElementType().AssociatedSystemType;
                    if (a.rank == 1)
                    {
                        result = elementType.MakeArrayType();
                    }
                    else
                    {
                        result = elementType.MakeArrayType(a.rank);
                    }
                    break;

                case TypeKind.TK_NullableType:
                    NullableType n = src.AsNullableType();
                    Type underlyingType = n.GetUnderlyingType().AssociatedSystemType;
                    result = typeof(Nullable<>).MakeGenericType(underlyingType);
                    break;

                case TypeKind.TK_PointerType:
                    PointerType p = src.AsPointerType();
                    Type referentType = p.GetReferentType().AssociatedSystemType;
                    result = referentType.MakePointerType();
                    break;

                case TypeKind.TK_ParameterModifierType:
                    ParameterModifierType r = src.AsParameterModifierType();
                    Type parameterType = r.GetParameterType().AssociatedSystemType;
                    result = parameterType.MakeByRefType();
                    break;

                case TypeKind.TK_AggregateType:
                    result = CalculateAssociatedSystemTypeForAggregate(src.AsAggregateType());
                    break;

                case TypeKind.TK_TypeParameterType:
                    TypeParameterType t = src.AsTypeParameterType();
                    if (t.IsMethodTypeParameter())
                    {
                        MethodInfo meth = t.GetOwningSymbol().AsMethodSymbol().AssociatedMemberInfo as MethodInfo;
                        result = meth.GetGenericArguments()[t.GetIndexInOwnParameters()];
                    }
                    else
                    {
                        Type parentType = t.GetOwningSymbol().AsAggregateSymbol().AssociatedSystemType;
                        result = parentType.GetTypeInfo().GenericTypeParameters[t.GetIndexInOwnParameters()];
                    }
                    break;

                case TypeKind.TK_ArgumentListType:
                case TypeKind.TK_BoundLambdaType:
                case TypeKind.TK_ErrorType:
                case TypeKind.TK_MethodGroupType:
                case TypeKind.TK_NaturalIntegerType:
                case TypeKind.TK_NullType:
                case TypeKind.TK_OpenTypePlaceholderType:
                case TypeKind.TK_UnboundLambdaType:
                case TypeKind.TK_VoidType:

                default:
                    break;
            }

            Debug.Assert(result != null || src.GetTypeKind() == TypeKind.TK_AggregateType);
            return result;
        }

        private static Type CalculateAssociatedSystemTypeForAggregate(AggregateType aggtype)
        {
            AggregateSymbol agg = aggtype.GetOwningAggregate();
            TypeArray typeArgs = aggtype.GetTypeArgsAll();

            List<Type> list = new List<Type>();

            // Get each type arg.
            for (int i = 0; i < typeArgs.size; i++)
            {
                // Unnamed type parameter types are just placeholders.
                if (typeArgs.Item(i).IsTypeParameterType() && typeArgs.Item(i).AsTypeParameterType().GetTypeParameterSymbol().name == null)
                {
                    return null;
                }
                list.Add(typeArgs.Item(i).AssociatedSystemType);
            }

            Type[] systemTypeArgs = list.ToArray();
            Type uninstantiatedType = agg.AssociatedSystemType;

            if (uninstantiatedType.GetTypeInfo().IsGenericType)
            {
                try
                {
                    return uninstantiatedType.MakeGenericType(systemTypeArgs);
                }
                catch (ArgumentException)
                {
                    // If the constraints don't work, just return the type without substituting it.
                    return uninstantiatedType;
                }
            }
            return uninstantiatedType;
        }

        // ITypeOrNamespace
        public bool IsType() { return true; }
        public bool IsNamespace() { return false; }
        public AssemblyQualifiedNamespaceSymbol AsNamespace() { throw Error.InternalCompilerError(); }
        public CType AsType() { return this; }

        public TypeKind GetTypeKind() { return _typeKind; }
        public void SetTypeKind(TypeKind kind) { _typeKind = kind; }

        public Name GetName() { return _pName; }
        public void SetName(Name pName) { _pName = pName; }

        public bool checkBogus() { return _isBogus; }
        public bool getBogus() { return _isBogus; }
        public bool hasBogus() { return _checkedBogus; }
        public void setBogus(bool isBogus)
        {
            _isBogus = isBogus;
            _checkedBogus = true;
        }
        public bool computeCurrentBogusState()
        {
            if (hasBogus())
            {
                return checkBogus();
            }

            bool fBogus = false;

            switch (GetTypeKind())
            {
                case TypeKind.TK_ParameterModifierType:
                case TypeKind.TK_PointerType:
                case TypeKind.TK_ArrayType:
                case TypeKind.TK_NullableType:
                    if (GetBaseOrParameterOrElementType() != null)
                    {
                        fBogus = GetBaseOrParameterOrElementType().computeCurrentBogusState();
                    }
                    break;

                case TypeKind.TK_ErrorType:
                    setBogus(false);
                    break;

                case TypeKind.TK_AggregateType:
                    fBogus = AsAggregateType().getAggregate().computeCurrentBogusState();
                    for (int i = 0; !fBogus && i < AsAggregateType().GetTypeArgsAll().size; i++)
                    {
                        fBogus |= AsAggregateType().GetTypeArgsAll().Item(i).computeCurrentBogusState();
                    }
                    break;

                case TypeKind.TK_TypeParameterType:
                case TypeKind.TK_VoidType:
                case TypeKind.TK_NullType:
                case TypeKind.TK_OpenTypePlaceholderType:
                case TypeKind.TK_ArgumentListType:
                case TypeKind.TK_NaturalIntegerType:
                    setBogus(false);
                    break;

                default:
                    throw Error.InternalCompilerError();
                    //setBogus(false);
                    //break;
            }

            if (fBogus)
            {
                // Only set this if at least 1 declared thing is bogus
                setBogus(fBogus);
            }

            return hasBogus() && checkBogus();
        }

        // This call switches on the kind and dispatches accordingly. This should really only be 
        // used when dereferencing TypeArrays. We should consider refactoring our code to not 
        // need this type of thing - strongly typed handling of TypeArrays would be much better.
        public CType GetBaseOrParameterOrElementType()
        {
            switch (GetTypeKind())
            {
                case TypeKind.TK_ArrayType:
                    return AsArrayType().GetElementType();

                case TypeKind.TK_PointerType:
                    return AsPointerType().GetReferentType();

                case TypeKind.TK_ParameterModifierType:
                    return AsParameterModifierType().GetParameterType();

                case TypeKind.TK_NullableType:
                    return AsNullableType().GetUnderlyingType();

                default:
                    return null;
            }
        }

        public void InitFromParent()
        {
            Debug.Assert(!IsAggregateType());
            CType typePar = null;

            if (IsErrorType())
            {
                typePar = AsErrorType().GetTypeParent();
            }
            else
            {
                typePar = GetBaseOrParameterOrElementType();
            }

            _fHasErrors = typePar.HasErrors();
            _fUnres = typePar.IsUnresolved();
#if CSEE

            this.typeRes = this;
            if (!this.fUnres)
                this.tsRes = ktsImportMax;
            this.fDirty = typePar.fDirty;
            this.tsDirty = typePar.tsDirty;
#endif // CSEE
        }

        public bool HasErrors()
        {
            return _fHasErrors;
        }
        public void SetErrors(bool fHasErrors)
        {
            _fHasErrors = fHasErrors;
        }
        public bool IsUnresolved()
        {
            return _fUnres;
        }
        public void SetUnresolved(bool fUnres)
        {
            _fUnres = fUnres;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Given a symbol, determine its fundamental type. This is the type that 
        // indicate how the item is stored and what instructions are used to reference 
        // if. The fundamental types are:
        // one of the integral/float types (includes enums with that underlying type)
        // reference type
        // struct/value type
        public FUNDTYPE fundType()
        {
            switch (GetTypeKind())
            {
                case TypeKind.TK_AggregateType:
                    {
                        AggregateSymbol sym = AsAggregateType().getAggregate();

                        // Treat enums like their underlying types.
                        if (sym.IsEnum())
                        {
                            sym = sym.GetUnderlyingType().getAggregate();
                        }

                        if (sym.IsStruct())
                        {
                            // Struct type could be predefined (int, long, etc.) or some other struct.
                            if (sym.IsPredefined())
                                return PredefinedTypeFacts.GetFundType(sym.GetPredefType());
                            return FUNDTYPE.FT_STRUCT;
                        }
                        return FUNDTYPE.FT_REF;  // Interfaces, classes, delegates are reference types.
                    }

                case TypeKind.TK_TypeParameterType:
                    return FUNDTYPE.FT_VAR;

                case TypeKind.TK_ArrayType:
                case TypeKind.TK_NullType:
                    return FUNDTYPE.FT_REF;

                case TypeKind.TK_PointerType:
                    return FUNDTYPE.FT_PTR;

                case TypeKind.TK_NullableType:
                    return FUNDTYPE.FT_STRUCT;

                default:
                    return FUNDTYPE.FT_NONE;
            }
        }
        public ConstValKind constValKind()
        {
            if (isPointerLike())
            {
                return ConstValKind.IntPtr;
            }

            switch (fundType())
            {
                case FUNDTYPE.FT_I8:
                case FUNDTYPE.FT_U8:
                    return ConstValKind.Long;
                case FUNDTYPE.FT_STRUCT:
                    // Here we can either have a decimal type, or an enum 
                    // whose fundamental type is decimal.
                    Debug.Assert((getAggregate().IsEnum() && getAggregate().GetUnderlyingType().getPredefType() == PredefinedType.PT_DECIMAL)
                        || (isPredefined() && getPredefType() == PredefinedType.PT_DATETIME)
                        || (isPredefined() && getPredefType() == PredefinedType.PT_DECIMAL));

                    if (isPredefined() && getPredefType() == PredefinedType.PT_DATETIME)
                    {
                        return ConstValKind.Long;
                    }
                    return ConstValKind.Decimal;

                case FUNDTYPE.FT_REF:
                    if (isPredefined() && getPredefType() == PredefinedType.PT_STRING)
                    {
                        return ConstValKind.String;
                    }
                    else
                    {
                        return ConstValKind.IntPtr;
                    }
                case FUNDTYPE.FT_R4:
                    return ConstValKind.Float;
                case FUNDTYPE.FT_R8:
                    return ConstValKind.Double;
                case FUNDTYPE.FT_I1:
                    return ConstValKind.Boolean;
                default:
                    return ConstValKind.Int;
            }
        }
        public CType underlyingType()
        {
            if (IsAggregateType() && getAggregate().IsEnum())
                return getAggregate().GetUnderlyingType();
            return this;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Strips off ArrayType, ParameterModifierType, PointerType, PinnedType and optionally NullableType
        // and returns the result.
        public CType GetNakedType(bool fStripNub)
        {
            for (CType type = this; ;)
            {
                switch (type.GetTypeKind())
                {
                    default:
                        return type;

                    case TypeKind.TK_NullableType:
                        if (!fStripNub)
                            return type;
                        type = type.GetBaseOrParameterOrElementType();
                        break;
                    case TypeKind.TK_ArrayType:
                    case TypeKind.TK_ParameterModifierType:
                    case TypeKind.TK_PointerType:
                        type = type.GetBaseOrParameterOrElementType();
                        break;
                }
            }
        }
        public AggregateSymbol GetNakedAgg()
        {
            return GetNakedAgg(false);
        }

        private AggregateSymbol GetNakedAgg(bool fStripNub)
        {
            CType type = GetNakedType(fStripNub);
            if (type != null && type.IsAggregateType())
                return type.AsAggregateType().getAggregate();
            return null;
        }
        public AggregateSymbol getAggregate()
        {
            Debug.Assert(IsAggregateType());
            return AsAggregateType().GetOwningAggregate();
        }

        public CType StripNubs()
        {
            CType type;
            for (type = this; type.IsNullableType(); type = type.AsNullableType().GetUnderlyingType())
                ;
            return type;
        }
        public CType StripNubs(out int pcnub)
        {
            pcnub = 0;
            CType type;
            for (type = this; type.IsNullableType(); type = type.AsNullableType().GetUnderlyingType())
                (pcnub)++;
            return type;
        }

        public bool isDelegateType()
        {
            return (IsAggregateType() && getAggregate().IsDelegate());
        }

        ////////////////////////////////////////////////////////////////////////////////
        // A few types are considered "simple" types for purposes of conversions and so
        // on. They are the fundamental types the compiler knows about for operators and
        // conversions.
        public bool isSimpleType()
        {
            return (isPredefined() &&
                    PredefinedTypeFacts.IsSimpleType(getPredefType()));
        }
        public bool isSimpleOrEnum()
        {
            return isSimpleType() || isEnumType();
        }
        public bool isSimpleOrEnumOrString()
        {
            return isSimpleType() || isPredefType(PredefinedType.PT_STRING) || isEnumType();
        }

        private bool isPointerLike()
        {
            return IsPointerType() || isPredefType(PredefinedType.PT_INTPTR) || isPredefType(PredefinedType.PT_UINTPTR);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // A few types are considered "numeric" types. They are the fundamental number
        // types the compiler knows about for operators and conversions.
        public bool isNumericType()
        {
            return (isPredefined() &&
                    PredefinedTypeFacts.IsNumericType(getPredefType()));
        }
        public bool isStructOrEnum()
        {
            return (IsAggregateType() && (getAggregate().IsStruct() || getAggregate().IsEnum())) || IsNullableType();
        }
        public bool isStructType()
        {
            return IsAggregateType() && getAggregate().IsStruct() || IsNullableType();
        }
        public bool isEnumType()
        {
            return (IsAggregateType() && getAggregate().IsEnum());
        }
        public bool isInterfaceType()
        {
            return (IsAggregateType() && getAggregate().IsInterface());
        }
        public bool isClassType()
        {
            return (IsAggregateType() && getAggregate().IsClass());
        }
        public AggregateType underlyingEnumType()
        {
            Debug.Assert(isEnumType());
            return getAggregate().GetUnderlyingType();
        }
        public bool isUnsigned()
        {
            if (IsAggregateType())
            {
                AggregateType sym = AsAggregateType();
                if (sym.isEnumType())
                {
                    sym = sym.underlyingEnumType();
                }
                if (sym.isPredefined())
                {
                    PredefinedType pt = sym.getPredefType();
                    return pt == PredefinedType.PT_UINTPTR || pt == PredefinedType.PT_BYTE || (pt >= PredefinedType.PT_USHORT && pt <= PredefinedType.PT_ULONG);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return IsPointerType();
            }
        }
        public bool isUnsafe()
        {
            // Pointer types are the only unsafe types.
            // Note that generics may not be instantiated with pointer types
            return (this != null && (IsPointerType() || (IsArrayType() && AsArrayType().GetElementType().isUnsafe())));
        }
        public bool isPredefType(PredefinedType pt)
        {
            if (IsAggregateType())
                return AsAggregateType().getAggregate().IsPredefined() && AsAggregateType().getAggregate().GetPredefType() == pt;
            return (IsVoidType() && pt == PredefinedType.PT_VOID);
        }
        public bool isPredefined()
        {
            return IsAggregateType() && getAggregate().IsPredefined();
        }
        public PredefinedType getPredefType()
        {
            //ASSERT(isPredefined());
            return getAggregate().GetPredefType();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Is this type System.TypedReference or System.ArgIterator?
        // (used for errors because these types can't go certain places)

        public bool isSpecialByRefType()
        {
            // ArgIterator, TypedReference and RuntimeArgumentHandle are not supported.
            return false;
        }

        public bool isStaticClass()
        {
            AggregateSymbol agg = GetNakedAgg(false);
            if (agg == null)
                return false;

            if (!agg.IsStatic())
                return false;

            return true;
        }
        public bool computeManagedType(SymbolLoader symbolLoader)
        {
            if (IsVoidType())
                return false;

            switch (fundType())
            {
                case FUNDTYPE.FT_NONE:
                case FUNDTYPE.FT_REF:
                case FUNDTYPE.FT_VAR:
                    return true;

                case FUNDTYPE.FT_STRUCT:
                    if (IsNullableType())
                    {
                        return true;
                    }
                    else
                    {
                        AggregateSymbol aggT = getAggregate();

                        // See if we already know.
                        if (aggT.IsKnownManagedStructStatus())
                        {
                            return aggT.IsManagedStruct();
                        }

                        // Generics are always managed.
                        if (aggT.GetTypeVarsAll().size > 0)
                        {
                            aggT.SetManagedStruct(true);
                            return true;
                        }

                        // If the struct layout has an error, don't recurse its children.
                        if (aggT.IsLayoutError())
                        {
                            aggT.SetUnmanagedStruct(true);
                            return false;
                        }

                        // at this point we can only determine the managed status
                        // if we have members defined, otherwise we don't know the result
                        if (symbolLoader != null)
                        {
                            for (Symbol ps = aggT.firstChild; ps != null; ps = ps.nextChild)
                            {
                                if (ps.IsFieldSymbol() && !ps.AsFieldSymbol().isStatic)
                                {
                                    CType type = ps.AsFieldSymbol().GetType();
                                    if (type.computeManagedType(symbolLoader))
                                    {
                                        aggT.SetManagedStruct(true);
                                        return true;
                                    }
                                }
                            }

                            aggT.SetUnmanagedStruct(true);
                        }

                        return false;
                    }
                default:
                    return false;
            }
        }
        public CType GetDelegateTypeOfPossibleExpression()
        {
            if (isPredefType(PredefinedType.PT_G_EXPRESSION))
            {
                return AsAggregateType().GetTypeArgsThis().Item(0);
            }

            return this;
        }

        // These check for AGGTYPESYMs, TYVARSYMs and others as appropriate.
        public bool IsValType()
        {
            switch (GetTypeKind())
            {
                case TypeKind.TK_TypeParameterType:
                    return AsTypeParameterType().IsValueType();
                case TypeKind.TK_AggregateType:
                    return AsAggregateType().getAggregate().IsValueType();
                case TypeKind.TK_NullableType:
                    return true;
                default:
                    return false;
            }
        }
        public bool IsNonNubValType()
        {
            switch (GetTypeKind())
            {
                case TypeKind.TK_TypeParameterType:
                    return AsTypeParameterType().IsNonNullableValueType();
                case TypeKind.TK_AggregateType:
                    return AsAggregateType().getAggregate().IsValueType();
                case TypeKind.TK_NullableType:
                    return false;
                default:
                    return false;
            }
        }
        public bool IsRefType()
        {
            switch (GetTypeKind())
            {
                case TypeKind.TK_ArrayType:
                case TypeKind.TK_NullType:
                    return true;
                case TypeKind.TK_TypeParameterType:
                    return AsTypeParameterType().IsReferenceType();
                case TypeKind.TK_AggregateType:
                    return AsAggregateType().getAggregate().IsRefType();
                default:
                    return false;
            }
        }

        // A few types can be the same pointer value and not actually
        // be equivalent or convertible (like ANONMETHSYMs)
        public bool IsNeverSameType()
        {
            return IsBoundLambdaType() || IsMethodGroupType() || (IsErrorType() && !AsErrorType().HasParent());
        }
    }
}
