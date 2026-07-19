// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Indicates that the specified member requires the caller to be in an unsafe context.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property,
        Inherited = false,
        AllowMultiple = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        sealed class RequiresUnsafeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresUnsafeAttribute"/> class.
        /// </summary>
        public RequiresUnsafeAttribute() { }
    }
}
