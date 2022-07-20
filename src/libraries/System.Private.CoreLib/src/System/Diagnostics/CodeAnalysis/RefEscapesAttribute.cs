// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Used to indicate a byref escapes and is not scoped.
    /// </summary>
    /// <remarks>
    /// There are several cases where the C# compiler treats a <c>ref</c> as implicitly
    /// <c>scoped</c> - where the compiler does not allow the <c>ref</c> to escape the method.
    ///
    /// For example:
    /// 1. <c>this</c> for struct instance methods
    /// 2. <c>ref</c> parameters that refer to <c>ref struct</c> types
    /// 3. <c>out</c> parameters
    ///
    /// This attribute is used in those instances where the <c>ref</c> should be allowed to escape.
    /// </remarks>
    [AttributeUsageAttribute(
        AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class RefEscapesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RefEscapesAttribute"/> class.
        /// </summary>
        public RefEscapesAttribute() { }
    }
}
