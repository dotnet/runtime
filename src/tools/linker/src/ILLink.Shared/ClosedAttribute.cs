// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace StaticCs
{
    [AttributeUsage(AttributeTargets.Enum)]
    [Conditional("EMIT_STATICCS_CLOSEDATTRIBUTE")]
    internal sealed class ClosedAttribute : Attribute
    {
        public ClosedAttribute() { }
    }
}
