// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Internal.Win32.SafeHandles;

namespace System.Diagnostics
{
    internal sealed class PerformanceDataRegistryKey : IDisposable
    {
        private const int PerformanceData = (int)RegistryHive.PerformanceData;

        private readonly SafeRegistryHandle _hkey;

        private PerformanceDataRegistryKey(SafeRegistryHandle hkey)
        {
            _hkey = hkey;
        }

        public static PerformanceDataRegistryKey OpenRemoteBaseKey(string machineName)
        {
            // connect to the specified remote registry
            int ret = Interop.Advapi32.RegConnectRegistry(machineName, new IntPtr(PerformanceData), out SafeRegistryHandle foreignHKey);
            if (ret == 0 && !foreignHKey.IsInvalid)
            {
                return new PerformanceDataRegistryKey(foreignHKey);
            }

            foreignHKey.Dispose();

            if (ret == Interop.Errors.ERROR_DLL_INIT_FAILED)
            {
                // return value indicates an error occurred
                throw new ArgumentException(SR.Format(SR.Arg_DllInitFailure, machineName));
            }

            if (ret != 0)
            {
                Win32Error(ret, null);
            }

            // return value indicates an error occurred
            throw new ArgumentException(SR.Format(SR.Arg_RegKeyNoRemoteConnect, machineName));
        }

        public static PerformanceDataRegistryKey OpenLocal()
        {
            var key = new SafeRegistryHandle(new IntPtr(PerformanceData), ownsHandle: true);
            return new PerformanceDataRegistryKey(key);
        }

        public byte[] GetValue(string name, bool usePool)
        {
            int size = 65000;
            int sizeInput = size;

            int ret;
            int type = 0;
            byte[] data = CreateBlob(size, usePool);
            while (Interop.Errors.ERROR_MORE_DATA == (ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, lpReserved: null, ref type, data, ref sizeInput)))
            {
                if (size == int.MaxValue)
                {
                    ReleaseData(data, usePool);

                    // ERROR_MORE_DATA was returned however we cannot increase the buffer size beyond Int32.MaxValue
                    Win32Error(ret, name);
                }
                else if (size > (int.MaxValue / 2))
                {
                    // at this point in the loop "size * 2" would cause an overflow
                    size = int.MaxValue;
                }
                else
                {
                    size *= 2;
                }
                sizeInput = size;

                ReleaseData(data, usePool);
                data = CreateBlob(size, usePool);
            }

            if (ret != 0)
            {
                ReleaseData(data, usePool);
                Win32Error(ret, name);
            }

            return data;
        }

        internal static void ReleaseData(byte[] data, bool usePool = true)
        {
            if (usePool)
            {
                ArrayPool<byte>.Shared.Return(data);
            }
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            _hkey.Dispose();
        }

        private static byte[] CreateBlob(int size, in bool usePool)
        {
            return usePool
                ? ArrayPool<byte>.Shared.Rent(size)
                : new byte[size];
        }

        private static void Win32Error(in int errorCode, string name)
        {
            if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED)
            {
                throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, name));
            }

            throw new IOException(Interop.Kernel32.GetMessage(errorCode), errorCode);
        }

    }
}
