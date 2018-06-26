// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class CommonSecurityDescriptor_GetBinaryForm
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_GetBinaryForm_TestData()
       {
           yield return new object[] { true, false, "O:LAG:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", "O:LAG:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", "G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 100 };
           yield return new object[] { true, false, "O:LAD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", "O:LAD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 10000 };
           yield return new object[] { true, false, "O:LAG:SYD:S:AI(AU;IDFA;FR;;;BA)", "O:LAG:SYD:S:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "O:LAG:SYS:AI(AU;IDFA;FR;;;BA)", "O:LAG:SYS:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "O:LAG:SYD:AI(A;ID;FR;;;BA)S:", "O:LAG:SYD:AI(A;ID;FR;;;BA)S:", 0 };
           yield return new object[] { true, false, "O:LAG:SYD:AI(A;ID;FR;;;BA)", "O:LAG:SYD:AI(A;ID;FR;;;BA)", 0 };
           yield return new object[] { true, false, "D:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", "D:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "G:SYS:AI(AU;IDFA;FR;;;BA)", "G:SYS:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "G:SYD:AI(A;ID;FR;;;BA)", "G:SYD:AI(A;ID;FR;;;BA)", 0 };
           yield return new object[] { true, false, "O:LAS:AI(AU;IDFA;FR;;;BA)", "O:LAS:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "O:LAD:AI(A;ID;FR;;;BA)", "O:LAD:AI(A;ID;FR;;;BA)", 0 };
           yield return new object[] { true, false, "O:LAG:SY", "O:LAG:SY", 0 };
           yield return new object[] { true, false, "O:LAG:SYD:AI(A;ID;FR;;;BA)", "O:LAG:SYD:AI(A;ID;FR;;;BA)", 0 };
           yield return new object[] { true, false, "O:LAG:SYS:AI(AU;IDFA;FR;;;BA)", "O:LAG:SYS:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "O:LAD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", "O:LAD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", "G:SYD:AI(A;ID;FR;;;BA)S:AI(AU;IDFA;FR;;;BA)", 0 };
           yield return new object[] { true, false, "O:LAG:SYD:(A;;KA;;;SY)(A;;KA;;;S-1-5-19-1)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(OA;;CCDC;bf967a9c-0de6-11d0-a285-00aa003049e2;;BU)(OA;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BG)(OA;;CCDC;bf967aa8-0de6-11d0-a285-00aa003049e2;;PU)(A;;LCRPRC;;;BO)S:(AU;SAFA;CCDCSWWPSDWDWO;;;WD)", "O:LAG:SYD:(A;;KA;;;SY)(A;;KA;;;S-1-5-19-1)(A;;LCRPRC;;;BO)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(OA;;CCDC;bf967a9c-0de6-11d0-a285-00aa003049e2;;BU)(OA;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BG)(OA;;CCDC;bf967aa8-0de6-11d0-a285-00aa003049e2;;PU)S:(AU;SAFA;CCDCSWWPSDWDWO;;;WD)", 0 };
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            // test case will be passing in binaryForm array that is null, length is invalid and offset is invalid
            CommonSecurityDescriptor commonSecurityDescriptor = null;
            string sddl = null;
            byte[] binaryForm = null;


            //Case 1, null byte array

            Assert.Throws<ArgumentNullException>(() =>
            {
                sddl = "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BG)(A;ID;FA;;;SY)";
                commonSecurityDescriptor = new CommonSecurityDescriptor(true, false, sddl);
                binaryForm = null;
                commonSecurityDescriptor.GetBinaryForm(binaryForm, 0);
            });


            //case 2, empty byte array

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                sddl = "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BG)(A;ID;FA;;;SY)";
                commonSecurityDescriptor = new CommonSecurityDescriptor(true, false, sddl);
                binaryForm = new byte[0];
                commonSecurityDescriptor.GetBinaryForm(binaryForm, 0);
            });

            //case 3, negative offset                 

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                sddl = "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BG)(A;ID;FA;;;SY)";
                commonSecurityDescriptor = new CommonSecurityDescriptor(true, false, sddl);
                binaryForm = new byte[100];
                commonSecurityDescriptor.GetBinaryForm(binaryForm, -1);
            });
            //case 4, binaryForm.Length - offset < BinaryLength

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                sddl = "O:LAG:SYD:AI(A;ID;FA;;;BA)(A;ID;FA;;;BG)(A;ID;FA;;;SY)";
                commonSecurityDescriptor = new CommonSecurityDescriptor(true, false, sddl);
                binaryForm = new byte[commonSecurityDescriptor.BinaryLength];
                commonSecurityDescriptor.GetBinaryForm(binaryForm, 8);
            });
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_GetBinaryForm_TestData))]
        [ActiveIssue(16919)]
        public static void TestGetBinaryForm(bool isContainer, bool isDS, string sddl, string verifierSddl, int offset)
        {
            CommonSecurityDescriptor commonSecurityDescriptor = null;
            CommonSecurityDescriptor verifierCommonSecurityDescriptor = null;
            string resultSddl = null;
            commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);
            byte[] binaryForm = new byte[commonSecurityDescriptor.BinaryLength + offset];
            commonSecurityDescriptor.GetBinaryForm(binaryForm, offset);
            verifierCommonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, binaryForm, offset);
            resultSddl = verifierCommonSecurityDescriptor.GetSddlForm(AccessControlSections.All);

            if (resultSddl == null || verifierSddl == null)
                Assert.True(resultSddl == null && verifierSddl == null);
            else
                Assert.True(string.Compare(resultSddl, verifierSddl, StringComparison.CurrentCultureIgnoreCase) == 0);
        }
    }
}
