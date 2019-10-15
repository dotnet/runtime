// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_Index
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce genericAce = null;
            GenericAce verifierGenericAce = null;
            string owner1 = "SY";
            string owner2 = "BA";
            string owner3 = "BG";
            string owner4 = "BO";
            int index = 0;
            int previousCount = 0;
            int previousLength = 0;
            // case 1, only one ACE, get at index 0
            rawAcl = new RawAcl(1, 1);
            index = 0;
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, genericAce);

            verifierGenericAce = rawAcl[index];
            Assert.True(genericAce == verifierGenericAce);

            //case 2, two ACEs, get at index Count -1
            rawAcl = new RawAcl(1, 2);
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, genericAce);
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
            rawAcl.InsertAce(1, genericAce);

            index = rawAcl.Count - 1;
            verifierGenericAce = rawAcl[index];
            Assert.True(genericAce == verifierGenericAce);

            //case 3, only three ACEs, index at Count/2
            rawAcl = new RawAcl(1, 3);
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, genericAce);
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
            rawAcl.InsertAce(1, genericAce);
            rawAcl.InsertAce(2, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null));
            index = rawAcl.Count / 2;
            verifierGenericAce = rawAcl[index];
            Assert.True(genericAce == verifierGenericAce);

            // case 4, only one ACE, set at index 0
            rawAcl = new RawAcl(1, 1);
            index = 0;
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, genericAce);
            previousCount = rawAcl.Count;
            previousLength = rawAcl.BinaryLength - genericAce.BinaryLength;

            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
            rawAcl[index] = genericAce;
            verifierGenericAce = rawAcl[index];
            Assert.True((genericAce == verifierGenericAce) && (previousCount == rawAcl.Count) && (previousLength + genericAce.BinaryLength == rawAcl.BinaryLength));

            //case 5, two ACEs, set at index Count -1
            rawAcl = new RawAcl(1, 2);
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, genericAce);
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
            rawAcl.InsertAce(1, genericAce);
            previousCount = rawAcl.Count;
            previousLength = rawAcl.BinaryLength - genericAce.BinaryLength;

            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null);
            index = rawAcl.Count - 1;
            rawAcl[index] = genericAce;
            verifierGenericAce = rawAcl[index];
            Assert.True(((genericAce == verifierGenericAce) && (previousCount == rawAcl.Count) && (previousLength + genericAce.BinaryLength == rawAcl.BinaryLength)));

            //case 6, only three ACEs, index at Count/2
            rawAcl = new RawAcl(1, 3);
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, genericAce);
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
            rawAcl.InsertAce(1, genericAce);
            rawAcl.InsertAce(2, new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null));
            previousCount = rawAcl.Count;
            previousLength = rawAcl.BinaryLength - genericAce.BinaryLength;

            index = rawAcl.Count / 2;
            genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner4)), false, null);
            rawAcl[index] = genericAce;
            verifierGenericAce = rawAcl[index];
            Assert.True((genericAce == verifierGenericAce) && (previousCount == rawAcl.Count) && (previousLength + genericAce.BinaryLength == rawAcl.BinaryLength));
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce genericAce = null;
            GenericAce verifierGenericAce = null;
            string owner = null;
            int index = 0;


            // case 1, no ACE, get index at -1

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = -1;
                verifierGenericAce = rawAcl[index];
            });

            //case 2, get index at Count

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = rawAcl.Count;
                verifierGenericAce = rawAcl[index];
            });

            //case 3, set index at -1

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = -1;
                owner = "BA";
                genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl[index] = genericAce;
            });
            //case 4, set index at Count

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = rawAcl.Count;
                owner = "BA";
                genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl[index] = genericAce;
            });
            //case 5, set null Ace

            Assert.Throws<ArgumentNullException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = 0;
                genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(0, genericAce);
                genericAce = null;
                rawAcl[index] = genericAce;
            });
            //case 6, set Ace causing binarylength overflow

            Assert.Throws<OverflowException>(() =>
            {
                byte[] opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4];
                rawAcl = new RawAcl(1, 1);
                index = 0;
                genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(0, genericAce);
                genericAce = new CustomAce(AceType.MaxDefinedAceType + 1, (AceFlags)223, opaque);
                rawAcl[index] = genericAce;
            });
        }
    }
}
