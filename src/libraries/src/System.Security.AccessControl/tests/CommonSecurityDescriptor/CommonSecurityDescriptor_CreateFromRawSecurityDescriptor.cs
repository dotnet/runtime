// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class CommonSecurityDescriptor_CreateFromRawSecurityDescriptorTestCases
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_CreateFromRawSecurityDescriptor_TestData()
       {
           yield return new object[] { false, false, "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)"                                                                                                                                                                                                                                                                             , "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(A;;FA;;;BA)(A;;FA;;;BG)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)"                                                                                                                                                                                                                                                     , "O:LAG:SYD:AI(A;;FA;;;BA)(A;;FA;;;BG)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
           yield return new object[] { false, true , "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)"                                                                                                                                                                                                                                                                             , "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
           yield return new object[] { true , true , "O:LAG:SYD:AI(A;;FA;;;BA)(A;;FA;;;BG)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)"                                                                                                                                                                                                                                                     , "O:LAG:SYD:AI(A;;FA;;;BA)(A;;FA;;;BG)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)"                                                                                                                                                                                                                                                                                    , "O:LAG:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)" };
           yield return new object[] { true , false, "G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)"                                                                                                                                                                                                                                                                                        , "G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)" };
           yield return new object[] { true , false, "O:LAD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)"                                                                                                                                                                                                                                                                                        , "O:LAD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)" };
           yield return new object[] { true , false, "O:LAG:SYD:S:AI(AU;IDFA;FR;;;BA)"                                                                                                                                                                                                                                                                                                    , "O:LAG:SYD:S:AI(AU;IDFA;FR;;;BA)" };
           yield return new object[] { true , false, "O:LAG:SYS:AI(AU;IDFA;FR;;;BA)"                                                                                                                                                                                                                                                                                                      , "O:LAG:SYS:AI(AU;IDFA;FR;;;BA)" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(A;ID;FR;;;BA)S:"                                                                                                                                                                                                                                                                                                       , "O:LAG:SYD:AI(A;ID;FR;;;BA)S:" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(A;ID;FR;;;BA)"                                                                                                                                                                                                                                                                                                         , "O:LAG:SYD:AI(A;ID;FR;;;BA)" };
           yield return new object[] { true , false, "O:LAG:SYD:(A;;KA;;;SY)(A;;KA;;;S-1-5-19-1)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(OA;;CCDC;bf967a9c-0de6-11d0-a285-00aa003049e2;;BU)(OA;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BG)(OA;;CCDC;bf967aa8-0de6-11d0-a285-00aa003049e2;;PU)(A;;LCRPRC;;;BO)S:(AU;SAFA;CCDCSWWPSDWDWO;;;WD)"                              , "O:LAG:SYD:(A;;KA;;;SY)(A;;KA;;;S-1-5-19-1)(A;;LCRPRC;;;BO)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(OA;;CCDC;bf967a9c-0de6-11d0-a285-00aa003049e2;;BU)(OA;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BG)(OA;;CCDC;bf967aa8-0de6-11d0-a285-00aa003049e2;;PU)S:(AU;SAFA;CCDCSWWPSDWDWO;;;WD)" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", "O:LAG:SYD:AI(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", "O:LAG:SYD:AI(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)" };
           yield return new object[] { true , false, "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)", "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)" }; 
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            //test cases include the exceptions from the constructor
            CommonSecurityDescriptor sd = null;

            // test case 1: rawSecurityDescriptor is null
            Assert.Throws<ArgumentNullException>(() =>
            {
                sd = new CommonSecurityDescriptor(true, true, (RawSecurityDescriptor)null);
            });
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_CreateFromRawSecurityDescriptor_TestData))]
        public static void TestCreateFromRawSecurityDescriptor(bool isContainer, bool isDS, string rawSecurityDescriptorSddl, string verifierSddl)
        {
            RawSecurityDescriptor rawSecurityDescriptor = new RawSecurityDescriptor(rawSecurityDescriptorSddl);
            CommonSecurityDescriptor commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, rawSecurityDescriptor);

            // verify the result
            Assert.True((isContainer == commonSecurityDescriptor.IsContainer) && (isDS == commonSecurityDescriptor.IsDS));

            string resultSddlForm = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);
            Assert.True(string.Compare(verifierSddl, resultSddlForm, StringComparison.CurrentCultureIgnoreCase) == 0);
        }
    }
}
