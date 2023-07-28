// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_IsSynchronized
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            GenericAce gAce = null;
            RawAcl rAcl = null;

            // collection has one ACE. By code review, this properties always return false. So no additional cases are needed
            rAcl = new RawAcl(0, 1);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            Assert.True(false == rAcl.IsSynchronized);
        }
    }
}
