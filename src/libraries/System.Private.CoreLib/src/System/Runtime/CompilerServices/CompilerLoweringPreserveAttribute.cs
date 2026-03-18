// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// When applied to an attribute class, instructs the compiler to flow applications of that attribute,
    /// from source code down to compiler-generated symbols. This can help IL-based analysis tools.
    /// </summary>
    /// <remarks>
    /// One example where this attribute applies is in C# primary constructor parameters. If an attribute
    /// marked with <see cref="CompilerLoweringPreserveAttribute"/> gets applied to a primary constructor
    /// parameter, the attribute will also be applied to any compiler-generated fields storing that parameter.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    sealed class CompilerLoweringPreserveAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerLoweringPreserveAttribute"/> class.
        /// </summary>
        public CompilerLoweringPreserveAttribute() { }
    }
}
