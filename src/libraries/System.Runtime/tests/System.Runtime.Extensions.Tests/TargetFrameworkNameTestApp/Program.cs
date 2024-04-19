// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;

namespace CustomAttributesTestApp
{
    internal class TargetFrameworkNameTestApp
    {
        internal static int Main()
        {
            return (AppContext.TargetFrameworkName == "DUMMY-TFA,Version=v0.0.1") ? 0 : -1;
        }
    }
}
