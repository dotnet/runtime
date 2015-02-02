// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRCAST : EXPR
    {
        public EXPR Argument;
        public EXPR GetArgument() { return Argument; }
        public void SetArgument(EXPR expr) { Argument = expr; }
        public EXPRTYPEORNAMESPACE DestinationType;
        public EXPRTYPEORNAMESPACE GetDestinationType() { return DestinationType; }
        public void SetDestinationType(EXPRTYPEORNAMESPACE expr) { DestinationType = expr; }
        public bool IsBoxingCast() { return (flags & (EXPRFLAG.EXF_BOX | EXPRFLAG.EXF_FORCE_BOX)) != 0; }
    }
}
