// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // Used to string together methods in the pool of available methods...
    internal class CandidateFunctionMember
    {
        public CandidateFunctionMember(MethPropWithInst mpwi, TypeArray @params, byte ctypeLift, bool fExpanded)
        {
            this.mpwi = mpwi;
            this.@params = @params;
            this.ctypeLift = ctypeLift;
            this.fExpanded = fExpanded;
        }
        public MethPropWithInst mpwi;
        // params is the result of type variable substitution on either mpwi.MethProp()->params or
        // an expansion of mpwi.MethProp()->params (for a param array).
        public TypeArray @params;
        public byte ctypeLift; // How many parameter types are lifted (for tie-breaking).
        public bool fExpanded; // Whether the params came from expanding mpwi.MethProp()->params.
    }
}
