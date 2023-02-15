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
        internal struct host_runtime_contract
        {
            public nint size;
            public void* context;
            public delegate* unmanaged[Stdcall]<byte*, byte*, nint, void*, nint> get_runtime_property;
            public IntPtr bundle_probe;
            public IntPtr pinvoke_override;
        }

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

        public static bool RunTest(string apiToTest, string[] args)
        {
            switch (apiToTest)
            {
                case $"{nameof(host_runtime_contract)}.{nameof(host_runtime_contract.get_runtime_property)}":
                    Test_get_runtime_property(args);
                    break;
                default:
                    return false;
            }

            return true;
        }
    }

}
