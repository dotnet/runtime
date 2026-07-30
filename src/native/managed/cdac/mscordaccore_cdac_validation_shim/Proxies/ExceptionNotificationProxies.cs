// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Wraps the caller's exception-notification sink while the production cDAC runs: every notification
/// is forwarded to the caller once. The pre-refactor cDAC did not compare notification callbacks.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class RecordingExceptionNotification
    : IXCLRDataExceptionNotification, IXCLRDataExceptionNotification2, IXCLRDataExceptionNotification3,
      IXCLRDataExceptionNotification4, IXCLRDataExceptionNotification5, ICustomQueryInterface
{
    private readonly IXCLRDataExceptionNotification _inner;
    private readonly IXCLRDataExceptionNotification2? _inner2;
    private readonly IXCLRDataExceptionNotification3? _inner3;
    private readonly IXCLRDataExceptionNotification4? _inner4;
    private readonly IXCLRDataExceptionNotification5? _inner5;

    internal RecordingExceptionNotification(IXCLRDataExceptionNotification inner)
    {
        _inner = inner;
        _inner2 = inner as IXCLRDataExceptionNotification2;
        _inner3 = inner as IXCLRDataExceptionNotification3;
        _inner4 = inner as IXCLRDataExceptionNotification4;
        _inner5 = inner as IXCLRDataExceptionNotification5;
    }

    private static int Record(string method, int hr, params object?[] arguments)
    {
        ShimCall.Current?.RecordCallback(new CallbackInvocation
        {
            Method = method,
            Arguments = arguments,
            HResult = hr,
        });
        return hr;
    }

    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
    {
        ppv = IntPtr.Zero;

        if ((iid == typeof(IXCLRDataExceptionNotification2).GUID && _inner2 is null)
            || (iid == typeof(IXCLRDataExceptionNotification3).GUID && _inner3 is null)
            || (iid == typeof(IXCLRDataExceptionNotification4).GUID && _inner4 is null)
            || (iid == typeof(IXCLRDataExceptionNotification5).GUID && _inner5 is null))
        {
            return CustomQueryInterfaceResult.Failed;
        }

        return CustomQueryInterfaceResult.NotHandled;
    }

    int IXCLRDataExceptionNotification.OnCodeGenerated(IXCLRDataMethodInstance? method)
        => Record(nameof(IXCLRDataExceptionNotification.OnCodeGenerated), _inner.OnCodeGenerated(method), method);

    int IXCLRDataExceptionNotification.OnCodeDiscarded(IXCLRDataMethodInstance? method)
        => Record(nameof(IXCLRDataExceptionNotification.OnCodeDiscarded), _inner.OnCodeDiscarded(method), method);

    int IXCLRDataExceptionNotification.OnProcessExecution(uint state)
        => Record(nameof(IXCLRDataExceptionNotification.OnProcessExecution), _inner.OnProcessExecution(state), state);

    int IXCLRDataExceptionNotification.OnTaskExecution(void* task, uint state)
        => Record(nameof(IXCLRDataExceptionNotification.OnTaskExecution), _inner.OnTaskExecution(task, state), state);

    int IXCLRDataExceptionNotification.OnModuleLoaded(IXCLRDataModule? mod)
        => Record(nameof(IXCLRDataExceptionNotification.OnModuleLoaded), _inner.OnModuleLoaded(mod), mod);

    int IXCLRDataExceptionNotification.OnModuleUnloaded(IXCLRDataModule? mod)
        => Record(nameof(IXCLRDataExceptionNotification.OnModuleUnloaded), _inner.OnModuleUnloaded(mod), mod);

    int IXCLRDataExceptionNotification.OnTypeLoaded(void* typeInst)
        => Record(nameof(IXCLRDataExceptionNotification.OnTypeLoaded), _inner.OnTypeLoaded(typeInst));

    int IXCLRDataExceptionNotification.OnTypeUnloaded(void* typeInst)
        => Record(nameof(IXCLRDataExceptionNotification.OnTypeUnloaded), _inner.OnTypeUnloaded(typeInst));

    int IXCLRDataExceptionNotification2.OnAppDomainLoaded(void* domain)
        => Record(nameof(IXCLRDataExceptionNotification2.OnAppDomainLoaded),
            _inner2 is null ? HResults.E_NOTIMPL : _inner2.OnAppDomainLoaded(domain));

    int IXCLRDataExceptionNotification2.OnAppDomainUnloaded(void* domain)
        => Record(nameof(IXCLRDataExceptionNotification2.OnAppDomainUnloaded),
            _inner2 is null ? HResults.E_NOTIMPL : _inner2.OnAppDomainUnloaded(domain));

    int IXCLRDataExceptionNotification2.OnException(IXCLRDataExceptionState? exception)
        => Record(nameof(IXCLRDataExceptionNotification2.OnException),
            _inner2 is null ? HResults.E_NOTIMPL : _inner2.OnException(exception), exception);

    int IXCLRDataExceptionNotification3.OnGcEvent(GcEvtArgs gcEvtArgs)
        => Record(nameof(IXCLRDataExceptionNotification3.OnGcEvent),
            _inner3 is null ? HResults.E_NOTIMPL : _inner3.OnGcEvent(gcEvtArgs), gcEvtArgs.type, gcEvtArgs.condemnedGeneration);

    int IXCLRDataExceptionNotification4.ExceptionCatcherEnter(IXCLRDataMethodInstance? catchingMethod, uint catcherNativeOffset)
        => Record(nameof(IXCLRDataExceptionNotification4.ExceptionCatcherEnter),
            _inner4 is null ? HResults.E_NOTIMPL : _inner4.ExceptionCatcherEnter(catchingMethod, catcherNativeOffset),
            catchingMethod, catcherNativeOffset);

    int IXCLRDataExceptionNotification5.OnCodeGenerated2(IXCLRDataMethodInstance? method, ClrDataAddress nativeCodeLocation)
        => Record(nameof(IXCLRDataExceptionNotification5.OnCodeGenerated2),
            _inner5 is null ? HResults.E_NOTIMPL : _inner5.OnCodeGenerated2(method, nativeCodeLocation),
            method, nativeCodeLocation.Value);
}

/// <summary>
/// Sink handed to the legacy DAC. It never calls the caller; the original cDAC had no callback
/// comparison block, so every notification is accepted without recording a validation mismatch.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ReplayingExceptionNotification
    : IXCLRDataExceptionNotification, IXCLRDataExceptionNotification2, IXCLRDataExceptionNotification3,
      IXCLRDataExceptionNotification4, IXCLRDataExceptionNotification5, ICustomQueryInterface
{
    private readonly bool _supportsNotification2;
    private readonly bool _supportsNotification3;
    private readonly bool _supportsNotification4;
    private readonly bool _supportsNotification5;

    internal ReplayingExceptionNotification(
        bool supportsNotification2,
        bool supportsNotification3,
        bool supportsNotification4,
        bool supportsNotification5)
    {
        _supportsNotification2 = supportsNotification2;
        _supportsNotification3 = supportsNotification3;
        _supportsNotification4 = supportsNotification4;
        _supportsNotification5 = supportsNotification5;
    }

    private static int Replay(string method, params object?[] arguments)
    {
        CallbackInvocation? recorded = ShimCall.Current?.NextRecordedCallback();
        Debug.Assert(recorded is not null, $"DAC raised unexpected notification {method}");
        if (recorded is null)
            return HResults.S_OK;

        Debug.Assert(recorded.Method == method, $"cDAC raised {recorded.Method}, DAC raised {method}");
        Debug.Assert(ArgumentsEquivalent(recorded.Arguments, arguments), $"Arguments differed for {method}");
        return recorded.HResult;
    }

    private static bool ArgumentsEquivalent(object?[] expected, object?[] actual)
    {
        if (expected.Length != actual.Length)
            return false;

        for (int i = 0; i < expected.Length; i++)
        {
            object? expectedValue = expected[i];
            object? actualValue = actual[i];
            if (expectedValue is null || actualValue is null)
            {
                if (expectedValue is not null || actualValue is not null)
                    return false;
            }
            else if (expectedValue.GetType().IsValueType || expectedValue is string)
            {
                if (!expectedValue.Equals(actualValue))
                    return false;
            }
        }

        return true;
    }

    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
    {
        ppv = IntPtr.Zero;

        if ((iid == typeof(IXCLRDataExceptionNotification2).GUID && !_supportsNotification2)
            || (iid == typeof(IXCLRDataExceptionNotification3).GUID && !_supportsNotification3)
            || (iid == typeof(IXCLRDataExceptionNotification4).GUID && !_supportsNotification4)
            || (iid == typeof(IXCLRDataExceptionNotification5).GUID && !_supportsNotification5))
        {
            return CustomQueryInterfaceResult.Failed;
        }

        return CustomQueryInterfaceResult.NotHandled;
    }

    int IXCLRDataExceptionNotification.OnCodeGenerated(IXCLRDataMethodInstance? method)
        => Replay(nameof(IXCLRDataExceptionNotification.OnCodeGenerated), method);

    int IXCLRDataExceptionNotification.OnCodeDiscarded(IXCLRDataMethodInstance? method)
        => Replay(nameof(IXCLRDataExceptionNotification.OnCodeDiscarded), method);

    int IXCLRDataExceptionNotification.OnProcessExecution(uint state)
        => Replay(nameof(IXCLRDataExceptionNotification.OnProcessExecution), state);

    int IXCLRDataExceptionNotification.OnTaskExecution(void* task, uint state)
        => Replay(nameof(IXCLRDataExceptionNotification.OnTaskExecution), state);

    int IXCLRDataExceptionNotification.OnModuleLoaded(IXCLRDataModule? mod)
        => Replay(nameof(IXCLRDataExceptionNotification.OnModuleLoaded), mod);

    int IXCLRDataExceptionNotification.OnModuleUnloaded(IXCLRDataModule? mod)
        => Replay(nameof(IXCLRDataExceptionNotification.OnModuleUnloaded), mod);

    int IXCLRDataExceptionNotification.OnTypeLoaded(void* typeInst)
        => Replay(nameof(IXCLRDataExceptionNotification.OnTypeLoaded));

    int IXCLRDataExceptionNotification.OnTypeUnloaded(void* typeInst)
        => Replay(nameof(IXCLRDataExceptionNotification.OnTypeUnloaded));

    int IXCLRDataExceptionNotification2.OnAppDomainLoaded(void* domain)
        => Replay(nameof(IXCLRDataExceptionNotification2.OnAppDomainLoaded));

    int IXCLRDataExceptionNotification2.OnAppDomainUnloaded(void* domain)
        => Replay(nameof(IXCLRDataExceptionNotification2.OnAppDomainUnloaded));

    int IXCLRDataExceptionNotification2.OnException(IXCLRDataExceptionState? exception)
        => Replay(nameof(IXCLRDataExceptionNotification2.OnException), exception);

    int IXCLRDataExceptionNotification3.OnGcEvent(GcEvtArgs gcEvtArgs)
        => Replay(nameof(IXCLRDataExceptionNotification3.OnGcEvent), gcEvtArgs.type, gcEvtArgs.condemnedGeneration);

    int IXCLRDataExceptionNotification4.ExceptionCatcherEnter(IXCLRDataMethodInstance? catchingMethod, uint catcherNativeOffset)
        => Replay(nameof(IXCLRDataExceptionNotification4.ExceptionCatcherEnter), catchingMethod, catcherNativeOffset);

    int IXCLRDataExceptionNotification5.OnCodeGenerated2(IXCLRDataMethodInstance? method, ClrDataAddress nativeCodeLocation)
        => Replay(nameof(IXCLRDataExceptionNotification5.OnCodeGenerated2), method, nativeCodeLocation.Value);
}
