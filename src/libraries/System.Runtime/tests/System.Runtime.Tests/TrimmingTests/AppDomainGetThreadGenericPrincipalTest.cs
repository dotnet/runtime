// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Principal;
using System.Threading;
using System.Diagnostics;

class Program
{
    static int Main(string[] args)
    {
        // Thread.CurrentPrincipal calls AppDomain.CurrentDomain.GetThreadPrincipal() which
        // contains annotation attributes and will require GenericPrincipal.GetDefaultInstance
        // after setting UnauthenticatedPrincipal as the PrincipalPolicy
        AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.UnauthenticatedPrincipal);
        IPrincipal genericPrincipal = Thread.CurrentPrincipal;
        if (genericPrincipal.GetType().Name != "GenericPrincipal")
        {
            return -1;
        }

        return 100;
    }
}
