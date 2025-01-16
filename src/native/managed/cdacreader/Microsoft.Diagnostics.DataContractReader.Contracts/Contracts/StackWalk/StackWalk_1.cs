// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct StackWalk_1 : IStackWalk
{
    private readonly Target _target;

    internal StackWalk_1(Target target)
    {
        _target = target;
    }

    private AMD64Context GetThreadContext(ThreadData threadData)
    {
        int hr;

        unsafe
        {
            byte[] bytes = new byte[AMD64Context.Size];

            fixed (byte* ptr = bytes)
            {
                Span<byte> buffer = bytes;
                hr = _target.GetThreadContext((uint)threadData.OSId.Value, AMD64Context.DefaultContextFlags, AMD64Context.Size, buffer);
            }

            if (hr != 0)
            {
                throw new InvalidOperationException($"GetThreadContext failed with hr={hr}");
            }

            AMD64Context context = Marshal.PtrToStructure<AMD64Context>((IntPtr)Unsafe.AsPointer(ref bytes[0]));
            return context;
        }
    }

    void IStackWalk.TestEntry()
    {
        string outputhPath = "C:\\Users\\maxcharlamb\\OneDrive - Microsoft\\Desktop\\out.txt";
        using StreamWriter writer = new StreamWriter(outputhPath);
        Console.SetOut(writer);

        ThreadStoreData tsdata = _target.Contracts.Thread.GetThreadStoreData();
        ThreadData threadData = _target.Contracts.Thread.GetThreadData(tsdata.FirstThread);

        IExecutionManager eman = _target.Contracts.ExecutionManager;

        AMD64Context context = GetThreadContext(threadData);
        Console.WriteLine($"[AMD64Context: RIP={context.InstructionPointer.Value:x16} RSP={context.StackPointer.Value:x16}]");
        CheckIP(new(context.InstructionPointer.Value));

        foreach (Frame frame in FrameIterator.EnumerateFrames(_target, threadData.Frame))
        {
            FrameIterator.PrintFrame(_target, frame);
            if (FrameIterator.TryGetContext(_target, frame, out TargetPointer? IP, out TargetPointer? SP))
            {
                CheckIP(new(IP.Value));
            }
        }


        Console.WriteLine(context.ToString());

        writer.Flush();
    }

    private void CheckIP(TargetCodePointer ip)
    {
        IExecutionManager eman = _target.Contracts.ExecutionManager;
        if (eman.GetCodeBlockHandle(ip) is CodeBlockHandle cbh)
        {
            TargetPointer methodDesc = eman.GetMethodDesc(cbh);
            TargetPointer moduleBase = eman.GetModuleBaseAddress(cbh);
            TargetPointer unwindInfo = eman.GetUnwindInfo(cbh, ip);
            Console.WriteLine($"MethodDesc: {methodDesc.Value:x16} BaseAddress: {moduleBase.Value:x16} UnwindInfo: {unwindInfo.Value:x16}");
        }
        else
        {
            Console.WriteLine("IP is unmanaged");
        }
    }
}
