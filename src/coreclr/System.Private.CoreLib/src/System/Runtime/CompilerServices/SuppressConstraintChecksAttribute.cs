// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates to the compiler that constraint checks should be suppressed
    /// and will instead be enforced at run-time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
    internal sealed class SuppressConstraintChecksAttribute : Attribute
    { }
}
