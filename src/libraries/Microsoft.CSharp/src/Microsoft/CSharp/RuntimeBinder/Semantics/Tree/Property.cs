// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRPROP : EXPR
    {
        // If we have this.prop = 123, but the implementation of the property is in the
        // base class, then the object is of the base class type. Note that to get
        // the object, we must go through the MEMGRP.
        //
        // "throughObject" is
        // of the type we are actually calling through.  (We need to know the
        // "through" type to ensure that protected semantics are correctly enforced.)

        private EXPR OptionalArguments;
        public EXPR GetOptionalArguments() { return OptionalArguments; }
        public void SetOptionalArguments(EXPR value) { OptionalArguments = value; }
        private EXPRMEMGRP MemberGroup;
        public EXPRMEMGRP GetMemberGroup() { return MemberGroup; }
        public void SetMemberGroup(EXPRMEMGRP value) { MemberGroup = value; }
        private EXPR OptionalObjectThrough;
        public EXPR GetOptionalObjectThrough() { return OptionalObjectThrough; }
        public void SetOptionalObjectThrough(EXPR value) { OptionalObjectThrough = value; }

        public PropWithType pwtSlot;
        public MethWithType mwtSet;
        public bool isBaseCall() { return 0 != (flags & EXPRFLAG.EXF_BASECALL); }
    }
}
