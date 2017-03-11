// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    [Flags]
    public enum FlagsForAce : byte
    {
        None = 0x00,
        OI = 0x01,
        CI = 0x02,
        NP = 0x04,
        IO = 0x08,
        IH = 0x10,
        SA = 0x40,
        FA = 0x80,

        InheritanceFlags = OI | CI | NP | IO,
        AuditFlags = SA | FA,
    }

    public class Utils
    {
        public static bool IsAceEqual(GenericAce ace1, GenericAce ace2)
        {
            bool result = true;
            byte[] ace1BinaryForm;
            byte[] ace2BinaryForm;


            if (null != ace1 && null != ace2)
            {
                //check the BinaryLength
                if (ace1.BinaryLength != ace2.BinaryLength)
                {
                    result = false;
                }
                else
                {
                    ace1BinaryForm = new byte[ace1.BinaryLength];
                    ace2BinaryForm = new byte[ace2.BinaryLength];
                    ace1.GetBinaryForm(ace1BinaryForm, 0);
                    ace2.GetBinaryForm(ace2BinaryForm, 0);
                    if (!IsBinaryFormEqual(ace1BinaryForm, ace2BinaryForm))
                    {
                        result = false;
                    }
                }
            }
            else if (null == ace1 && null == ace2)
            {
                Console.WriteLine("Both aces are null");
            }
            else
                result = false;
            return result;
        }

        public static bool IsBinaryFormEqual(byte[] binaryForm1, int offset, byte[] binaryForm2)
        {
            bool result = true;
            if (null == binaryForm1 && null == binaryForm2)
                result = true;
            else if (null != binaryForm1 && null != binaryForm2)
            {
                if (binaryForm1.Length - offset != binaryForm2.Length)
                {
                    result = false;
                }
                else
                {
                    for (int i = 0; i < binaryForm2.Length; i++)
                    {
                        if (binaryForm1[offset + i] != binaryForm2[i])
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }
            else
                result = false;
            return result;
        }

        public static bool IsBinaryFormEqual(byte[] binaryForm1, byte[] binaryForm2)
        {
            return IsBinaryFormEqual(binaryForm1, 0, binaryForm2);
        }

        public static RawAcl CreateRawAclFromString(string rawAclString)
        {
            RawAcl rawAcl = null;
            byte revision = 0;
            int capacity = 1;
            CommonAce cAce = null;
            AceFlags aceFlags = AceFlags.None;
            AceQualifier aceQualifier = AceQualifier.AccessAllowed;
            int accessMask = 1;
            SecurityIdentifier sid = null;
            bool isCallback = false;
            int opaqueSize = 0;
            byte[] opaque = null;


            string[] parts = null;
            string[] subparts = null;
            char[] delimiter1 = new char[] { '#' };
            char[] delimiter2 = new char[] { ':' };

            if (rawAclString != null)
            {
                rawAcl = new RawAcl(revision, capacity);

                parts = rawAclString.Split(delimiter1);
                for (int i = 0; i < parts.Length; i++)
                {
                    subparts = parts[i].Split(delimiter2);
                    if (subparts.Length != 6)
                    {
                        return null;
                    }

                    aceFlags = (AceFlags)byte.Parse(subparts[0]);
                    aceQualifier = (AceQualifier)int.Parse(subparts[1]);
                    accessMask = int.Parse(subparts[2]);
                    sid = new SecurityIdentifier(TranslateStringConstFormatSidToStandardFormatSid(subparts[3]));
                    isCallback = bool.Parse(subparts[4]);
                    if (!isCallback)
                        opaque = null;
                    else
                    {
                        opaqueSize = int.Parse(subparts[5]);
                        opaque = new byte[opaqueSize];
                    }
                    cAce = new CommonAce(aceFlags, aceQualifier, accessMask, sid, isCallback, opaque);
                    rawAcl.InsertAce(rawAcl.Count, cAce);
                }
            }
            return rawAcl;
        }

        public static string TranslateStringConstFormatSidToStandardFormatSid(string sidStringConst)
        {
            string stFormatSid = null;
            if (sidStringConst == "BA")
                stFormatSid = "S-1-5-32-544";
            else if (sidStringConst == "BO")
                stFormatSid = "S-1-5-32-551";
            else if (sidStringConst == "BG")
                stFormatSid = "S-1-5-32-546";

            else if (sidStringConst == "AN")
                stFormatSid = "S-1-5-7";

            else if (sidStringConst == "NO")
                stFormatSid = "S-1-5-32-556";

            else if (sidStringConst == "SO")
                stFormatSid = "S-1-5-32-549";

            else if (sidStringConst == "RD")
                stFormatSid = "S-1-5-32-555";

            else if (sidStringConst == "SY")
                stFormatSid = "S-1-5-18";

            else
                stFormatSid = sidStringConst;
            return stFormatSid;
        }

        public static void PrintBinaryForm(byte[] binaryForm)
        {
            Console.WriteLine();

            if (binaryForm != null)
            {
                Console.WriteLine("BinaryForm:");
                for (int i = 0; i < binaryForm.Length; i++)
                {
                    Console.WriteLine("{0}", binaryForm[i]);
                }
                Console.WriteLine();
            }
            else
                Console.WriteLine("BinaryForm: null");
        }

        public static int ComputeBinaryLength(CommonSecurityDescriptor commonSecurityDescriptor, bool needCountDacl)
        {
            int verifierBinaryLength = 0;
            if (commonSecurityDescriptor != null)
            {
                verifierBinaryLength = 20; //initialize the binary length to header length
                if (commonSecurityDescriptor.Owner != null)
                    verifierBinaryLength += commonSecurityDescriptor.Owner.BinaryLength;
                if (commonSecurityDescriptor.Group != null)
                    verifierBinaryLength += commonSecurityDescriptor.Group.BinaryLength;
                if ((commonSecurityDescriptor.ControlFlags & ControlFlags.SystemAclPresent) != 0 && commonSecurityDescriptor.SystemAcl != null)
                    verifierBinaryLength += commonSecurityDescriptor.SystemAcl.BinaryLength;
                if ((commonSecurityDescriptor.ControlFlags & ControlFlags.DiscretionaryAclPresent) != 0 && commonSecurityDescriptor.DiscretionaryAcl != null && needCountDacl)
                    verifierBinaryLength += commonSecurityDescriptor.DiscretionaryAcl.BinaryLength;
            }

            return verifierBinaryLength;
        }
        //verify the dacl is crafted with one Allow Everyone Everything ACE

        public static bool VerifyDaclWithCraftedAce(bool isContainer, bool isDS, DiscretionaryAcl dacl)
        {
            byte[] craftedBForm;
            byte[] binaryForm;

            DiscretionaryAcl craftedDacl = new DiscretionaryAcl(isContainer, isDS, 1);
            craftedDacl.AddAccess(AccessControlType.Allow,
                new SecurityIdentifier("S-1-1-0"),
                -1,
                isContainer ? InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit : InheritanceFlags.None,
                PropagationFlags.None);
            craftedBForm = new byte[craftedDacl.BinaryLength];
            binaryForm = new byte[dacl.BinaryLength];
            Assert.False(craftedBForm == null || binaryForm == null);
            craftedDacl.GetBinaryForm(craftedBForm, 0);
            dacl.GetBinaryForm(binaryForm, 0);

            return Utils.IsBinaryFormEqual(craftedBForm, binaryForm);

        }

        public static RawAcl CopyRawACL(RawAcl rawAcl)
        {
            byte[] binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            return new RawAcl(binaryForm, 0);
        }

        public static bool AclPartialEqual(GenericAcl acl1, GenericAcl acl2, int acl1StartAceIndex, int acl1EndAceIndex, int acl2StartAceIndex, int acl2EndAceIndex)
        {
            int index1 = 0;
            int index2 = 0;
            bool result = true;
            if (null != acl1 && null != acl2)
            {
                if (acl1StartAceIndex < 0 || acl1EndAceIndex < 0 || acl1StartAceIndex > acl1.Count - 1 || acl1EndAceIndex > acl1.Count - 1 ||
                    acl2StartAceIndex < 0 || acl2EndAceIndex < 0 || acl2StartAceIndex > acl2.Count - 1 || acl2EndAceIndex > acl2.Count - 1)
                {
                    //the caller has garenteeed the index calculation is correct so if any above condition hold, 
                    //that means the range of the index is invalid
                    return true;
                }
                if (acl1EndAceIndex - acl1StartAceIndex != acl2EndAceIndex - acl2StartAceIndex)
                {
                    result = false;
                }
                else
                {
                    for (index1 = acl1StartAceIndex, index2 = acl2StartAceIndex; index1 <= acl1EndAceIndex; index1++, index2++)
                    {
                        if (!Utils.IsAceEqual(acl1[index1], acl2[index2]))
                        {
                            result = false;
                            break;
                        }
                    }
                }
            }
            else if (null == acl1 && null == acl2)
            {
            }
            else
                result = false;

            return result;
        }

        public static bool TestGetEnumerator(IEnumerator enumerator, RawAcl rAcl, bool isExplicit)
        {
            bool result = false;//assume failure
            GenericAce gAce = null;
            if (!(isExplicit ? enumerator.MoveNext() : ((AceEnumerator)enumerator).MoveNext()))
            {//enumerator is created from empty RawAcl
                if (0 != rAcl.Count)
                    return false;
                else
                    return true;
            }
            else if (0 == rAcl.Count)
            {//rawAcl is empty but enumerator is still enumerable
                return false;
            }
            else//non-empty rAcl, non-empty enumerator
            {
                //check all aces enumerated are in the RawAcl
                if (isExplicit)
                {
                    enumerator.Reset();
                }
                else
                {
                    ((AceEnumerator)enumerator).Reset();
                }
                while (isExplicit ? enumerator.MoveNext() : ((AceEnumerator)enumerator).MoveNext())
                {
                    gAce = (GenericAce)(isExplicit ? enumerator.Current : ((AceEnumerator)enumerator).Current);
                    //check this gAce exists in the RawAcl
                    for (int i = 0; i < rAcl.Count; i++)
                    {
                        if (GenericAce.ReferenceEquals(gAce, rAcl[i]))
                        {//found
                            result = true;
                            break;
                        }
                    }
                    if (!result)
                    {//not exists in the RawAcl, failed
                        return false;
                    }
                    //enumerate to next one
                }
                //check all aces of rAcl are enumerable by the enumerator
                result = false; //assume failure
                for (int i = 0; i < rAcl.Count; i++)
                {
                    gAce = rAcl[i];
                    //check this gAce is enumerable
                    if (isExplicit)
                    {
                        enumerator.Reset();
                    }
                    else
                    {
                        ((AceEnumerator)enumerator).Reset();
                    }

                    while (isExplicit ? enumerator.MoveNext() : ((AceEnumerator)enumerator).MoveNext())
                    {
                        if (GenericAce.ReferenceEquals((GenericAce)(isExplicit ? enumerator.Current : ((AceEnumerator)enumerator).Current), gAce))
                        {
                            result = true;
                            break;
                        }
                    }
                    if (!result)
                    {//not enumerable
                        return false;
                    }
                    //check next ace in the rAcl
                }
                //now all passed
                return true;
            }
        }

        public sealed class Win32AclLayer
        {
            internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;
            internal const int VER_PLATFORM_WIN32_NT = 2;
            [DllImport("Advapi32.dll", EntryPoint = "InitializeAcl", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int InitializeAclNative(
            byte[] acl,
            uint aclLength,
            uint aclRevision);
            [DllImport("Advapi32.dll", EntryPoint = "AddAccessAllowedAceEx", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int AddAccessAllowedAceExNative(
            byte[] acl,
            uint aclRevision,
            uint aceFlags,
            uint accessMask,
            byte[] sid);
            [DllImport("Advapi32.dll", EntryPoint = "AddAccessDeniedAceEx", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int AddAccessDeniedAceExNative(
            byte[] acl,
            uint aclRevision,
            uint aceFlags,
            uint accessMask,
            byte[] sid);
            [DllImport("Advapi32.dll", EntryPoint = "AddAuditAccessAceEx", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int AddAuditAccessAceExNative(
            byte[] acl,
            uint aclRevision,
            uint aceFlags,
            uint accessMask,
            byte[] sid,
            uint bAuditSccess,
            uint bAuditFailure);
        }
    }
}
