// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// An attribute used to indicate a GC transition should be skipped when making an unmanaged function call.
    /// </summary>
    /// <remarks>
    /// The eventual public attribute should replace this one: https://github.com/dotnet/corefx/issues/40740
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class SuppressGCTransitionAttribute : Attribute
    {
        public SuppressGCTransitionAttribute()
        {
        }
    }
}