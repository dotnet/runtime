// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // AggregateType
    //
    // Represents a generic constructed (or instantiated) type. Parent is the AggregateSymbol.
    // ----------------------------------------------------------------------------

    internal sealed class AggregateType : CType
    {
        private AggregateType _baseType;  // This is the result of calling SubstTypeArray on the aggregate's baseClass.
        private TypeArray _ifacesAll;  // This is the result of calling SubstTypeArray on the aggregate's ifacesAll.
        private TypeArray _winrtifacesAll; //This is the list of collection interfaces implemented by a WinRT object.
        private Type _associatedSystemType;

        public AggregateType(AggregateSymbol parent, TypeArray typeArgsThis, AggregateType outerType)
            : base(TypeKind.TK_AggregateType)
        {

            Debug.Assert(typeArgsThis != null);
            OuterType = outerType;
            OwningAggregate = parent;
            TypeArgsThis = typeArgsThis;

            // Here we need to check our current type args. If we have an open placeholder,
            // then we need to have all open placeholders, and we want to flush
            // our outer type args so that they're open placeholders.
            //
            // This is because of the following scenario:
            //
            // class B<T>
            // {
            //     class C<U>
            //     {
            //     }
            //     class D
            //     {
            //         void Foo()
            //         {
            //             Type T = typeof(C<>);
            //         }
            //     }
            // }
            //
            // The outer type will be B<T>, but the inner type will be C<>. However,
            // this will eventually be represented in IL as B<>.C<>. As such, we should
            // keep our data structures clear - if we have one open type argument, then
            // all of them must be open type arguments.
            //
            // Ensure that invariant here.

            Debug.Assert(outerType == null || outerType.TypeArgsAll != null);
            TypeArgsAll = outerType != null ? TypeArray.Concat(outerType.TypeArgsAll, typeArgsThis) : typeArgsThis;
        }

        public bool? ConstraintError;       // Did the constraints check produce an error?

        // These two flags are used to track hiding within interfaces.
        // Their use and validity is always localized. See e.g. MemberLookup::LookupInInterfaces.
        public bool AllHidden;             // All members are hidden by a derived interface member.
        public bool DiffHidden;            // Members other than a specific kind are hidden by a derived interface member or class member.

        public AggregateType OuterType { get; }         // the outer type if this is a nested type

        public AggregateSymbol OwningAggregate { get; }

        public AggregateType BaseClass
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get
            {
                if (_baseType == null)
                {
                    Type baseSysType = AssociatedSystemType.BaseType;
                    if (baseSysType == null)
                    {
                        return null;
                    }

                    // If we have a generic type definition, then we need to set the
                    // base class to be our current base type, and use that to calculate
                    // our agg type and its base, then set it to be the generic version of the
                    // base type. This is because:
                    //
                    // Suppose we have Foo<T> : IFoo<T>
                    //
                    // Initially, the BaseType will be IFoo<Foo.T>, which gives us the substitution
                    // that we want to use for our agg type's base type. However, in the Symbol chain,
                    // we want the base type to be IFoo<IFoo.T>. So we need to substitute.
                    //
                    // If we don't have a generic type definition, then we just need to set our base
                    // class. This is so that if we have a base type that's generic, we'll be
                    // getting the correctly instantiated base type.
                    AggregateType baseClass = SymbolTable.GetCTypeFromType(baseSysType) as AggregateType;
                    Debug.Assert(baseClass != null);
                    _baseType = TypeManager.SubstType(baseClass, TypeArgsAll);
                }

                return _baseType;
            }
        }

        public IEnumerable<AggregateType> TypeHierarchy
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get
            {
                if (IsInterfaceType)
                {
                    yield return this;
                    foreach (AggregateType iface in IfacesAll.Items)
                    {
                        yield return iface;
                    }

                    yield return GetPredefinedAggregateGetThisTypeWithSuppressedMessage();
                }
                else
                {
                    for (AggregateType agg = this; agg != null; agg = agg.BaseClassWithSuppressedMessage)
                    {
                        yield return agg;
                    }
                }
            }
        }

        private AggregateType BaseClassWithSuppressedMessage
        {
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Workarounds https://github.com/mono/linker/issues/1906. All usages are marked as unsafe.")]
            get => BaseClass;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Workarounds https://github.com/mono/linker/issues/1906. All usages are marked as unsafe.")]
        private static AggregateType GetPredefinedAggregateGetThisTypeWithSuppressedMessage()
        {
            return PredefinedTypes.GetPredefinedAggregate(PredefinedType.PT_OBJECT).getThisType();
        }

        public TypeArray TypeArgsThis { get; }

        public TypeArray TypeArgsAll { get; }

        public TypeArray IfacesAll => _ifacesAll ??= TypeManager.SubstTypeArray(OwningAggregate.GetIfacesAll(), TypeArgsAll);

        private bool IsCollectionType
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get
            {
                Type sysType = AssociatedSystemType;
                if (sysType.IsGenericType)
                {
                    Type genType = sysType.GetGenericTypeDefinition();
                    return genType == typeof(IList<>)
                        || genType == typeof(ICollection<>)
                        || genType == typeof(IEnumerable<>)
                        || genType == typeof(IReadOnlyList<>)
                        || genType == typeof(IReadOnlyCollection<>)
                        || genType == typeof(IDictionary<,>)
                        || genType == typeof(IReadOnlyDictionary<,>);
                }

                return sysType == typeof(System.Collections.IList)
                    || sysType == typeof(System.Collections.ICollection)
                    || sysType == typeof(System.Collections.IEnumerable)
                    || sysType == typeof(System.Collections.Specialized.INotifyCollectionChanged)
                    || sysType == typeof(System.ComponentModel.INotifyPropertyChanged);
            }
        }

        public TypeArray WinRTCollectionIfacesAll
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get
            {
                if (_winrtifacesAll == null)
                {
                    List<CType> typeList = new List<CType>();
                    foreach (AggregateType type in IfacesAll.Items)
                    {
                        Debug.Assert(type.IsInterfaceType);
                        if (type.IsCollectionType)
                        {
                            typeList.Add(type);
                        }
                    }

                    _winrtifacesAll = TypeArray.Allocate(typeList.ToArray());
                }

                return _winrtifacesAll;
            }
        }

        public override bool IsReferenceType => OwningAggregate.IsRefType();

        public override bool IsNonNullableValueType => IsValueType;

        public override bool IsValueType => OwningAggregate.IsValueType();

        public override bool IsStaticClass => OwningAggregate.IsStatic();

        public override bool IsPredefined => OwningAggregate.IsPredefined();

        public override PredefinedType PredefinedType
        {
            get
            {
                Debug.Assert(IsPredefined);
                return OwningAggregate.GetPredefType();
            }
        }

        public override bool IsPredefType(PredefinedType pt)
        {
            AggregateSymbol agg = OwningAggregate;
            return agg.IsPredefined() && agg.GetPredefType() == pt;
        }

        public override bool IsDelegateType => OwningAggregate.IsDelegate();

        public override bool IsSimpleType
        {
            get
            {
                AggregateSymbol agg = OwningAggregate;
                return agg.IsPredefined() && PredefinedTypeFacts.IsSimpleType(agg.GetPredefType());
            }
        }

        public override bool IsSimpleOrEnum
        {
            get
            {
                AggregateSymbol agg = OwningAggregate;
                return agg.IsPredefined() ? PredefinedTypeFacts.IsSimpleType(agg.GetPredefType()) : agg.IsEnum();
            }
        }

        public override bool IsSimpleOrEnumOrString
        {
            get
            {
                AggregateSymbol agg = OwningAggregate;
                if (agg.IsPredefined())
                {
                    PredefinedType pt = agg.GetPredefType();
                    return PredefinedTypeFacts.IsSimpleType(pt) || pt == PredefinedType.PT_STRING;
                }

                return agg.IsEnum();
            }
        }

        public override bool IsNumericType
        {
            get
            {
                AggregateSymbol agg = OwningAggregate;
                return agg.IsPredefined() && PredefinedTypeFacts.IsNumericType(agg.GetPredefType());
            }
        }

        public override bool IsStructOrEnum
        {
            get
            {
                AggregateSymbol agg = OwningAggregate;
                return agg.IsStruct() || agg.IsEnum();
            }
        }

        public override bool IsStructType => OwningAggregate.IsStruct();

        public override bool IsEnumType => OwningAggregate.IsEnum();

        public override bool IsInterfaceType => OwningAggregate.IsInterface();

        public override bool IsClassType => OwningAggregate.IsClass();

        public override AggregateType UnderlyingEnumType
        {
            get
            {
                Debug.Assert(IsEnumType);
                return OwningAggregate.GetUnderlyingType();
            }
        }

        public override Type AssociatedSystemType
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get => _associatedSystemType ??= CalculateAssociatedSystemType();
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private Type CalculateAssociatedSystemType()
        {
            Type uninstantiatedType = OwningAggregate.AssociatedSystemType;
            if (uninstantiatedType.IsGenericType)
            {
                // Get each type arg.
                TypeArray typeArgs = TypeArgsAll;
                Type[] systemTypeArgs = new Type[typeArgs.Count];
                for (int i = 0; i < systemTypeArgs.Length; i++)
                {
                    // Unnamed type parameter types are just placeholders.
                    CType typeArg = typeArgs[i];
                    if (typeArg is TypeParameterType typeParamArg && typeParamArg.Symbol.name == null)
                    {
                        return null;
                    }

                    systemTypeArgs[i] = typeArg.AssociatedSystemType;
                }

                try
                {
                    return uninstantiatedType.MakeGenericType(systemTypeArgs);
                }
                catch (ArgumentException)
                {
                    // If the constraints don't work, just return the type without substituting it.
                }
            }

            return uninstantiatedType;
        }

        public override FUNDTYPE FundamentalType
        {
            get
            {
                AggregateSymbol sym = OwningAggregate;

                // Treat enums like their underlying types.
                if (sym.IsEnum())
                {
                    sym = sym.GetUnderlyingType().OwningAggregate;
                }
                else if (!sym.IsStruct())
                {
                    return FUNDTYPE.FT_REF; // Interfaces, classes, delegates are reference types.
                }

                // Struct type could be predefined (int, long, etc.) or some other struct.
                return sym.IsPredefined() ? PredefinedTypeFacts.GetFundType(sym.GetPredefType()) : FUNDTYPE.FT_STRUCT;
            }
        }

        public override ConstValKind ConstValKind
        {
            get
            {
                if (IsPredefType(PredefinedType.PT_INTPTR) || IsPredefType(PredefinedType.PT_UINTPTR))
                {
                    return ConstValKind.IntPtr;
                }

                switch (FundamentalType)
                {
                    case FUNDTYPE.FT_I8:
                    case FUNDTYPE.FT_U8:
                        return ConstValKind.Long;

                    case FUNDTYPE.FT_STRUCT:

                        // Here we can either have a decimal type, or an enum
                        // whose fundamental type is decimal.
                        Debug.Assert(
                            OwningAggregate.IsEnum() && OwningAggregate.GetUnderlyingType().PredefinedType == PredefinedType.PT_DECIMAL
                            || IsPredefined && PredefinedType == PredefinedType.PT_DATETIME
                            || IsPredefined && PredefinedType == PredefinedType.PT_DECIMAL);

                        return IsPredefined && PredefinedType == PredefinedType.PT_DATETIME
                            ? ConstValKind.Long
                            : ConstValKind.Decimal;

                    case FUNDTYPE.FT_REF:
                        return IsPredefined && PredefinedType == PredefinedType.PT_STRING
                            ? ConstValKind.String
                            : ConstValKind.IntPtr;

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
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public override AggregateType GetAts() => this;
    }
}
