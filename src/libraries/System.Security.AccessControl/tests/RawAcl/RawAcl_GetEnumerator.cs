// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_GetEnumerator
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            GenericAce gAce = null;
            RawAcl rAcl = null;
            AceEnumerator aceEnumerator = null;

            // Case 1, when collection is actually empty
            rAcl = new RawAcl(1, 1);
            aceEnumerator = rAcl.GetEnumerator();

            //Case 2, collection has one ACE
            rAcl = new RawAcl(0, 1);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            aceEnumerator = rAcl.GetEnumerator();
            Assert.True(Utils.TestGetEnumerator(aceEnumerator, rAcl, false));
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            GenericAce gAce = null;
            RawAcl rAcl = null;
            AceEnumerator aceEnumerator = null;

            //Case 1, RawAcl with huge number of Aces
            rAcl = new RawAcl(0, GenericAcl.MaxBinaryLength + 1);
            for (int i = 0; i < 1820; i++)
            {
                //this ace binary length is 36, 1820 * 36 = 65520                        
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
            }
            aceEnumerator = rAcl.GetEnumerator();
            Assert.True(Utils.TestGetEnumerator(aceEnumerator, rAcl, false));
        }
    }
}
