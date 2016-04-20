// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class CommonSecurityDescriptor_WasDiscretionaryAclCanonicalInitially
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_WasDiscretionaryAclCanonicalInitially_TestData()
       {
           yield return new object[] { true, false, "O:LAG:SYD:AI(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(D;CIID;WO;;;BA)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)(AU;CIIDSAFA;WO;;;BA)", false };
           yield return new object[] { true, false, "O:LAG:SYD:AI(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", false };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;OI;WO;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(A;OI;FR;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(D;CIID;WO;;;BA)S:AI(AU;OISA;FR;;;BA)(AU;OIFA;WO;;;BG)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)", true };
           yield return new object[] { true, false, "O:LAG:SYD:AI(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;CIIDSAFA;WO;;;BA)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", false };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)S:AI(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)", false };
           yield return new object[] { true, false, "O:LAG:SYD:AI(D;CIID;WO;;;BA)(OD;;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(D;OI;WO;;;BG)(OA;;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BA)(A;OI;FR;;;BG)S:AI(AU;OIFA;WO;;;BG)(AU;OISA;FR;;;BA)(AU;CIIDSAFA;WO;;;BA)(OU;FA;CCDC;6da8a4ff-0e52-11d0-a286-00aa003049e2;;BA)(OU;SA;CCDC;bf967aba-0de6-11d0-a285-00aa003049e2;;BG)", false };
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_WasDiscretionaryAclCanonicalInitially_TestData))]
        public static void TestWasDiscretionaryAclCanonicalInitially(bool isContainer, bool isDS, string sddl, bool verifierWasCanonicalInitially)
        {
            CommonSecurityDescriptor commonSecurityDescriptor = null;
            commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, sddl);
            Assert.True(verifierWasCanonicalInitially == commonSecurityDescriptor.IsDiscretionaryAclCanonical);
        }

    }
}