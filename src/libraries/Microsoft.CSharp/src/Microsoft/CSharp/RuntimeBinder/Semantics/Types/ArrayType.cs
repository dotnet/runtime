// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // ArrayType - a symbol representing an array.
    // ----------------------------------------------------------------------------

    internal sealed class ArrayType : CType
    {
        public ArrayType(CType elementType, int rank, bool isSZArray)
            : base(TypeKind.TK_ArrayType)
        {
            Rank = rank;
            IsSZArray = isSZArray;
            ElementType = elementType;
        }

        public int Rank { get; }

        public bool IsSZArray { get; }

        public CType ElementType { get; }

        // Returns the first non-array type in the parent chain.
        public CType BaseElementType
        {
            get
            {
                CType type = ElementType;
                while (type is ArrayType arr)
                {
                    type = arr.ElementType;
                }

                return type;
            }
        }

        public override bool IsReferenceType => true;

        public override bool IsUnsafe() => BaseElementType is PointerType;

        public override Type AssociatedSystemType
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get
            {
                Type elementType = ElementType.AssociatedSystemType;
                return IsSZArray ? elementType.MakeArrayType() : elementType.MakeArrayType(Rank);
            }
        }

        public override CType BaseOrParameterOrElementType => ElementType;

        public override FUNDTYPE FundamentalType => FUNDTYPE.FT_REF;

        public override ConstValKind ConstValKind => ConstValKind.IntPtr;

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public override AggregateType GetAts() => SymbolLoader.GetPredefindType(PredefinedType.PT_ARRAY);
    }
}
