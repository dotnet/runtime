// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class CommonSecurityDescriptor_PurgeAccessControl
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_PurgeAccessControl_TestData()
       {
           yield return new object[] { true, false, "O:LAG:SYD:AIS:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)"                                                                                                                                                  , "BA", "O:LAG:SYD:AIS:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)" };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", "BO", "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)" };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)"                                                   , "BA", "O:LAG:SYD:AI(D;OI;WO;;;BG)(A;OI;FR;;;BG)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)" };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", "BA", "O:LAG:SYD:AI(D;OI;WO;;;BG)(A;OI;FR;;;BG)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)" };
           yield return new object[] { true, false, "O:LAG:SYD:AI(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)"                                                                                               , "BA", "O:LAG:SYD:AIS:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)" };
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            CommonSecurityDescriptor sd = null;

            // test case 1: sid is null
            Assert.Throws<ArgumentNullException>(() =>
            {
                sd = new CommonSecurityDescriptor(true, false, (ControlFlags)0, null, null, null, null);
                sd.PurgeAccessControl(null);
                // expect to throw exception but not
            });
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_PurgeAccessControl_TestData))]
        public static void TestPurgeAccessControl(bool isContainer, bool isDS, string sddl, string sidString, string verifierSddl)
        {
            CommonSecurityDescriptor commonSecurityDescriptor = null;
            string resultSddl = null;
            SecurityIdentifier sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sidString));
            commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);
            commonSecurityDescriptor.PurgeAccessControl(sid);
            resultSddl = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);
            Assert.False(resultSddl != verifierSddl);
        }
    }
}
