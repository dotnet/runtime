// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRCALL : EXPR
    {
        private EXPR _OptionalArguments;
        public EXPR GetOptionalArguments() { return _OptionalArguments; }
        public void SetOptionalArguments(EXPR value) { _OptionalArguments = value; }

        private EXPRMEMGRP _MemberGroup;
        public EXPRMEMGRP GetMemberGroup() { return _MemberGroup; }
        public void SetMemberGroup(EXPRMEMGRP value) { _MemberGroup = value; }

        public MethWithInst mwi;

        public PREDEFMETH PredefinedMethod;

        public NullableCallLiftKind nubLiftKind;
        public EXPR pConversions;
        public EXPR castOfNonLiftedResultToLiftedType;
    }
}
