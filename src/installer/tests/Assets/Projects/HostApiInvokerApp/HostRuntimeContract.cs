// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace HostApiInvokerApp
{
    public static unsafe class HostRuntimeContract
    {
#pragma warning disable CS0649
        internal struct host_runtime_contract
        {
            public nint size;
            public void* context;
            public delegate* unmanaged[Stdcall]<byte*, byte*, nint, void*, nint> get_runtime_property;
            public delegate* unmanaged[Stdcall]<byte*, nint, nint, nint, byte> bundle_probe;
            public IntPtr pinvoke_override;
        }
#pragma warning restore CS0649

        private static host_runtime_contract GetContract()
        {
            string contractString = (string)AppContext.GetData("HOST_RUNTIME_CONTRACT");
            if (string.IsNullOrEmpty(contractString))
                throw new Exception("HOST_RUNTIME_CONTRACT not found");

            host_runtime_contract* contract = (host_runtime_contract*)Convert.ToUInt64(contractString, 16);
            if (contract->size != sizeof(host_runtime_contract))
                throw new Exception($"Unexpected contract size {contract->size}. Expected: {sizeof(host_runtime_contract)}");

            return *contract;
        }

        private static void Test_get_runtime_property(string[] args)
        {
            host_runtime_contract contract = GetContract();

            foreach (string name in args)
            {
                string value = GetProperty(name, contract);
                Console.WriteLine($"{nameof(host_runtime_contract.get_runtime_property)}: {name} = {(value == null ? "<none>" : value)}");
            }

            static string GetProperty(string name, host_runtime_contract contract)
            {
                Span<byte> nameSpan = stackalloc byte[Encoding.UTF8.GetMaxByteCount(name.Length)];
                byte* namePtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(nameSpan));
                int nameLen = Encoding.UTF8.GetBytes(name, nameSpan);
                nameSpan[nameLen] = 0;

                nint len = 256;
                byte* buffer = stackalloc byte[(int)len];
                nint lenActual = contract.get_runtime_property(namePtr, buffer, len, contract.context);
                if (lenActual <= 0)
                {
                    Console.WriteLine($"No value for {name} - {nameof(host_runtime_contract.get_runtime_property)} returned {lenActual}");
                    return null;
                }

                if (lenActual <= len)
                    return Encoding.UTF8.GetString(buffer, (int)lenActual);

                len = lenActual;
                byte* expandedBuffer = stackalloc byte[(int)len];
                lenActual = contract.get_runtime_property(namePtr, expandedBuffer, len, contract.context);
                return Encoding.UTF8.GetString(expandedBuffer, (int)lenActual);
            }
        }

        public static void Test_bundle_probe(string[] args)
        {
            host_runtime_contract contract = GetContract();
            if (contract.bundle_probe == null)
            {
                Console.WriteLine("host_runtime_contract.bundle_probe is not set");
                return;
            }

            foreach (string path in args)
            {
                Probe(contract, path);
            }

            unsafe static void Probe(host_runtime_contract contract, string path)
            {
                Span<byte> pathSpan = stackalloc byte[Encoding.UTF8.GetMaxByteCount(path.Length)];
                byte* pathPtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(pathSpan));
                int pathLen = Encoding.UTF8.GetBytes(path, pathSpan);
                pathSpan[pathLen] = 0;

                Int64 size, offset, compressedSize;
                bool exists = contract.bundle_probe(pathPtr, (IntPtr)(&offset), (IntPtr)(&size), (IntPtr)(&compressedSize)) != 0;

                Console.WriteLine($"{nameof(host_runtime_contract.get_runtime_property)}: {path} - found = {exists}");
                if (exists)
                {
                    if (compressedSize < 0 || compressedSize > size)
                        throw new Exception($"Invalid compressedSize obtained for {path} within bundle.");

                    if (size <= 0 || offset <= 0)
                        throw new Exception($"Invalid location obtained for {path} within bundle.");
                }
            }
        }

        public static bool RunTest(string apiToTest, string[] args)
        {
            switch (apiToTest)
            {
                case $"{nameof(host_runtime_contract)}.{nameof(host_runtime_contract.get_runtime_property)}":
                    Test_get_runtime_property(args[1..]);
                    break;
                case $"{nameof(host_runtime_contract)}.{nameof(host_runtime_contract.bundle_probe)}":
                    Test_bundle_probe(args[1..]);
                    break;
                default:
                    return false;
            }

            return true;
        }
    }

}
