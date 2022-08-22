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
        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ReleaseGssBuffer")]
        internal static partial void ReleaseGssBuffer(
            IntPtr bufferPtr,
            ulong length);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_DisplayMinorStatus")]
        internal static partial Status DisplayMinorStatus(
            out Status minorStatus,
            Status statusValue,
            ref GssBuffer buffer);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_DisplayMajorStatus")]
        internal static partial Status DisplayMajorStatus(
            out Status minorStatus,
            Status statusValue,
            ref GssBuffer buffer);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ImportUserName")]
        internal static partial Status ImportUserName(
            out Status minorStatus,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string inputName,
            int inputNameByteCount,
            out SafeGssNameHandle outputName);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ImportPrincipalName")]
        internal static partial Status ImportPrincipalName(
            out Status minorStatus,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string inputName,
            int inputNameByteCount,
            out SafeGssNameHandle outputName);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ReleaseName")]
        internal static partial Status ReleaseName(
            out Status minorStatus,
            ref IntPtr inputName);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_AcquireAcceptorCred")]
        internal static partial Status AcquireAcceptorCred(
            out Status minorStatus,
            out SafeGssCredHandle outputCredHandle);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_InitiateCredSpNego")]
        internal static partial Status InitiateCredSpNego(
            out Status minorStatus,
            SafeGssNameHandle desiredName,
            out SafeGssCredHandle outputCredHandle);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_InitiateCredWithPassword", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial Status InitiateCredWithPassword(
            out Status minorStatus,
            PackageType packageType,
            SafeGssNameHandle desiredName,
            string password,
            int passwordLen,
            out SafeGssCredHandle outputCredHandle);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_ReleaseCred")]
        internal static partial Status ReleaseCred(
            out Status minorStatus,
            ref IntPtr credHandle);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_InitSecContext")]
        private static partial Status InitSecContext(
            out Status minorStatus,
            SafeGssCredHandle initiatorCredHandle,
            ref SafeGssContextHandle contextHandle,
            PackageType packageType,
            SafeGssNameHandle? targetName,
            uint reqFlags,
            ref byte inputBytes,
            int inputLength,
            ref GssBuffer token,
            out uint retFlags,
            [MarshalAs(UnmanagedType.Bool)] out bool isNtlmUsed);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_InitSecContextEx")]
        private static partial Status InitSecContext(
            out Status minorStatus,
            SafeGssCredHandle initiatorCredHandle,
            ref SafeGssContextHandle contextHandle,
            PackageType packageType,
            IntPtr cbt,
            int cbtSize,
            SafeGssNameHandle? targetName,
            uint reqFlags,
            ref byte inputBytes,
            int inputLength,
            ref GssBuffer token,
            out uint retFlags,
            [MarshalAs(UnmanagedType.Bool)] out bool isNtlmUsed);

        internal static Status InitSecContext(
            out Status minorStatus,
            SafeGssCredHandle initiatorCredHandle,
            ref SafeGssContextHandle contextHandle,
            PackageType packageType,
            SafeGssNameHandle? targetName,
            uint reqFlags,
            ReadOnlySpan<byte> inputBytes,
            ref GssBuffer token,
            out uint retFlags,
            out bool isNtlmUsed)
        {
            return InitSecContext(
                out minorStatus,
                initiatorCredHandle,
                ref contextHandle,
                packageType,
                targetName,
                reqFlags,
                ref MemoryMarshal.GetReference(inputBytes),
                inputBytes.Length,
                ref token,
                out retFlags,
                out isNtlmUsed);
        }

        internal static Status InitSecContext(
            out Status minorStatus,
            SafeGssCredHandle initiatorCredHandle,
            ref SafeGssContextHandle contextHandle,
            PackageType packageType,
            IntPtr cbt,
            int cbtSize,
            SafeGssNameHandle? targetName,
            uint reqFlags,
            ReadOnlySpan<byte> inputBytes,
            ref GssBuffer token,
            out uint retFlags,
            out bool isNtlmUsed)
        {
            return InitSecContext(
                out minorStatus,
                initiatorCredHandle,
                ref contextHandle,
                packageType,
                cbt,
                cbtSize,
                targetName,
                reqFlags,
                ref MemoryMarshal.GetReference(inputBytes),
                inputBytes.Length,
                ref token,
                out retFlags,
                out isNtlmUsed);
        }

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_AcceptSecContext")]
        private static partial Status AcceptSecContext(
            out Status minorStatus,
            SafeGssCredHandle acceptorCredHandle,
            ref SafeGssContextHandle acceptContextHandle,
            ref byte inputBytes,
            int inputLength,
            ref GssBuffer token,
            out uint retFlags,
            [MarshalAs(UnmanagedType.Bool)] out bool isNtlmUsed);

        internal static Status AcceptSecContext(
            out Status minorStatus,
            SafeGssCredHandle acceptorCredHandle,
            ref SafeGssContextHandle acceptContextHandle,
            ReadOnlySpan<byte> inputBytes,
            ref GssBuffer token,
            out uint retFlags,
            out bool isNtlmUsed)
        {
            return AcceptSecContext(
                out minorStatus,
                acceptorCredHandle,
                ref acceptContextHandle,
                ref MemoryMarshal.GetReference(inputBytes),
                inputBytes.Length,
                ref token,
                out retFlags,
                out isNtlmUsed);
        }

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_DeleteSecContext")]
        internal static partial Status DeleteSecContext(
            out Status minorStatus,
            ref IntPtr contextHandle);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_GetUser")]
        internal static partial Status GetUser(
            out Status minorStatus,
            SafeGssContextHandle? acceptContextHandle,
            ref GssBuffer token);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_Wrap")]
        private static unsafe partial Status Wrap(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            [MarshalAs(UnmanagedType.Bool)] ref bool isEncrypt,
            byte* inputBytes,
            int count,
            ref GssBuffer outBuffer);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_Unwrap")]
        private static unsafe partial Status Unwrap(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            [MarshalAs(UnmanagedType.Bool)] out bool isEncrypt,
            byte* inputBytes,
            int count,
            ref GssBuffer outBuffer);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_GetMic")]
        private static unsafe partial Status GetMic(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            byte* inputBytes,
            int inputLength,
            ref GssBuffer outBuffer);

        [LibraryImport(Interop.Libraries.NetSecurityNative, EntryPoint="NetSecurityNative_VerifyMic")]
        private static unsafe partial Status VerifyMic(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            byte* inputBytes,
            int inputLength,
            byte* tokenBytes,
            int tokenLength);

        internal static unsafe Status WrapBuffer(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            ref bool isEncrypt,
            ReadOnlySpan<byte> inputBytes,
            ref GssBuffer outBuffer)
        {
            fixed (byte* inputBytesPtr = inputBytes)
            {
                return Wrap(out minorStatus, contextHandle, ref isEncrypt, inputBytesPtr, inputBytes.Length, ref outBuffer);
            }
        }

        internal static unsafe Status UnwrapBuffer(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            out bool isEncrypt,
            ReadOnlySpan<byte> inputBytes,
            ref GssBuffer outBuffer)
        {
            fixed (byte* inputBytesPtr = inputBytes)
            {
                return Unwrap(out minorStatus, contextHandle, out isEncrypt, inputBytesPtr, inputBytes.Length, ref outBuffer);
            }
        }

        internal static unsafe Status GetMic(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            ReadOnlySpan<byte> inputBytes,
            ref GssBuffer outBuffer)
        {
            fixed (byte* inputBytesPtr = inputBytes)
            {
                return GetMic(out minorStatus, contextHandle, inputBytesPtr, inputBytes.Length, ref outBuffer);
            }
        }

        internal static unsafe Status VerifyMic(
            out Status minorStatus,
            SafeGssContextHandle? contextHandle,
            ReadOnlySpan<byte> inputBytes,
            ReadOnlySpan<byte> tokenBytes)
        {
            fixed (byte* inputBytesPtr = inputBytes)
            fixed (byte* tokenBytesPtr = tokenBytes)
            {
                return VerifyMic(out minorStatus, contextHandle, inputBytesPtr, inputBytes.Length, tokenBytesPtr, tokenBytes.Length);
            }
        }
    }
}
