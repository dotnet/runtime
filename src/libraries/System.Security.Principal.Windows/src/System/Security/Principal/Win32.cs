// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Principal
{
    internal static class Win32
    {
        internal const int FALSE = 0;

        //
        // Wrapper around advapi32.LsaOpenPolicy
        //


        internal static SafeLsaPolicyHandle LsaOpenPolicy(
            string? systemName,
            Interop.Advapi32.PolicyRights rights)
        {

            Interop.OBJECT_ATTRIBUTES attributes = default;
            uint error = Interop.Advapi32.LsaOpenPolicy(systemName, ref attributes, (int)rights, out SafeLsaPolicyHandle policyHandle);
            if (error == 0)
            {
                return policyHandle;
            }

            policyHandle.Dispose();

            if (error == Interop.StatusOptions.STATUS_ACCESS_DENIED)
            {
                throw new UnauthorizedAccessException();
            }
            else if (error == Interop.StatusOptions.STATUS_INSUFFICIENT_RESOURCES ||
                      error == Interop.StatusOptions.STATUS_NO_MEMORY)
            {
                throw new OutOfMemoryException();
            }
            else
            {
                uint win32ErrorCode = Interop.Advapi32.LsaNtStatusToWinError(error);

                throw new Win32Exception(unchecked((int)win32ErrorCode));
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


        internal static unsafe int CreateSidFromString(
            string stringSid,
            out byte[]? resultSid
            )
        {
            int ErrorCode;
            void* pSid = null;

            try
            {
                if (Interop.BOOL.FALSE == Interop.Advapi32.ConvertStringSidToSid(stringSid, out pSid))
                {
                    ErrorCode = Marshal.GetLastPInvokeError();
                    goto Error;
                }

                resultSid = ConvertIntPtrSidToByteArraySid((IntPtr)pSid);
            }
            finally
            {
                //
                // Now is a good time to get rid of the returned pointer
                //

                Marshal.FreeHGlobal((IntPtr)pSid);
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
            SecurityIdentifier? domainSid,
            out byte[]? resultSid
            )
        {
            //
            // Passing an array as big as it can ever be is a small price to pay for
            // not having to P/Invoke twice (once to get the buffer, once to get the data)
            //

            uint length = (uint)SecurityIdentifier.MaxBinaryLength;
            resultSid = new byte[length];

            if (FALSE != Interop.Advapi32.CreateWellKnownSid((int)sidType, domainSid?.BinaryForm, resultSid, ref length))
            {
                return Interop.Errors.ERROR_SUCCESS;
            }
            else
            {
                resultSid = null;

                return Marshal.GetLastPInvokeError();
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
                byte[] BinaryForm1 = new byte[sid1.BinaryLength];
                sid1.GetBinaryForm(BinaryForm1, 0);

                byte[] BinaryForm2 = new byte[sid2.BinaryLength];
                sid2.GetBinaryForm(BinaryForm2, 0);

                return (Interop.Advapi32.IsEqualDomainSid(BinaryForm1, BinaryForm2, out bool result) == FALSE ? false : result);
            }
        }

        //
        // Wrapper around avdapi32.GetWindowsAccountDomainSid
        //
        internal static int GetWindowsAccountDomainSid(
            SecurityIdentifier sid,
            out SecurityIdentifier? resultSid
            )
        {
            //
            // Passing an array as big as it can ever be is a small price to pay for
            // not having to P/Invoke twice (once to get the buffer, once to get the data)
            //

            byte[] BinaryForm = new byte[sid.BinaryLength];
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

                return Marshal.GetLastPInvokeError();
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
