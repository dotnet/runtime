// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    /// <summary>
    /// Code options for codegen in this context.
    /// These options are used for providing optional optimizations depending on available APIs.
    /// </summary>
    /// <param name="SkipInit">Skip initialization of locals when possible.</param>
    public record struct CodeEmitOptions(bool SkipInit);
}
