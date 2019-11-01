// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprMemberGroup : ExprWithType
    {
        public ExprMemberGroup(EXPRFLAG flags, Name name, TypeArray typeArgs, SYMKIND symKind, CType parentType, Expr optionalObject, CMemberLookupResults memberLookupResults)
            : base(ExpressionKind.MemberGroup, MethodGroupType.Instance)
        {
            Debug.Assert(
                (flags & ~(EXPRFLAG.EXF_CTOR | EXPRFLAG.EXF_INDEXER | EXPRFLAG.EXF_OPERATOR | EXPRFLAG.EXF_NEWOBJCALL
                           | EXPRFLAG.EXF_DELEGATE | EXPRFLAG.EXF_USERCALLABLE
                           | EXPRFLAG.EXF_MASK_ANY)) == 0);
            Flags = flags;
            Name = name;
            TypeArgs = typeArgs ?? TypeArray.Empty;
            SymKind = symKind;
            ParentType = parentType;
            OptionalObject = optionalObject;
            MemberLookupResults = memberLookupResults;
        }

        public Name Name { get; }

        public TypeArray TypeArgs { get; }

        public SYMKIND SymKind { get; }
        // The type containing the members. This may be a TypeParameterType or an AggregateType.
        // This may be NULL (if types is not NULL).

        // This is normally NULL - meaning the particular member
        // is unknown. For the invoke method on a delegate it is typically known.
        // After binding the group to arguments, this is set to the meth sym that is bound.
        // The object expression. NULL for a static invocation.
        public Expr OptionalObject { get; set; }

        // The owning call of this member group. NEVER visit this thing.
        // The list of the methods that the memgroup is binding to. This list is formulated after binding
        // the name of the method. When we've attempted to bind the arguments, we populate the MethPropWithInst list.
        public CMemberLookupResults MemberLookupResults { get; }

        public CType ParentType { get; }

        public bool IsDelegate => (Flags & EXPRFLAG.EXF_DELEGATE) != 0;
    }
}
