using System.Security.Principal;
using System.Threading;

class Program
{
    static int Main(string[] args)
    {
        // Thread.CurrentPrincipal calls AppDomain.CurrentDomain.GetThreadPrincipal() which
        // contains annotation attributes and will require WindowsPrincipal.GetDefaultInstance
        // and GenericPrincipal.GetDefaultInstance
        IPrincipal principal = Thread.CurrentPrincipal;
        return 100;
    }
}