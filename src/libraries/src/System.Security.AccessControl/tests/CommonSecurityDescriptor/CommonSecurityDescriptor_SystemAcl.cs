// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class CommonSecurityDescriptor_SystemAcl
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_SystemAcl_TestData()
       {
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", true , false, "64:2:1:BA:false:0" };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)"                                                                                                                                                                     , true , false, "64:2:1:BA:false:0 " };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", true , false, null };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)"                                                                                                                                                                     , true , false, null };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", false, false, "64:2:1:BA:false:0" };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", true , true , "64:2:1:BA:false:0" };
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_SystemAcl_TestData))]
        public static void TestSystemAcl(bool isContainerSD, bool isDSSD, string sddl, bool isContainerSacl, bool isDSSacl, string newSaclStr)
        {
            CommonSecurityDescriptor commonSecurityDescriptor = new CommonSecurityDescriptor(isContainerSD, isDSSD, sddl);
            SystemAcl sacl = newSaclStr == null ? null : new SystemAcl(isContainerSacl, isDSSacl, Utils.CreateRawAclFromString(newSaclStr));
            try
            {
                commonSecurityDescriptor.SystemAcl = sacl;
                Assert.True(sacl == commonSecurityDescriptor.SystemAcl);

                if (sacl != null)
                    Assert.True((commonSecurityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != 0);
                else
                    Assert.True((commonSecurityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) == 0);
            }
            catch (NullReferenceException)
            {
                Assert.Null(sacl);
            }
            catch (ArgumentException)
            {
                Assert.True((sacl.IsContainer != commonSecurityDescriptor.IsContainer) || (sacl.IsDS != commonSecurityDescriptor.IsDS));
            }
        }
    }
}