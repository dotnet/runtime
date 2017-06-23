// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class CreateFromBinaryFormTestCases
    {
        public static IEnumerable<object[]> CommonSecurityDescriptor_CreateFromBinaryForm_TestData()
        {
            yield return new object[] { false, false, "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)", 0, "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(A;;FA;;;BA)(A;;FA;;;BG)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)", 1, "O:LAG:SYD:AI(A;;FA;;;BA)(A;;FA;;;BG)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
            yield return new object[] { false, true, "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)", 0, "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
            yield return new object[] { true, true, "O:LAG:SYD:AI(A;;FA;;;BA)(A;;FA;;;BG)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)", 100, "O:LAG:SYD:AI(A;;FA;;;BA)(A;;FA;;;BG)(A;ID;FA;;;BA)(A;ID;FA;;;BO)(A;ID;FA;;;SY)" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 0, "O:LAG:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)" };
            yield return new object[] { true, false, "G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 10000, "G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)" };
            yield return new object[] { true, false, "O:LAD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 0, "O:LAD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)" };
            yield return new object[] { true, false, "O:LAG:SYD:S:AI(AU;IDFA;FR;;;BA)", 0, "O:LAG:SYD:S:AI(AU;IDFA;FR;;;BA)" };
            yield return new object[] { true, false, "O:LAG:SYS:AI(AU;IDFA;FR;;;BA)", 0, "O:LAG:SYS:AI(AU;IDFA;FR;;;BA)" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(A;ID;FR;;;BA)S:", 0, "O:LAG:SYD:AI(A;ID;FR;;;BA)S:" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(A;ID;FR;;;BA)", 0, "O:LAG:SYD:AI(A;ID;FR;;;BA)" };
            yield return new object[] { true, false, "O:LAG:SYD:(A;;KA;;;SY)(A;;KA;;;S-1-5-19-1)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(OA;;CCDC;bf967a9c-0de6-11d0-a285-00aa003049e2;;BU)(OA;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BG)(OA;;CCDC;bf967aa8-0de6-11d0-a285-00aa003049e2;;PU)(A;;LCRPRC;;;BO)S:(AU;SAFA;CCDCSWWPSDWDWO;;;WD)", 0, "O:LAG:SYD:(A;;KA;;;SY)(A;;KA;;;S-1-5-19-1)(A;;LCRPRC;;;BO)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(OA;;CCDC;bf967a9c-0de6-11d0-a285-00aa003049e2;;BU)(OA;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BG)(OA;;CCDC;bf967aa8-0de6-11d0-a285-00aa003049e2;;PU)S:(AU;SAFA;CCDCSWWPSDWDWO;;;WD)" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", 0, "O:LAG:SYD:AI(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", 0, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", 0, "O:LAG:SYD:AI(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", 0, "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)" };
            yield return new object[] { true, false, "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)", 0, "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)" };
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            CommonSecurityDescriptor commonSecurityDescriptor = null;

            // Case1, null binary form
            Assert.Throws<ArgumentNullException>(() =>
            {
                commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, null, 0);
                // expect to throw exception but not
            });

            // case 2: offset < 0
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, new byte[24], -1);
                // expect to throw exception but not
            });

            // case 3: binaryForm.Length - offset < HeaderLength
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, new byte[24], 5);
                // expect to throw exception but not
            });

            // case 4: binaryForm[offset + 0] != Revision
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, new byte[24], 0);
                // expect to throw exception but not
            });

            // case 5: ControlFlags.SelfRelative is not set
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                byte[] binaryForm = new byte[24];
                for (int i = 0; i < binaryForm.Length; i++)
                    binaryForm[i] = 0;
                //set correct Revision
                binaryForm[0] = 1;
                commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, binaryForm, 0);
                // expect to throw exception but not
            });

            // Case 6, parameter binaryForm is empty
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, new byte[0], 0);
                // expect to throw exception but not
            });

            // Case 7, an array of garbage
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                byte[] binaryForm = { 1, 0, 0, 128, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                commonSecurityDescriptor = new CommonSecurityDescriptor(false, false, binaryForm, 0);
                // expect to throw exception
            });
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_CreateFromBinaryForm_TestData))]
        private static void TestCreateFromBinaryForm(bool isContainer, bool isDS, string sddl, int offset, string verifierSddl)
        {
            CommonSecurityDescriptor commonSecurityDescriptor = null;
            byte[] binaryForm;
            Assert.Equal(0, Utils.CreateBinaryArrayFromSddl(sddl, out binaryForm));
            byte[] tempBForm = null;
            if (offset > 0)
            {
                //copy the binaryform to a new array, start at offset
                tempBForm = new byte[binaryForm.Length + offset];
                for (int i = 0; i < binaryForm.Length; i++)
                {
                    tempBForm[i + offset] = binaryForm[i];
                }
                commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, tempBForm, offset);
            }
            else
            {
                commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, binaryForm, offset);
            }
            string resultSddlForm = commonSecurityDescriptor.GetSddlForm(AccessControlSections.All);
            Assert.True(String.Compare(verifierSddl, resultSddlForm, StringComparison.CurrentCultureIgnoreCase) == 0);
        }
    }
}
