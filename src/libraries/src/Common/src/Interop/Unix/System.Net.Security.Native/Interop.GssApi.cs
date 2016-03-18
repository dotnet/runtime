// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static class GssApi
    {
        internal static bool EstablishSecurityContext(
            ref SafeGssContextHandle context,
            SafeGssCredHandle credential,
            bool isNtlm,
            SafeGssNameHandle targetName,
            Interop.NetSecurityNative.GssFlags inFlags,
            byte[] buffer,
            out byte[] outputBuffer,
            out uint outFlags)
        {
            outputBuffer = null;
            outFlags = 0;

            // EstablishSecurityContext is called multiple times in a session.
            // In each call, we need to pass the context handle from the previous call.
            // For the first call, the context handle will be null.
            if (context == null)
            {
                context = new SafeGssContextHandle();
            }

            Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);
            Interop.NetSecurityNative.Status status;

            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                status = NetSecurityNative.InitSecContext(out minorStatus,
                                                          credential,
                                                          ref context,
                                                          isNtlm,
                                                          targetName,
                                                          (uint)inFlags,
                                                          buffer,
                                                          (buffer == null) ? 0 : buffer.Length,
                                                          ref token,
                                                          out outFlags);

                if ((status != NetSecurityNative.Status.GSS_S_COMPLETE) && (status != NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                {
                    throw new NetSecurityNative.GssApiException(status, minorStatus);
                }

                outputBuffer = token.ToByteArray();
            }
            finally
            {
                token.Dispose();
            }

            return status == NetSecurityNative.Status.GSS_S_COMPLETE;
        }

        internal static byte[] Encrypt(
            SafeGssContextHandle context,
            bool encrypt,
            byte[] buffer,
            int offset,
            int count)
        {
            Debug.Assert((buffer != null) && (buffer.Length > 0), "Invalid input buffer passed to Encrypt");
            Debug.Assert((offset >= 0) && (offset < buffer.Length), "Invalid input offset passed to Encrypt");
            Debug.Assert((count >= 0) && (count <= (buffer.Length - offset)), "Invalid input count passed to Encrypt");

            Interop.NetSecurityNative.GssBuffer encryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
            try
            {

                NetSecurityNative.Status minorStatus;
                NetSecurityNative.Status status = NetSecurityNative.WrapBuffer(out minorStatus, context, encrypt, buffer, offset, count, ref encryptedBuffer);
                if (status != NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new NetSecurityNative.GssApiException(status, minorStatus);
                }

                return encryptedBuffer.ToByteArray();
            }
            finally
            {
                encryptedBuffer.Dispose();
            }
        }

        internal static int Decrypt(
            SafeGssContextHandle context,
            byte[] buffer,
            int offset,
            int count)
        {
            Debug.Assert((buffer != null) && (buffer.Length > 0), "Invalid input buffer passed to Decrypt");
            Debug.Assert((offset >= 0) && (offset <= buffer.Length), "Invalid input offset passed to Decrypt");
            Debug.Assert((count >= 0) && (count <= (buffer.Length - offset)), "Invalid input count passed to Decrypt");

            Interop.NetSecurityNative.GssBuffer decryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
            try
            {
                NetSecurityNative.Status minorStatus;
                NetSecurityNative.Status status = NetSecurityNative.UnwrapBuffer(out minorStatus, context, buffer, offset, count, ref decryptedBuffer);
                if (status != NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new NetSecurityNative.GssApiException(status, minorStatus);
                }

                return decryptedBuffer.Copy(buffer, offset);
            }
            finally
            {
                decryptedBuffer.Dispose();
            }
        }
    }
}


