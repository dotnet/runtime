// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRCAST : EXPR
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
