// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
    // UnverifiableCodeAttribute:
    //  Indicates that the target module contains unverifiable code.
    [AttributeUsage(AttributeTargets.Module, AllowMultiple = true, Inherited = false)]
    public sealed class UnverifiableCodeAttribute : Attribute
    {
        public UnverifiableCodeAttribute() { }
    }
}
