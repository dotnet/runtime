// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime
{
    // R2R compilation matches this attribute by namespace + name (see
    // CorInfoImpl.ReadyToRun.ShouldCodeNotBeCompiledIntoFinalImage), so a local copy of the
    // CoreLib-internal attribute is sufficient to force the annotated method to be interpreted
    // (or otherwise excluded from the final R2R image) when a Methodical test is R2R compiled.
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}
