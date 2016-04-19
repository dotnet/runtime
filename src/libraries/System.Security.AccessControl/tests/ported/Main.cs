using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace System.Security.AccessControl.Test
{
    public class MainAccessControlTests
    {
        public static int Main()
        {
            try
            {

                if (ACETest.Test() &&
                    AceEnumeratorTestCases.Test() &&
                    RawSecurityDescriptorTestCases.Test() &&
                    AuthorizationRuleTestCases.Test() &&
                    CommonSecurityDescriptorTestCases.Test() &&
                    DiscretionaryAclTestCases.Test() &&
                    RawAclTestCases.Test() &&
                    SystemAclTestCases.Test())
                {
                    Console.WriteLine("PASSED");
                    return 100;
                }
                else
                {
                    Console.WriteLine("FAILED");
                    return 101;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.Write("Exception: {0}", e.ToString());
                return 101;
            }
        }
    }
}