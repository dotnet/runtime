// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public interface IInliningPolicy
    {
        bool CanInline(MethodDesc caller, MethodDesc callee);
    }
}
