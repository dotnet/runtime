// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    // Calls to methods marked with this attribute may be replaced at
    // some call sites with jit intrinsic expansions.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    internal sealed class IntrinsicAttribute : Attribute
    {
    }
}
