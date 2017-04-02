// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprProperty : Expr, IExprWithArgs
    {
        // If we have this.prop = 123, but the implementation of the property is in the
        // base class, then the object is of the base class type. Note that to get
        // the object, we must go through the MEMGRP.
        //
        // "throughObject" is
        // of the type we are actually calling through.  (We need to know the
        // "through" type to ensure that protected semantics are correctly enforced.)

        public Expr OptionalArguments { get; set; }

        public ExprMemberGroup MemberGroup { get; set; }

        public Expr OptionalObject
        {
            get { return MemberGroup.OptionalObject; }
            set { MemberGroup.OptionalObject = value; }
        }

        public Expr OptionalObjectThrough { get; set; }

        public PropWithType PropWithTypeSlot { get; set; }

        public MethWithType MethWithTypeSet { get; set; }

        public bool IsBaseCall => 0 != (Flags & EXPRFLAG.EXF_BASECALL);

        SymWithType IExprWithArgs.GetSymWithType() => PropWithTypeSlot;
    }
}
