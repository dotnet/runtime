// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class CommonSecurityDescriptor_DiscretionaryAcl
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_DiscretionaryAcl_TestData()
       {
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", true , false, "0:1:1:BA:false:0" };
           yield return new object[] { true, false, "O:LAG:SYD:AIS:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)"                                                                                                                                                  , true , false, "0:1:1:BA:false:0" };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", true , false, null };
           yield return new object[] { true, false, "O:LAG:SYD:AIS:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)"                                                                                                                                                  , true , false, null };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", false, false, "0:1:1:BA:false:0" };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", true , true , "0:1:1:BA:false:0" };
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_DiscretionaryAcl_TestData))]
        public static void TestDiscretionaryAcl(bool isContainerSD, bool isDSSD, string sddl, bool isContainerDacl, bool isDSDacl, string newDaclStr)
        {
            bool result = false;
            bool isContainer = false;
            bool isDS = false;
            CommonSecurityDescriptor commonSecurityDescriptor = new CommonSecurityDescriptor(isContainerSD, isDSSD, sddl);
            DiscretionaryAcl dacl = newDaclStr == null ? null : new DiscretionaryAcl(isContainerDacl, isDSDacl, Utils.CreateRawAclFromString(newDaclStr));

            try
            {
                //save IsContainer, IsDS
                isContainer = commonSecurityDescriptor.IsContainer;
                isDS = commonSecurityDescriptor.IsDS;

                commonSecurityDescriptor.DiscretionaryAcl = dacl;
                //per shawn, the controlflag will show DaclPresent when dacl assigned is null
                if ((commonSecurityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0)
                {
                    if (dacl == null)
                    {
                        //a dacl with Allow Everyone Everything Ace should be assigned
                        if (commonSecurityDescriptor.DiscretionaryAcl.Count == 1 &&
                        commonSecurityDescriptor.IsContainer == isContainer &&
                        commonSecurityDescriptor.IsDS == isDS &&
                        Utils.VerifyDaclWithCraftedAce(commonSecurityDescriptor.IsContainer, commonSecurityDescriptor.IsDS, commonSecurityDescriptor.DiscretionaryAcl))
                        {
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    else if (dacl == commonSecurityDescriptor.DiscretionaryAcl)
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
                else
                {
                    result = false;
                }
            }
            catch (NullReferenceException)
            {
                Assert.Null(dacl);
                result = true;
            }
            catch (ArgumentException)
            {
                Assert.True((dacl.IsContainer != commonSecurityDescriptor.IsContainer) || (dacl.IsDS != commonSecurityDescriptor.IsDS));
                result = true;
            }
            Assert.True(result);
        }
    }
}