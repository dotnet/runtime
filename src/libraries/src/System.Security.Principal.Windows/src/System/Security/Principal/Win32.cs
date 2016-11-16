// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
    [Flags]
    internal enum PolicyRights
    {
        POLICY_VIEW_LOCAL_INFORMATION = 0x00000001,
        POLICY_VIEW_AUDIT_INFORMATION = 0x00000002,
        POLICY_GET_PRIVATE_INFORMATION = 0x00000004,
        POLICY_TRUST_ADMIN = 0x00000008,
        POLICY_CREATE_ACCOUNT = 0x00000010,
        POLICY_CREATE_SECRET = 0x00000020,
        POLICY_CREATE_PRIVILEGE = 0x00000040,
        POLICY_SET_DEFAULT_QUOTA_LIMITS = 0x00000080,
        POLICY_SET_AUDIT_REQUIREMENTS = 0x00000100,
        POLICY_AUDIT_LOG_ADMIN = 0x00000200,
        POLICY_SERVER_ADMIN = 0x00000400,
        POLICY_LOOKUP_NAMES = 0x00000800,
        POLICY_NOTIFICATION = 0x00001000,
    }

    internal static class Win32
    {
        internal const int FALSE = 0;

        //
        // Wrapper around advapi32.LsaOpenPolicy
        //


        internal static SafeLsaPolicyHandle LsaOpenPolicy(
            string systemName,
            PolicyRights rights)
        {
            uint ReturnCode;
            SafeLsaPolicyHandle Result;
            Interop.LSA_OBJECT_ATTRIBUTES Loa;

            Loa.Length = Marshal.SizeOf<Interop.LSA_OBJECT_ATTRIBUTES>();
            Loa.RootDirectory = IntPtr.Zero;
            Loa.ObjectName = IntPtr.Zero;
            Loa.Attributes = 0;
            Loa.SecurityDescriptor = IntPtr.Zero;
            Loa.SecurityQualityOfService = IntPtr.Zero;

            if (0 == (ReturnCode = Interop.Advapi32.LsaOpenPolicy(systemName, ref Loa, (int)rights, out Result)))
            {
                return Result;
            }
            else if (ReturnCode == Interop.StatusOptions.STATUS_ACCESS_DENIED)
            {
                throw new UnauthorizedAccessException();
            }
            else if (ReturnCode == Interop.StatusOptions.STATUS_INSUFFICIENT_RESOURCES ||
                      ReturnCode == Interop.StatusOptions.STATUS_NO_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else
            {
                int win32ErrorCode = Interop.NtDll.RtlNtStatusToDosError(unchecked((int)ReturnCode));

                throw new Win32Exception(win32ErrorCode);
            }
        }


        internal static byte[] ConvertIntPtrSidToByteArraySid(IntPtr binaryForm)
        {
            byte[] ResultSid;

            //
            // Verify the revision (just sanity, should never fail to be 1)
            //

            byte Revision = Marshal.ReadByte(binaryForm, 0);

            if (Revision != SecurityIdentifier.Revision)
            {
                throw new ArgumentException(SR.IdentityReference_InvalidSidRevision, nameof(binaryForm));
            }

            //
            // Need the subauthority count in order to figure out how many bytes to read
            //

            byte SubAuthorityCount = Marshal.ReadByte(binaryForm, 1);

            if (SubAuthorityCount < 0 ||
                SubAuthorityCount > SecurityIdentifier.MaxSubAuthorities)
            {
                throw new ArgumentException(SR.Format(SR.IdentityReference_InvalidNumberOfSubauthorities, SecurityIdentifier.MaxSubAuthorities), nameof(binaryForm));
            }

            //
            // Compute the size of the binary form of this SID and allocate the memory
            //

            int BinaryLength = 1 + 1 + 6 + SubAuthorityCount * 4;
            ResultSid = new byte[BinaryLength];

            //
            // Extract the data from the returned pointer
            //

            Marshal.Copy(binaryForm, ResultSid, 0, BinaryLength);

            return ResultSid;
        }

        //
        // Wrapper around advapi32.ConvertStringSidToSidW
        //


        internal static int CreateSidFromString(
            string stringSid,
            out byte[] resultSid
            )
        {
            int ErrorCode;
            IntPtr ByteArray = IntPtr.Zero;

            try
            {
                if (FALSE == Interop.Advapi32.ConvertStringSidToSid(stringSid, out ByteArray))
                {
                    ErrorCode = Marshal.GetLastWin32Error();
                    goto Error;
                }

                resultSid = ConvertIntPtrSidToByteArraySid(ByteArray);
            }
            finally
            {
                //
                // Now is a good time to get rid of the returned pointer
                //

                Interop.Kernel32.LocalFree(ByteArray);
            }

            //
            // Now invoke the SecurityIdentifier factory method to create the result
            //

            return Interop.Errors.ERROR_SUCCESS;

        Error:

            resultSid = null;
            return ErrorCode;
        }

        //
        // Wrapper around advapi32.CreateWellKnownSid
        //


        internal static int CreateWellKnownSid(
            WellKnownSidType sidType,
            SecurityIdentifier domainSid,
            out byte[] resultSid
            )
        {
            //
            // Passing an array as big as it can ever be is a small price to pay for
            // not having to P/Invoke twice (once to get the buffer, once to get the data)
            //

            uint length = (uint)SecurityIdentifier.MaxBinaryLength;
            resultSid = new byte[length];

            if (FALSE != Interop.Advapi32.CreateWellKnownSid((int)sidType, domainSid == null ? null : domainSid.BinaryForm, resultSid, ref length))
            {
                return Interop.Errors.ERROR_SUCCESS;
            }
            else
            {
                resultSid = null;

                return Marshal.GetLastWin32Error();
            }
        }

        //
        // Wrapper around advapi32.EqualDomainSid
        //


        internal static bool IsEqualDomainSid(SecurityIdentifier sid1, SecurityIdentifier sid2)
        {
            if (sid1 == null || sid2 == null)
            {
                return false;
            }
            else
            {
                bool result;

                byte[] BinaryForm1 = new Byte[sid1.BinaryLength];
                sid1.GetBinaryForm(BinaryForm1, 0);

                byte[] BinaryForm2 = new Byte[sid2.BinaryLength];
                sid2.GetBinaryForm(BinaryForm2, 0);

                return (Interop.Advapi32.IsEqualDomainSid(BinaryForm1, BinaryForm2, out result) == FALSE ? false : result);
            }
        }

        /// <summary>
        ///     Setup the size of the buffer Windows provides for an LSA_REFERENCED_DOMAIN_LIST
        /// </summary>

        internal static void InitializeReferencedDomainsPointer(SafeLsaMemoryHandle referencedDomains)
        {
            Debug.Assert(referencedDomains != null, "referencedDomains != null");

            // We don't know the real size of the referenced domains yet, so we need to set an initial
            // size based on the LSA_REFERENCED_DOMAIN_LIST structure, then resize it to include all of
            // the domains.
            referencedDomains.Initialize((uint)Marshal.SizeOf<Interop.LSA_REFERENCED_DOMAIN_LIST>());
            Interop.LSA_REFERENCED_DOMAIN_LIST domainList = referencedDomains.Read<Interop.LSA_REFERENCED_DOMAIN_LIST>(0);

            unsafe
            {
                byte* pRdl = null;
                try
                {
                    referencedDomains.AcquirePointer(ref pRdl);

                    // If there is a trust information list, then the buffer size is the end of that list minus
                    // the beginning of the domain list. Otherwise, then the buffer is just the size of the
                    // referenced domain list structure, which is what we defaulted to.
                    if (domainList.Domains != IntPtr.Zero)
                    {
                        Interop.LSA_TRUST_INFORMATION* pTrustInformation = (Interop.LSA_TRUST_INFORMATION*)domainList.Domains;
                        pTrustInformation = pTrustInformation + domainList.Entries;

                        long bufferSize = (byte*)pTrustInformation - pRdl;
                        Debug.Assert(bufferSize > 0, "bufferSize > 0");
                        referencedDomains.Initialize((ulong)bufferSize);
                    }
                }
                finally
                {
                    if (pRdl != null)
                        referencedDomains.ReleasePointer();
                }
            }
        }

        //
        // Wrapper around avdapi32.GetWindowsAccountDomainSid
        //
        internal static int GetWindowsAccountDomainSid(
            SecurityIdentifier sid,
            out SecurityIdentifier resultSid
            )
        {
            //
            // Passing an array as big as it can ever be is a small price to pay for
            // not having to P/Invoke twice (once to get the buffer, once to get the data)
            //

            byte[] BinaryForm = new Byte[sid.BinaryLength];
            sid.GetBinaryForm(BinaryForm, 0);
            uint sidLength = (uint)SecurityIdentifier.MaxBinaryLength;
            byte[] resultSidBinary = new byte[sidLength];

            if (FALSE != Interop.Advapi32.GetWindowsAccountDomainSid(BinaryForm, resultSidBinary, ref sidLength))
            {
                resultSid = new SecurityIdentifier(resultSidBinary, 0);

                return Interop.Errors.ERROR_SUCCESS;
            }
            else
            {
                resultSid = null;

                return Marshal.GetLastWin32Error();
            }
        }

        //
        // Wrapper around advapi32.IsWellKnownSid
        //


        internal static bool IsWellKnownSid(
            SecurityIdentifier sid,
            WellKnownSidType type
            )
        {
            byte[] BinaryForm = new byte[sid.BinaryLength];
            sid.GetBinaryForm(BinaryForm, 0);

            if (FALSE == Interop.Advapi32.IsWellKnownSid(BinaryForm, (int)type))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
