// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that the parameter contains Swift error handler.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class SwiftErrorReturnAttribute : Attribute
    {
        public SwiftErrorReturnAttribute()
        {
        }
    }
}
