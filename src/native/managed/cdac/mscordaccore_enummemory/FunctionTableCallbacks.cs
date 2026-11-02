// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>Provides native callbacks for enumerating Windows JIT function tables.</summary>
public static unsafe partial class FunctionTableCallbacks
{
    private const int StatusSuccess = 0;
    private const int StatusUnsuccessful = unchecked((int)0xc0000001);
    private const int StatusInvalidParameter = unchecked((int)0xc000000d);
    private const int StatusNoMemory = unchecked((int)0xc0000017);
    private const int StatusInvalidParameter3 = unchecked((int)0xc00000f1);
    private const int StatusInvalidParameter4 = unchecked((int)0xc00000f2);

    private static readonly ContractDescriptorParser.ContractDescriptor s_descriptor = CreateDescriptor();

    /// <summary>Enumerates a target function table using a process handle.</summary>
    /// <param name="process">The target process handle.</param>
    /// <param name="tableAddress">The address of the target dynamic function table.</param>
    /// <param name="entries">The location that receives the number of returned records on success.</param>
    /// <param name="functions">The location that receives the process-heap allocation on success.</param>
    /// <returns>An NTSTATUS value indicating success or failure.</returns>
    [UnmanagedCallersOnly(EntryPoint = "OutOfProcessFunctionTableCallback", CallConvs = [typeof(CallConvCdecl)])]
    public static int OutOfProcessFunctionTableCallback(nint process, nuint tableAddress, uint* entries, void** functions)
        => Enumerate(&ReadProcess, (void*)process, tableAddress, entries, functions);

    /// <summary>Enumerates a target function table using a caller-supplied memory reader.</summary>
    /// <param name="readMemory">The callback that reads target memory.</param>
    /// <param name="context">The context passed to the memory reader.</param>
    /// <param name="tableAddress">The address of the target dynamic function table.</param>
    /// <param name="entries">The location that receives the number of returned records on success.</param>
    /// <param name="functions">The location that receives the process-heap allocation on success.</param>
    /// <returns>An NTSTATUS value indicating success or failure.</returns>
    [UnmanagedCallersOnly(EntryPoint = "OutOfProcessFunctionTableCallbackEx", CallConvs = [typeof(CallConvCdecl)])]
    public static int OutOfProcessFunctionTableCallbackEx(
        delegate* unmanaged[Cdecl]<void*, void*, void*, nuint, nuint*, int> readMemory,
        void* context, nuint tableAddress, uint* entries, void** functions)
        => Enumerate(readMemory, context, tableAddress, entries, functions);

    private static int Enumerate(
        delegate* unmanaged[Cdecl]<void*, void*, void*, nuint, nuint*, int> readMemory,
        void* context, nuint tableAddress, uint* entries, void** functions)
    {
        if (!OperatingSystem.IsWindows() ||
            RuntimeInformation.ProcessArchitecture is not (Architecture.X64 or Architecture.Arm64))
        {
            return StatusUnsuccessful;
        }

        if (entries is not null)
            *entries = 0;
        if (functions is not null)
            *functions = null;
        if (entries is null)
            return StatusInvalidParameter3;
        if (functions is null)
            return StatusInvalidParameter4;
        if (readMemory is null || tableAddress == 0)
            return StatusInvalidParameter;
        void* allocation = null;
        try
        {
            Reader reader = new(readMemory, context);
            // A target is local to this call so cached fields cannot outlive the memory snapshot.
            ContractDescriptorTarget target = ContractDescriptorTarget.Create(
                s_descriptor, [], reader.Read,
                static (_, _) => HResults.E_NOTIMPL,
                static (_, _, _) => HResults.E_NOTIMPL,
                static (_, _) => HResults.E_NOTIMPL,
                static (ulong _, out ulong address) => { address = 0; return HResults.E_NOTIMPL; },
                isLittleEndian: true, pointerSize: sizeof(ulong),
                contractRegistrations: [CoreCLRContracts.Register]);
            IReadOnlyList<TargetPointer> functionEntries = target.Contracts.ExecutionManager.GetDynamicFunctionTableEntries(new(tableAddress));
            if (functionEntries.Count == 0)
                return StatusSuccess;

            int entrySize = checked((int)target.GetTypeInfo(DataType.RuntimeFunction).Size!.Value);
            nuint byteCount = checked((nuint)functionEntries.Count * (nuint)entrySize);
            allocation = HeapAlloc(GetProcessHeap(), 0, byteCount);
            if (allocation is null)
                return StatusNoMemory;

            for (int i = 0; i < functionEntries.Count; i++)
            {
                byte* buffer = (byte*)allocation + checked((nint)((nuint)i * (nuint)entrySize));
                target.ReadBuffer(functionEntries[i], new Span<byte>(buffer, entrySize));
            }

            *entries = checked((uint)functionEntries.Count);
            *functions = allocation;
            allocation = null;
            return StatusSuccess;
        }
        catch (OutOfMemoryException)
        {
            return StatusNoMemory;
        }
        catch (System.Exception ex) when (ex is VirtualReadException or InvalidOperationException or OverflowException or ArgumentException)
        {
            return StatusUnsuccessful;
        }
        finally
        {
            if (allocation is not null)
                HeapFree(GetProcessHeap(), 0, allocation);
        }
    }

    private static ContractDescriptorParser.ContractDescriptor CreateDescriptor()
    {
        // Windows 64-bit layouts, checked against the VM in debug/daccess/fntableaccess.h.
        return new()
        {
            Version = 2,
            Contracts = new()
            {
                [nameof(ExecutionManager)] = "c1",
                [nameof(RuntimeInfo)] = "c1",
            },
            Types = new()
            {
                [nameof(DataType.DynamicFunctionTable)] = Type(("MinimumAddress", 0x20), ("Context", 0x40)),
                [nameof(DataType.EEJitManager)] = Type(("AllCodeHeaps", 0x10)),
                [nameof(DataType.CodeHeapListNode)] = Type(
                    ("Next", 0), ("StartAddress", 0x10), ("EndAddress", 0x18),
                    ("MapBase", 0x20), ("HeaderMap", 0x28), ("CLRPersonalityRoutine", 0x40)),
                [nameof(DataType.RealCodeHeader)] = Type(("NumUnwindInfos", 0x20), ("UnwindInfos", 0x24)),
                [nameof(DataType.RuntimeFunction)] = new()
                {
                    Size = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? 8u : 12u,
                },
            },
            Globals = new()
            {
                [Constants.Globals.OperatingSystem] = new() { StringValue = nameof(RuntimeInfoOperatingSystem.Windows) },
                [Constants.Globals.Architecture] = new()
                {
                    StringValue = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                        ? nameof(RuntimeInfoArchitecture.Arm64) : nameof(RuntimeInfoArchitecture.X64),
                },
                [Constants.Globals.StubCodeBlockLast] = new() { NumericValue = 0xf },
                [Constants.Globals.HashMapValueMask] = new() { NumericValue = 0x7fff_ffff_ffff_ffff },
                // Required by construction, but this callback never looks up the range-section map.
                [Constants.Globals.ExecutionManagerCodeRangeMapAddress] = new() { NumericValue = 0, Type = nameof(DataType.pointer) },
            },
        };

        static ContractDescriptorParser.TypeDescriptor Type(params (string Name, int Offset)[] fields)
        {
            Dictionary<string, ContractDescriptorParser.FieldDescriptor> descriptors = new(fields.Length);
            foreach ((string name, int offset) in fields)
                descriptors.Add(name, new() { Offset = offset });
            return new() { Fields = descriptors };
        }
    }

    private readonly struct Reader(delegate* unmanaged[Cdecl]<void*, void*, void*, nuint, nuint*, int> readMemory, void* context)
    {
        public int Read(ulong address, Span<byte> destination)
        {
            nuint bytesRead = 0;
            fixed (byte* buffer = destination)
                return readMemory(context, (void*)checked((nuint)address), buffer, (nuint)destination.Length, &bytesRead) != 0 &&
                    bytesRead == (nuint)destination.Length ? HResults.S_OK : HResults.E_FAIL;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int ReadProcess(void* context, void* address, void* buffer, nuint size, nuint* bytesRead)
        => ReadProcessMemory((nint)context, address, buffer, size, bytesRead);

    [LibraryImport("kernel32.dll")]
    private static partial int ReadProcessMemory(nint process, void* address, void* buffer, nuint size, nuint* bytesRead);

    [LibraryImport("kernel32.dll")]
    private static partial nint GetProcessHeap();

    [LibraryImport("kernel32.dll")]
    private static partial void* HeapAlloc(nint heap, uint flags, nuint size);

    [LibraryImport("kernel32.dll")]
    private static partial int HeapFree(nint heap, uint flags, void* allocation);
}
