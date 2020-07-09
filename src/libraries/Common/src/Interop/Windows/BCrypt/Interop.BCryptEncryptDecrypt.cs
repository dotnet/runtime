// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        // Note: input and output are allowed to be the same buffer. BCryptEncrypt will correctly do the encryption in place according to CNG documentation.
        internal static int BCryptEncrypt(SafeKeyHandle hKey, byte[] input, int inputOffset, int inputCount, byte[]? iv, byte[] output, int outputOffset, int outputCount)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(inputCount >= 0);
            Debug.Assert(inputCount <= input.Length - inputOffset);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(outputCount >= 0);
            Debug.Assert(outputCount <= output.Length - outputOffset);

            unsafe
            {
                fixed (byte* pbInput = input)
                {
                    fixed (byte* pbOutput = output)
                    {
                        int cbResult;
                        NTSTATUS ntStatus = BCryptEncrypt(hKey, pbInput + inputOffset, inputCount, IntPtr.Zero, iv, iv == null ? 0 : iv.Length, pbOutput + outputOffset, outputCount, out cbResult, 0);
                        if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                            throw CreateCryptographicException(ntStatus);
                        return cbResult;
                    }
                }
            }
        }

        // Note: input and output are allowed to be the same buffer. BCryptDecrypt will correctly do the decryption in place according to CNG documentation.
        internal static int BCryptDecrypt(SafeKeyHandle hKey, byte[] input, int inputOffset, int inputCount, byte[]? iv, byte[] output, int outputOffset, int outputCount)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(inputCount >= 0);
            Debug.Assert(inputCount <= input.Length - inputOffset);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(outputCount >= 0);
            Debug.Assert(outputCount <= output.Length - outputOffset);

            unsafe
            {
                fixed (byte* pbInput = input)
                {
                    fixed (byte* pbOutput = output)
                    {
                        int cbResult;
                        NTSTATUS ntStatus = BCryptDecrypt(hKey, pbInput + inputOffset, inputCount, IntPtr.Zero, iv, iv == null ? 0 : iv.Length, pbOutput + outputOffset, outputCount, out cbResult, 0);
                        if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                            throw CreateCryptographicException(ntStatus);
                        return cbResult;
                    }
                }
            }
        }

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        public static extern unsafe NTSTATUS BCryptEncrypt(SafeKeyHandle hKey, byte* pbInput, int cbInput, IntPtr paddingInfo, [In, Out] byte[]? pbIV, int cbIV, byte* pbOutput, int cbOutput, out int cbResult, int dwFlags);

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        public static extern unsafe NTSTATUS BCryptDecrypt(SafeKeyHandle hKey, byte* pbInput, int cbInput, IntPtr paddingInfo, [In, Out] byte[]? pbIV, int cbIV, byte* pbOutput, int cbOutput, out int cbResult, int dwFlags);
    }
}
