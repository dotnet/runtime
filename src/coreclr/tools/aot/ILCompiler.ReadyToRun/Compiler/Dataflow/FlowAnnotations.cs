// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class FlowAnnotations
    {
        public bool RequiresDataflowAnalysisDueToSignature(FieldDesc field) => false;

        public bool RequiresDataflowAnalysisDueToSignature(MethodDesc method) => false;
    }
}
