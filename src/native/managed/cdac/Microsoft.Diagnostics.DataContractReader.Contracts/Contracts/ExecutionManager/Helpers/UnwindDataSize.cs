// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal static class UnwindDataSize
{
    public static uint GetUnwindDataSize(Target target, TargetPointer unwindInfo, TargetPointer moduleBase, RuntimeInfoArchitecture arch)
    {
        switch (arch)
        {
            case RuntimeInfoArchitecture.X86:
                return sizeof(uint);
            case RuntimeInfoArchitecture.X64:
                // see https://learn.microsoft.com/cpp/build/exception-handling-x64
                uint sizeOfUnwindCode = 2; // from spec
                uint unwindCodeOffset = 4; // from spec
                Data.UnwindInfo unwind = target.ProcessedData.GetOrAdd<Data.UnwindInfo>(unwindInfo);
                return AlignUp((int)(sizeof(uint) +
                (unwind.CountOfUnwindCodes * sizeOfUnwindCode)
                + unwindCodeOffset), sizeof(uint));
            case RuntimeInfoArchitecture.Arm:
            case RuntimeInfoArchitecture.Arm64:
                TargetPointer xdata = unwindInfo;
                uint xdata0 = target.Read<uint>(xdata);
                uint size = 4;                     // initial header
                uint unwindWords;
                uint epilogScopes;
                if (arch == RuntimeInfoArchitecture.Arm)
                {
                    // See https://learn.microsoft.com/cpp/build/arm-exception-handling
                    unwindWords = xdata0 >> 28;
                    epilogScopes = (xdata0 >> 23) & 0x1F;
                }
                else
                {
                    // See https://learn.microsoft.com/cpp/build/arm64-exception-handling
                    unwindWords = xdata0 >> 27;
                    epilogScopes = (xdata0 >> 22) & 0x1F;
                }
                if (unwindWords == 0 && epilogScopes == 0)
                {
                    size += 4;
                    uint xdata1 = target.Read<uint>(xdata + 4);
                    unwindWords = (xdata1 >> 16) & 0xff;
                    epilogScopes = xdata1 & 0xffff;
                }

                if ((xdata0 & (1 << 21)) != 0)
                    size += 4 * epilogScopes;

                size += 4 * unwindWords;
                size += 4;
                return size;

            case RuntimeInfoArchitecture.LoongArch64:
            case RuntimeInfoArchitecture.RiscV64:
                xdata = unwindInfo;
                xdata0 = target.Read<uint>(xdata);

                // If both Epilog Count and Code Word is not zero
                // Info of Epilog and Unwind scopes are given by 1 word header
                // Otherwise this info is given by a 2 word header
                if ((xdata0 >> 27) != 0)
                {
                    size = 4;
                    epilogScopes = (xdata0 >> 22) & 0x1f;
                    unwindWords = (xdata0 >> 27) & 0x1f;
                }
                else
                {
                    size = 8;
                    uint xdata1 = target.Read<uint>(xdata + 4);
                    epilogScopes = xdata1 & 0xffff;
                    unwindWords = (xdata1 >> 16) & 0xff;
                }

                if ((xdata0 & (1 << 21)) != 0)
                    size += 4 * epilogScopes;

                size += 4 * unwindWords;

                size += 4;                      // exception handler RVA
                return size;
            default:
                throw new NotSupportedException($"GetUnwindDataSize not supported for architecture: {arch}");
        }
    }
    private static uint AlignUp(int offset, int align)
        => (uint)((offset + align - 1) & ~(align - 1));
}
