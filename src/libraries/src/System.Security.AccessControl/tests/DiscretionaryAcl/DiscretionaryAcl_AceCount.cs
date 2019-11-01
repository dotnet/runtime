// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class DiscretionaryAcl_AceCount
    {
        [Fact]
        public static void AceCount_BasicValidation()
        {
            RawAcl rawAcl = null;
            GenericAce gAce = null;
            DiscretionaryAcl discretionaryAcl = null;
            bool isContainer = false;
            bool isDS = false;
            byte revision = 0;
            int capacity = 0;
            string sid = "BG";

            //case 1, empty DiscretionaryAcl
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            Assert.True(0 == discretionaryAcl.Count);

            //case 2, DiscretionaryAcl with one Ace
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            Assert.True(1 == discretionaryAcl.Count);

            //case 3, DiscretionaryAcl with two Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //223 has all AceFlags
            gAce = new CommonAce((AceFlags)223, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 2,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            Assert.True(2 == discretionaryAcl.Count);

        }

        [Fact]
        public static void AceCount_AdditionalTests()
        {
            RawAcl rawAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BG";
            DiscretionaryAcl discretionaryAcl = null;
            bool isContainer = false;
            bool isDS = false;

            //case 1, DiscretionaryAcl with huge number of Aces
            revision = 0;
            capacity = 1;
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
            rawAcl = new RawAcl(revision, capacity);
            for (int i = 0; i < 1820; i++)
            {
                gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, i + 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + i.ToString())), false, null);
                rawAcl.InsertAce(0, gAce);
            }
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            Assert.True(1820 == discretionaryAcl.Count);

        }
    }
}