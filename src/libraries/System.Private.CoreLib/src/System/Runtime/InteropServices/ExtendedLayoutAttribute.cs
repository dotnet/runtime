// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Indicates the layout rules for a value type at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false)]
    public sealed class ExtendedLayoutAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedLayoutAttribute"/> class with the specified layout kind.
        /// </summary>
        /// <param name="layoutKind">The layout algorithm to use for this value type.</param>
        public ExtendedLayoutAttribute(ExtendedLayoutKind layoutKind)
        {
        }
    }
}
