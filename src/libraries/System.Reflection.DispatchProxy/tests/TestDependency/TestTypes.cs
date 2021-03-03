// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace DispatchProxyTestDependency
{
    internal interface TestType_IExternalNonPublicHiService
    {
        string Hi(string message);
    }

    internal class TestType_ExternalNonPublicBaseClassForProxy : DispatchProxy
    {
        protected override object Invoke(MethodInfo targetMethod, object[] args) => null;
    }
}
