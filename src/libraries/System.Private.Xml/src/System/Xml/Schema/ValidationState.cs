// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    internal struct StateUnion
    {
        [FieldOffset(0)]
        public int State;  //DFA
        [FieldOffset(0)]
        public int AllElementsRequired; //AllContentValidator
        [FieldOffset(0)]
        public int CurPosIndex; //NFAContentValidator
        [FieldOffset(0)]
        public int NumberOfRunningPos; //RangeContentValidator
    }

    internal sealed class ValidationState
    {
        public bool IsNill;
        public bool IsDefault;
        public bool NeedValidateChildren;  // whether need to validate the children of this element
        public bool CheckRequiredAttribute; //PSVI
        public bool ValidationSkipped;
        public XmlSchemaContentProcessing ProcessContents;
        public XmlSchemaValidity Validity;
        public SchemaElementDecl? ElementDecl;            // ElementDecl
        public SchemaElementDecl? ElementDeclBeforeXsi; //elementDecl before its changed by that of xsi:type's
        public string? LocalName;
        public string? Namespace;
        public ConstraintStruct[]? Constr;

        public StateUnion CurrentState;

        //For content model validation
        public bool HasMatched;       // whether the element has been verified correctly

        //For NFAs
        private BitSet[]? _curPos;
        public BitSet[] CurPos => _curPos ??= new BitSet[2];

        //For all
        public BitSet? AllElementsSet;

        //For MinMaxNFA
        public List<RangePositionInfo>? RunningPositions;
        public bool TooComplex;
    };
}
