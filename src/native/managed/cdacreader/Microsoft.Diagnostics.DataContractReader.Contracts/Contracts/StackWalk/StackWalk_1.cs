// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct StackWalk_1 : IStackWalk
{
    private readonly Target _target;

    internal StackWalk_1(Target target)
    {
        _target = target;
    }

    private T GetThreadContext<T>(ThreadData threadData)
        where T : struct, IContext
    {
        int hr;
        unsafe
        {
            byte[] bytes = new byte[T.Size];
            Span<byte> buffer = new Span<byte>(bytes);
            hr = _target.GetThreadContext((uint)threadData.OSId.Value, T.DefaultContextFlags, (uint)T.Size, buffer);
            if (hr != 0)
            {
                throw new InvalidOperationException($"GetThreadContext failed with hr={hr}");
            }

            T context = default;
            Span<T> structSpan = MemoryMarshal.CreateSpan(ref context, sizeof(T));
            Span<byte> byteSpan = MemoryMarshal.Cast<T, byte>(structSpan);
            buffer.Slice(0, sizeof(T)).CopyTo(byteSpan);
            return context;
        }
    }

    void IStackWalk.TestEntry()
    {
        string outputhPath = "C:\\Users\\maxcharlamb\\OneDrive - Microsoft\\Desktop\\out.txt";
        using StreamWriter writer = new StreamWriter(outputhPath);
        Console.SetOut(writer);

        try
        {
            Handle();
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            writer.Close();
        }
    }

    private void Handle()
    {
        ThreadStoreData tsdata = _target.Contracts.Thread.GetThreadStoreData();
        ThreadData threadData = _target.Contracts.Thread.GetThreadData(tsdata.FirstThread);

        _target.GetPlatform(out Target.CorDebugPlatform platform);
        Console.WriteLine(platform.ToString());
        try
        {
            IContext context;
            switch (platform)
            {
                case Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_AMD64:
                case Target.CorDebugPlatform.CORDB_PLATFORM_POSIX_AMD64:
                case Target.CorDebugPlatform.CORDB_PLATFORM_MAC_AMD64:
                    context = GetThreadContext<AMD64Context>(threadData);
                    Console.WriteLine($"[{context.GetType().Name}: RIP={context.InstructionPointer.Value:x16} RSP={context.StackPointer.Value:x16}]");
                    Unwind<AMD64Context>(threadData);
                    break;
                case Target.CorDebugPlatform.CORDB_PLATFORM_POSIX_ARM64:
                case Target.CorDebugPlatform.CORDB_PLATFORM_WINDOWS_ARM64:
                    context = GetThreadContext<ARM64Context>(threadData);
                    Console.WriteLine($"[{context.GetType().Name}: RIP={context.InstructionPointer.Value:x16} RSP={context.StackPointer.Value:x16}]");
                    Unwind<ARM64Context>(threadData);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex);
        }
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

    private void Unwind<T>(ThreadData threadData)
        where T : struct, IContext
    {
        IContext context = GetThreadContext<T>(threadData);
        UnwindUntilNative(ref context);

        foreach (Data.Frame frame in FrameIterator.EnumerateFrames(_target, threadData.Frame))
        {
            FrameIterator.PrintFrame(_target, frame);
            if (FrameIterator.TryUpdateContext(_target, frame, ref context))
            {
                UnwindUntilNative(ref context);
            }
        }
    }

    private void UnwindUntilNative(ref IContext context)
    {
        IExecutionManager eman = _target.Contracts.ExecutionManager;

        TargetPointer startSp = context.StackPointer;

        while (eman.GetCodeBlockHandle(new(context.InstructionPointer)) is CodeBlockHandle cbh && cbh.Address != TargetPointer.Null)
        {
            TargetPointer methodDesc = eman.GetMethodDesc(cbh);
            Console.WriteLine($"[{context.GetType().Name}] IP: {context.InstructionPointer.Value:x16} SP: {context.StackPointer.Value:x16} MethodDesc: {methodDesc.Value:x16}");
            try
            {
                context.Unwind(_target);

            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        Console.WriteLine($"IP: {context.InstructionPointer.Value:x16} SP: {context.StackPointer.Value:x16}");
        Console.WriteLine($"IP is unmanaged, finishing unwind started at {startSp.Value:x16}");
    }
};
