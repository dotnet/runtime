// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;

namespace Internal.Reflection.Tracing
{
    internal interface ITraceableTypeMember
    {
        // Returns the Name value *without recursing into the public Name implementation.*
        string MemberName { get; }

        // Returns the DeclaringType value *without recursing into the public DeclaringType implementation.*
        Type ContainingType { get; }
    }
}
