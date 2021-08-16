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
    }
}
