// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Internal copy of SuppressGCTransitionAttribute used only to enable files
// that use this to compile.  On any platforms that support this attribute,
// the real one exposed from CoreLib should be used.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class SuppressGCTransitionAttribute : Attribute { }
}
