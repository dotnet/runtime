using System;
using System.Security.Principal;
using System.Threading;
using System.Diagnostics;

class Program
{
    static int Main(string[] args)
    {
        // Thread.CurrentPrincipal calls AppDomain.CurrentDomain.GetThreadPrincipal() which
        // contains annotation attributes and will require WindowsPrincipal.GetDefaultInstance
        // after setting that as the PrincipalPolicy
        AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
        IPrincipal principal = Thread.CurrentPrincipal;

        return 100;
    }
}