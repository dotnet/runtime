// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class CType
    {
        private protected CType(TypeKind kind)
        {
            TypeKind = kind;
        }

        [ExcludeFromCodeCoverage(Justification = "Should only be called through override")]
        public virtual Type AssociatedSystemType
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get => throw Error.InternalCompilerError();
        }

        public TypeKind TypeKind { get; }

        public virtual CType BaseOrParameterOrElementType => null;

        ////////////////////////////////////////////////////////////////////////////////
        // Given a symbol, determine its fundamental type. This is the type that
        // indicate how the item is stored and what instructions are used to reference
        // if. The fundamental types are:
        // one of the integral/float types (includes enums with that underlying type)
        // reference type
        // struct/value type
        public virtual FUNDTYPE FundamentalType => FUNDTYPE.FT_NONE;

        public virtual ConstValKind ConstValKind => ConstValKind.Int;

        ////////////////////////////////////////////////////////////////////////////////
        // Strips off ArrayType, ParameterModifierType, PointerType, PinnedType and optionally NullableType
        // and returns the result.
        public CType GetNakedType(bool fStripNub)
        {
            for (CType type = this; ;)
            {
                switch (type.TypeKind)
                {
                    default:
                        return type;

                    case TypeKind.TK_NullableType:
                        if (!fStripNub)
                        {
                            goto default;
                        }

                        goto case TypeKind.TK_ArrayType;

                    case TypeKind.TK_ArrayType:
                    case TypeKind.TK_ParameterModifierType:
                    case TypeKind.TK_PointerType:
                        type = type.BaseOrParameterOrElementType;
                        break;
                }
            }
        }

        public virtual CType StripNubs() => this;

        public virtual CType StripNubs(out bool wasNullable)
        {
            wasNullable = false;
            return this;
        }

        public virtual bool IsDelegateType => false;

        ////////////////////////////////////////////////////////////////////////////////
        // A few types are considered "simple" types for purposes of conversions and so
        // on. They are the fundamental types the compiler knows about for operators and
        // conversions.
        public virtual bool IsSimpleType => false;

        public virtual bool IsSimpleOrEnum => false;

        public virtual bool IsSimpleOrEnumOrString => false;

        ////////////////////////////////////////////////////////////////////////////////
        // A few types are considered "numeric" types. They are the fundamental number
        // types the compiler knows about for operators and conversions.
        public virtual bool IsNumericType => false;

        public virtual bool IsStructOrEnum => false;

        public virtual bool IsStructType => false;

        public virtual bool IsEnumType => false;

        public virtual bool IsInterfaceType => false;

        public virtual bool IsClassType => false;

        [ExcludeFromCodeCoverage(Justification = "Should only be called through override")]
        public virtual AggregateType UnderlyingEnumType => throw Error.InternalCompilerError();

        // Pointer types (or arrays of them) are the only unsafe types.
        // Note that generics may not be instantiated with pointer types
        public virtual bool IsUnsafe() => false;

        public virtual bool IsPredefType(PredefinedType pt) => false;

        public virtual bool IsPredefined => false;

        [ExcludeFromCodeCoverage(Justification = "Should only be called through override")]
        public virtual PredefinedType PredefinedType => throw Error.InternalCompilerError();

        public virtual bool IsStaticClass => false;

        // These check for AGGTYPESYMs, TYVARSYMs and others as appropriate.
        public virtual bool IsValueType => false;

        public virtual bool IsNonNullableValueType => false;

        public virtual bool IsReferenceType => false;

        [ExcludeFromCodeCoverage(Justification = "Should only be called through override")]
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public virtual AggregateType GetAts()
        {
            Debug.Fail("Bad type for AsAggregateType");
            return null;
        }
    }
}
