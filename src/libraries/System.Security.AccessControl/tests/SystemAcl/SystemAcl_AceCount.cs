// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class SystemAcl_AceCount
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce gAce = null;
            SystemAcl systemAcl = null;
            bool isContainer = false;
            bool isDS = false;
            byte revision = 0;
            int capacity = 0;
            string sid = "BA";

            //case 1, empty SystemAcl
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            isContainer = false;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(0 == systemAcl.Count);

            //case 2, SystemAcl with one Ace
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(1 == systemAcl.Count);

            //case 3, SystemAcl with two Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(2 == systemAcl.Count);
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BA";
            SystemAcl systemAcl = null;
            bool isContainer = false;
            bool isDS = false;

            //case 1, SysemAcl with huge number of Aces
            revision = 0;
            capacity = 1;
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
            rawAcl = new RawAcl(revision, capacity);
            for (int i = 0; i < 1820; i++)
            {
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + i.ToString())), false, null);
                rawAcl.InsertAce(0, gAce);
            }
            isContainer = true;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(1820 == systemAcl.Count);
        }
    }
}