// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRTYPEOF : EXPR
    {
        public EXPRTYPEORNAMESPACE SourceType;
        public EXPRTYPEORNAMESPACE GetSourceType() { return SourceType; }
        public void SetSourceType(EXPRTYPEORNAMESPACE value) { SourceType = value; }
    }
}
