// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Indicates that the specified method requires unsafe code that may not be available
    /// in all execution environments.
    /// </summary>
    /// <remarks>
    /// This allows tools to understand which methods are unsafe to call when targeting
    /// environments that do not support unsafe code.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    [Conditional("DEBUG")]
    internal sealed class RequiresUnsafeAttribute : Attribute
    {
    }
}
