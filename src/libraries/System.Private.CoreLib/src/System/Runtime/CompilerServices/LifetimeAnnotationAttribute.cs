// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This type is defined until we consume the C# 11 compiler.
    /// </summary>
    /// <remarks>
    /// Also remove in the reference assemblies.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class LifetimeAnnotationAttribute : Attribute
    {
        public LifetimeAnnotationAttribute(bool isRefScoped, bool isValueScoped)
        {
            IsRefScoped = isRefScoped;
            IsValueScoped = isValueScoped;
        }
        public bool IsRefScoped { get; }
        public bool IsValueScoped { get; }
    }
}
