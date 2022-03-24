// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    public enum HelperEntrypoint
    {
        EnsureClassConstructorRunAndReturnGCStaticBase,
        EnsureClassConstructorRunAndReturnNonGCStaticBase,
        EnsureClassConstructorRunAndReturnThreadStaticBase,
        GetThreadStaticBaseForType,
    }
}
