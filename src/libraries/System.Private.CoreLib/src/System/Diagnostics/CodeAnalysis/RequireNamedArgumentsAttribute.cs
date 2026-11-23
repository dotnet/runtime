// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Specifies that callers should use a named argument for the attributed parameter, or for
    /// every parameter of the attributed member or delegate.
    /// </summary>
    /// <remarks>
    /// This attribute is intended for use by compilers and analyzers. It has no effect at run time.
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Method |
        AttributeTargets.Constructor |
        AttributeTargets.Property |
        AttributeTargets.Delegate |
        AttributeTargets.Parameter,
        AllowMultiple = false,
        Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        sealed class RequireNamedArgumentsAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequireNamedArgumentsAttribute"/> class.
        /// </summary>
        public RequireNamedArgumentsAttribute()
        {
        }
    }
}
