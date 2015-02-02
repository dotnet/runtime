// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRPROP : EXPR
    {
        // If we have this.prop = 123, but the implementation of the property is in the
        // base class, then the object is of the base class type. Note that to get
        // the object, we must go through the MEMGRP.
        //
        // "throughObject" is
        // of the type we are actually calling through.  (We need to know the
        // "through" type to ensure that protected semantics are correctly enforced.)

        public EXPR OptionalArguments;
        public EXPR GetOptionalArguments() { return OptionalArguments; }
        public void SetOptionalArguments(EXPR value) { OptionalArguments = value; }
        public EXPRMEMGRP MemberGroup;
        public EXPRMEMGRP GetMemberGroup() { return MemberGroup; }
        public void SetMemberGroup(EXPRMEMGRP value) { MemberGroup = value; }
        public EXPR OptionalObjectThrough;
        public EXPR GetOptionalObjectThrough() { return OptionalObjectThrough; }
        public void SetOptionalObjectThrough(EXPR value) { OptionalObjectThrough = value; }

        public PropWithType pwtSlot;
        public MethWithType mwtSet;
        public bool isBaseCall() { return 0 != (flags & EXPRFLAG.EXF_BASECALL); }
    }
}
