// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NetSecurityNative
    {
        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ReleaseGssBuffer")]
        internal static extern void ReleaseGssBuffer(
            IntPtr bufferPtr,
            ulong length);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_DisplayMinorStatus")]
        internal static extern Status DisplayMinorStatus(
            out Status minorStatus,
            Status statusValue,
            ref GssBuffer buffer);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_DisplayMajorStatus")]
        internal static extern Status DisplayMajorStatus(
            out Status minorStatus,
            Status statusValue,
            ref GssBuffer buffer);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ImportUserName")]
        internal static extern Status ImportUserName(
            out Status minorStatus,
            string inputName,
            int inputNameByteCount,
            out SafeGssNameHandle outputName);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ImportPrincipalName")]
        internal static extern Status ImportPrincipalName(
            out Status minorStatus,
            string inputName,
            int inputNameByteCount,
            out SafeGssNameHandle outputName);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ReleaseName")]
        internal static extern Status ReleaseName(
            out Status minorStatus,
            ref IntPtr inputName);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_AcquireAcceptorCred")]
        internal static extern Status AcquireAcceptorCred(
            out Status minorStatus,
            out SafeGssCredHandle outputCredHandle);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_InitiateCredSpNego")]
        internal static extern Status InitiateCredSpNego(
            out Status minorStatus,
            SafeGssNameHandle desiredName,
            out SafeGssCredHandle outputCredHandle);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_InitiateCredWithPassword")]
        internal static extern Status InitiateCredWithPassword(
            out Status minorStatus,
            bool isNtlm,
            SafeGssNameHandle desiredName,
            string password,
            int passwordLen,
            out SafeGssCredHandle outputCredHandle);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ReleaseCred")]
        internal static extern Status ReleaseCred(
            out Status minorStatus,
            ref IntPtr credHandle);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_InitSecContext")]
        internal static extern Status InitSecContext(
            out Status minorStatus,
            SafeGssCredHandle initiatorCredHandle,
            ref SafeGssContextHandle contextHandle,
            bool isNtlmOnly,
            SafeGssNameHandle? targetName,
            uint reqFlags,
            byte[]? inputBytes,
            int inputLength,
            ref GssBuffer token,
            out uint retFlags,
            out bool isNtlmUsed);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_InitSecContextEx")]
        internal static extern Status InitSecContext(
            out Status minorStatus,
            SafeGssCredHandle initiatorCredHandle,
            ref SafeGssContextHandle contextHandle,
            bool isNtlmOnly,
            IntPtr cbt,
            int cbtSize,
            SafeGssNameHandle? targetName,
            uint reqFlags,
            byte[]? inputBytes,
            int inputLength,
            ref GssBuffer token,
            out uint retFlags,
            out bool isNtlmUsed);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_AcceptSecContext")]
        internal static extern Status AcceptSecContext(
            out Status minorStatus,
            SafeGssCredHandle acceptorCredHandle,
            ref SafeGssContextHandle acceptContextHandle,
            byte[]? inputBytes,
            int inputLength,
            ref GssBuffer token,
            out uint retFlags,
            out bool isNtlmUsed);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_DeleteSecContext")]
        internal static extern Status DeleteSecContext(
            out Status minorStatus,
            ref IntPtr contextHandle);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_GetUser")]
        internal static extern Status GetUser(
            out Status minorStatus,
            SafeGssContextHandle? acceptContextHandle,
            ref GssBuffer token);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_Wrap")]
        private static extern unsafe Status Wrap(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            bool isEncrypt,
            byte* inputBytes,
            int count,
            ref GssBuffer outBuffer);

        [DllImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_Unwrap")]
        private static extern Status Unwrap(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            byte[] inputBytes,
            int offset,
            int count,
            ref GssBuffer outBuffer);

        internal static unsafe Status WrapBuffer(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            bool isEncrypt,
            ReadOnlySpan<byte> inputBytes,
            ref GssBuffer outBuffer)
        {
            fixed (byte* inputBytesPtr = inputBytes)
            {
                return Wrap(out minorStatus, contextHandle, isEncrypt, inputBytesPtr, inputBytes.Length, ref outBuffer);
            }
        }

        internal static Status UnwrapBuffer(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            byte[] inputBytes,
            int offset,
            int count,
            ref GssBuffer outBuffer)
        {
            Debug.Assert(inputBytes != null, "inputBytes must be valid value");
            Debug.Assert(offset >= 0 && offset <= inputBytes.Length, "offset must be valid");
            Debug.Assert(count >= 0 && count <= inputBytes.Length, "count must be valid");

            return Unwrap(out minorStatus, contextHandle, inputBytes, offset, count, ref outBuffer);
        }

        // https://www.gnu.org/software/gss/reference/gss.pdf Page 65
        internal const int GSS_C_ROUTINE_ERROR_OFFSET = 16;

        // https://www.gnu.org/software/gss/reference/gss.pdf Page 9
        internal enum Status : uint
        {
            GSS_S_COMPLETE = 0,
            GSS_S_CONTINUE_NEEDED = 1,
            GSS_S_BAD_MECH = 1 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_NAME = 2 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_NAMETYPE = 3 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_BINDINGS = 4 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_STATUS = 5 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_SIG = 6 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_NO_CRED = 7 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_NO_CONTEXT = 8 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_DEFECTIVE_TOKEN = 9 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_DEFECTIVE_CREDENTIAL = 10 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_CREDENTIALS_EXPIRED = 11 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_CONTEXT_EXPIRED = 12 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_FAILURE = 13 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_QOP = 14 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_UNAUTHORIZED = 15 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_UNAVAILABLE = 16 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_DUPLICATE_ELEMENT = 17 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_NAME_NOT_MN = 18 << GSS_C_ROUTINE_ERROR_OFFSET,
        }

        [Flags]
        internal enum GssFlags : uint
        {
            GSS_C_DELEG_FLAG = 0x1,
            GSS_C_MUTUAL_FLAG = 0x2,
            GSS_C_REPLAY_FLAG = 0x4,
            GSS_C_SEQUENCE_FLAG = 0x8,
            GSS_C_CONF_FLAG = 0x10,
            GSS_C_INTEG_FLAG = 0x20,
            GSS_C_ANON_FLAG = 0x40,
            GSS_C_PROT_READY_FLAG = 0x80,
            GSS_C_TRANS_FLAG = 0x100,
            GSS_C_DCE_STYLE = 0x1000,
            GSS_C_IDENTIFY_FLAG = 0x2000,
            GSS_C_EXTENDED_ERROR_FLAG = 0x4000,
            GSS_C_DELEG_POLICY_FLAG = 0x8000
        }
    }
}
