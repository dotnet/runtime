// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
    // Has no effect in .NET Core
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class SecurityTransparentAttribute : Attribute
    {
        public SecurityTransparentAttribute() { }
    }
}
