// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    internal class RuntimeFunctions
    {
        private static TypeFields RuntimeFunctionFields(bool includeEndAddress)
        {
            TargetTestHelpers.Field[] fields = [
                new(nameof(Data.RuntimeFunction.BeginAddress), DataType.uint32),
                new(nameof(Data.RuntimeFunction.UnwindData), DataType.uint32),
            ];
            if (includeEndAddress)
                fields = fields.Append(new(nameof(Data.RuntimeFunction.EndAddress), DataType.uint32)).ToArray();

            return new()
            {
                DataType = DataType.RuntimeFunction,
                Fields = fields
            };
        }

        private static TypeFields UnwindInfoFields(bool isFunctionLength) => new()
        {
            DataType = DataType.UnwindInfo,
            Fields = isFunctionLength
                ? [new(nameof(Data.UnwindInfo.FunctionLength), DataType.uint32)]
                : [new(nameof(Data.UnwindInfo.Header), DataType.uint32)]
        };

        internal MockMemorySpace.Builder Builder { get; }
        internal Dictionary<DataType, Target.TypeInfo> Types { get; }

        private const ulong DefaultAllocationRangeStart = 0x0004_0000;
        private const ulong DefaultAllocationRangeEnd = 0x0005_0000;

        internal const uint DefaultFunctionLength = 0x100;

        private readonly MockMemorySpace.BumpAllocator _allocator;

        public RuntimeFunctions(MockMemorySpace.Builder builder, bool includeEndAddress = true, bool unwindInfoIsFunctionLength = false)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd), includeEndAddress, unwindInfoIsFunctionLength)
        { }

        public RuntimeFunctions(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange, bool includeEndAddress, bool unwindInfoIsFunctionLength)
        {
            Builder = builder;
            _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);
            Types = GetTypesForTypeFields(
                Builder.TargetTestHelpers,
                [
                    RuntimeFunctionFields(includeEndAddress),
                    UnwindInfoFields(unwindInfoIsFunctionLength)
                ]);
        }

        public TargetPointer AddRuntimeFunctions(uint[] runtimeFunctions)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;

            // Add the array of runtime functions
            uint numRuntimeFunctions = (uint)runtimeFunctions.Length;
            Target.TypeInfo runtimeFunctionType = Types[DataType.RuntimeFunction];
            uint runtimeFunctionSize = runtimeFunctionType.Size.Value;
            Target.TypeInfo unwindInfoType = Types[DataType.UnwindInfo];
            MockMemorySpace.HeapFragment runtimeFunctionsFragment = _allocator.Allocate((numRuntimeFunctions + 1) * runtimeFunctionSize, $"RuntimeFunctions[{numRuntimeFunctions}]");
            Builder.AddHeapFragment(runtimeFunctionsFragment);
            for (uint i = 0; i < numRuntimeFunctions; i++)
            {
                Span<byte> func = Builder.BorrowAddressRange(runtimeFunctionsFragment.Address + i * runtimeFunctionSize, (int)runtimeFunctionSize);
                helpers.Write(func.Slice(runtimeFunctionType.Fields[nameof(Data.RuntimeFunction.BeginAddress)].Offset, sizeof(uint)), runtimeFunctions[i]);

                // Set the function length to the default function length or up to the next function start
                uint functionLength = i < numRuntimeFunctions - 1
                    ? Math.Min(runtimeFunctions[i + 1] - runtimeFunctions[i], DefaultFunctionLength)
                    : DefaultFunctionLength;
                if (runtimeFunctionType.Fields.ContainsKey(nameof(Data.RuntimeFunction.EndAddress)))
                    helpers.Write(func.Slice(runtimeFunctionType.Fields[nameof(Data.RuntimeFunction.EndAddress)].Offset, sizeof(uint)), runtimeFunctions[i] + functionLength);

                // Add the unwindInfo
                MockMemorySpace.HeapFragment unwindInfoFragment = _allocator.Allocate(unwindInfoType.Size.Value, $"UnwindInfo for RuntimeFunction {runtimeFunctions[i]}");
                Builder.AddHeapFragment(unwindInfoFragment);
                Span<byte> unwindInfo = unwindInfoFragment.Data.AsSpan();
                if (Types[DataType.UnwindInfo].Fields.ContainsKey(nameof(Data.UnwindInfo.FunctionLength)))
                {
                    helpers.Write(unwindInfo.Slice(unwindInfoType.Fields[nameof(Data.UnwindInfo.FunctionLength)].Offset, sizeof(uint)), functionLength);
                }
                else
                {
                    // First 18 bits of the header are function length / (pointer size / 2) 
                    uint headerBits = (uint)(functionLength / (helpers.PointerSize / 2));
                    if (headerBits > 1 << 18 - 1)
                        throw new InvalidOperationException("Function length is too long ");

                    helpers.Write(unwindInfo.Slice(unwindInfoType.Fields[nameof(Data.UnwindInfo.Header)].Offset, sizeof(uint)), headerBits);
                }

                helpers.Write(func.Slice(runtimeFunctionType.Fields[nameof(Data.RuntimeFunction.UnwindData)].Offset, sizeof(uint)), (uint)unwindInfoFragment.Address);
            }

            // Runtime function entries are terminated by a sentinel value of -1
            Span<byte> sentinel = Builder.BorrowAddressRange(runtimeFunctionsFragment.Address + numRuntimeFunctions * runtimeFunctionSize, (int)runtimeFunctionSize);
            helpers.Write(sentinel.Slice(runtimeFunctionType.Fields[nameof(Data.RuntimeFunction.BeginAddress)].Offset, sizeof(uint)), ~0u);

            return runtimeFunctionsFragment.Address;
        }
    }
}
