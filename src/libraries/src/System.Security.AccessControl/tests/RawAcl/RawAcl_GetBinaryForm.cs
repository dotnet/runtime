// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_GetBinaryForm
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            RawAcl rAcl = null;
            GenericAce gAce = null;
            byte[] binaryForm = null;

            //Case 1, a RawAcl with one AccessAllowed Ace
            SecurityIdentifier sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA"));
            byte[] verifierBinaryForm = null;
            byte[] sidBinaryForm = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBinaryForm, 0);
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, sid, false, null);
            rAcl.InsertAce(0, gAce);
            binaryForm = new byte[rAcl.BinaryLength];
            verifierBinaryForm = new byte[rAcl.BinaryLength];
            rAcl.GetBinaryForm(binaryForm, 0);

            int errorCode;
            if (0 == Utils.Win32AclLayer.InitializeAclNative
                (verifierBinaryForm, (uint)rAcl.BinaryLength, (uint)GenericAcl.AclRevision))
            {
                errorCode = Marshal.GetLastWin32Error();
            }
            else if (0 == Utils.Win32AclLayer.AddAccessAllowedAceExNative
                (verifierBinaryForm, (uint)GenericAcl.AclRevision, (uint)AceFlags.None, (uint)1, sidBinaryForm))
            {
                errorCode = Marshal.GetLastWin32Error();
            }
            else
                Assert.True(Utils.IsBinaryFormEqual(binaryForm, verifierBinaryForm));

            //Case 2, a RawAcl with one AccessDenied Ace
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA"));
            verifierBinaryForm = null;
            sidBinaryForm = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBinaryForm, 0);
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            //31 include ObjectInherit, ContainerInherit, NoPropagateInherit, InheritOnly and Inherited
            gAce = new CommonAce((AceFlags)31, AceQualifier.AccessDenied, 1, sid, false, null);
            rAcl.InsertAce(0, gAce);
            binaryForm = new byte[rAcl.BinaryLength];
            verifierBinaryForm = new byte[rAcl.BinaryLength];
            rAcl.GetBinaryForm(binaryForm, 0);

            if (0 == Utils.Win32AclLayer.InitializeAclNative
                (verifierBinaryForm, (uint)rAcl.BinaryLength, (uint)GenericAcl.AclRevision))
            {
                errorCode = Marshal.GetLastWin32Error();
            }
            else if (0 == Utils.Win32AclLayer.AddAccessDeniedAceExNative
                (verifierBinaryForm, (uint)GenericAcl.AclRevision, (uint)31, (uint)1, sidBinaryForm))
            {
                errorCode = Marshal.GetLastWin32Error();
            }
            else
                Assert.True(Utils.IsBinaryFormEqual(binaryForm, verifierBinaryForm));

            //Case 3, a RawAcl with one Audit Ace
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA"));
            verifierBinaryForm = null;
            sidBinaryForm = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBinaryForm, 0);
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            //223 include ObjectInherit, ContainerInherit, NoPropagateInherit, InheritOnly, Inherited, SuccessfulAccess and FailedAccess
            gAce = new CommonAce((AceFlags)223, AceQualifier.SystemAudit, 1, sid, false, null);
            rAcl.InsertAce(0, gAce);
            binaryForm = new byte[rAcl.BinaryLength];
            verifierBinaryForm = new byte[rAcl.BinaryLength];
            rAcl.GetBinaryForm(binaryForm, 0);

            if (0 == Utils.Win32AclLayer.InitializeAclNative
                (verifierBinaryForm, (uint)rAcl.BinaryLength, (uint)GenericAcl.AclRevision))
            {
                errorCode = Marshal.GetLastWin32Error();
            }
            else if (0 == Utils.Win32AclLayer.AddAuditAccessAceExNative
                (verifierBinaryForm, (uint)GenericAcl.AclRevision, (uint)223, (uint)1, sidBinaryForm, 1, 1))
            {
                errorCode = Marshal.GetLastWin32Error();
            }
            else
                Assert.True(Utils.IsBinaryFormEqual(binaryForm, verifierBinaryForm));
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rAcl = null;
            GenericAce gAce = null;
            byte[] binaryForm = null;

            //Case 1, array binaryForm is null

            Assert.Throws<ArgumentNullException>(() =>
            {
                rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                rAcl.GetBinaryForm(binaryForm, 0);
            });
            //Case 2, offset is negative

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[100];
                rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                rAcl.GetBinaryForm(binaryForm, -1);
            });
            //Case 3, offset is equal to binaryForm length

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[100];
                rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                rAcl.GetBinaryForm(binaryForm, binaryForm.Length);
            });
            //Case , offset is a big possitive number

            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            binaryForm = new byte[rAcl.BinaryLength + 10000];
            rAcl.GetBinaryForm(binaryForm, 10000);
            //recreate the RawAcl from BinaryForm
            rAcl = new RawAcl(binaryForm, 10000);
            byte[] verifierBinaryForm = new byte[rAcl.BinaryLength];
            rAcl.GetBinaryForm(verifierBinaryForm, 0);
            Assert.True(Utils.IsBinaryFormEqual(binaryForm, 10000, verifierBinaryForm));

            //Case 4, binaryForm array's size is insufficient

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[4];
                rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                rAcl.GetBinaryForm(binaryForm, 0);
            });

            //Case 5, an empty RawAcl

            binaryForm = new byte[16];
            verifierBinaryForm = new byte[16];
            for (int i = 0; i < binaryForm.Length; i++)
            {
                binaryForm[i] = (byte)0;
                verifierBinaryForm[i] = (byte)0;
            }
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            rAcl.GetBinaryForm(binaryForm, 0);
            int errorCode;
            if (0 == Utils.Win32AclLayer.InitializeAclNative
                (verifierBinaryForm, (uint)8, (uint)GenericAcl.AclRevision))
            {
                errorCode = Marshal.GetLastWin32Error();
            }
            else
                Assert.True(Utils.IsBinaryFormEqual(binaryForm, verifierBinaryForm));
            //Case 6, a RawAcl with huge number of AccessAllowed, AccessDenied, and AuditAce
            SecurityIdentifier sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA"));

            verifierBinaryForm = null;
            byte[] sidBinaryForm = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBinaryForm, 0);
            rAcl = new RawAcl(GenericAcl.AclRevision, GenericAcl.MaxBinaryLength + 1);
            for (int i = 0; i < 780; i++)
            {
                //three aces binary length together is 24 + 36 + 24 = 84, 780 * 84 = 65520
                gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, i, sid, false, null);
                rAcl.InsertAce(rAcl.Count, gAce);
                gAce = new CommonAce((AceFlags)223, AceQualifier.SystemAudit, i + 1, sid, false, null);
                rAcl.InsertAce(rAcl.Count, gAce);
                gAce = new CommonAce((AceFlags)31, AceQualifier.AccessDenied, i + 2, sid, false, null);
                rAcl.InsertAce(rAcl.Count, gAce);

            }
            binaryForm = new byte[rAcl.BinaryLength];
            verifierBinaryForm = new byte[rAcl.BinaryLength];
            rAcl.GetBinaryForm(binaryForm, 0);

            if (0 == Utils.Win32AclLayer.InitializeAclNative
                (verifierBinaryForm, (uint)rAcl.BinaryLength, (uint)GenericAcl.AclRevision))
            {
                errorCode = Marshal.GetLastWin32Error();
            }
            else
            {
                int i = 0;
                for (i = 0; i < 780; i++)
                {
                    if (0 == Utils.Win32AclLayer.AddAccessAllowedAceExNative
                    (verifierBinaryForm, (uint)GenericAcl.AclRevision, (uint)AceFlags.None, (uint)i, sidBinaryForm))
                    {
                        errorCode = Marshal.GetLastWin32Error();
                        break;
                    }
                    if (0 == Utils.Win32AclLayer.AddAuditAccessAceExNative
                    (verifierBinaryForm, (uint)GenericAcl.AclRevision, (uint)223, (uint)(i + 1), sidBinaryForm, 1, 1))
                    {
                        errorCode = Marshal.GetLastWin32Error();
                        break;
                    }
                    if (0 == Utils.Win32AclLayer.AddAccessDeniedAceExNative
                    (verifierBinaryForm, (uint)GenericAcl.AclRevision, (uint)31, (uint)(i + 2), sidBinaryForm))
                    {
                        errorCode = Marshal.GetLastWin32Error();
                        break;
                    }
                }
                if (i == 780)
                {
                    //the loop finishes
                    Assert.True(Utils.IsBinaryFormEqual(binaryForm, verifierBinaryForm));
                }
                else
                {
                    Utils.PrintBinaryForm(binaryForm);
                    Utils.PrintBinaryForm(verifierBinaryForm);
                }
            }
        }
    }
}
