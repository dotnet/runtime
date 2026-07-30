// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// The kind of target mutation the production cDAC performed while servicing a call.
/// </summary>
internal enum MutationKind
{
    WriteVirtual,
    SetThreadContext,
    AllocVirtual,
    FreeVirtual,
    SetTLSValue,
}

/// <summary>
/// A single mutation the production cDAC performed against the real data target, captured so the
/// legacy DAC's mutation target can replay it instead of performing it a second time.
/// </summary>
internal sealed class Mutation
{
    internal MutationKind Kind { get; init; }
    internal ulong Address { get; init; }
    internal uint ThreadId { get; init; }
    internal uint Index { get; init; }
    internal ulong Value { get; init; }
    internal byte[]? Data { get; init; }
    internal uint Count { get; init; }
    internal int HResult { get; init; }
}

/// <summary>
/// A caller-supplied callback invocation captured while the production cDAC ran, so the legacy DAC's
/// equivalent invocation can be compared against it without invoking the caller a second time.
/// </summary>
internal sealed class CallbackInvocation
{
    internal string Method { get; init; } = string.Empty;
    internal object?[] Arguments { get; init; } = [];
    internal int HResult { get; init; }
}

/// <summary>
/// State scoped to one proxied call. Nested (reentrant) calls get their own state and restore the
/// enclosing one on exit, so a caller callback that re-enters the shim cannot disturb the record and
/// replay streams of the call that is already in flight.
/// </summary>
internal sealed class CallState
{
    internal CallState(CallState? parent) => Parent = parent;

    internal CallState? Parent { get; }

    private List<Mutation>? _mutations;
    private int _replayIndex;

    private List<CallbackInvocation>? _callbacks;
    private int _callbackReplayIndex;

    internal void Record(Mutation mutation) => (_mutations ??= []).Add(mutation);

    /// <summary>Consumes the next recorded mutation, or <c>null</c> when the cDAC recorded none.</summary>
    internal Mutation? NextRecordedMutation()
    {
        if (_mutations is null || _replayIndex >= _mutations.Count)
            return null;

        return _mutations[_replayIndex++];
    }

    internal void RecordCallback(CallbackInvocation invocation) => (_callbacks ??= []).Add(invocation);

    internal CallbackInvocation? NextRecordedCallback()
    {
        if (_callbacks is null || _callbackReplayIndex >= _callbacks.Count)
            return null;

        return _callbacks[_callbackReplayIndex++];
    }

    internal int RecordedCallbackCount => _callbacks?.Count ?? 0;

    internal int ReplayedCallbackCount => _callbackReplayIndex;
}

/// <summary>
/// Scope object pushed for the duration of one proxied call. Use with a <c>using</c> declaration at
/// the top of a proxy method.
/// </summary>
internal readonly struct ShimCall : IDisposable
{
    [ThreadStatic]
    private static CallState? t_current;

    private readonly CallState _state;

    private ShimCall(CallState state) => _state = state;

    /// <summary>The call currently in flight on this thread, if any.</summary>
    internal static CallState? Current => t_current;

    internal static ShimCall Enter()
    {
        CallState state = new(t_current);
        t_current = state;
        return new ShimCall(state);
    }

    public void Dispose() => t_current = _state.Parent;
}

/// <summary>
/// Pairs a production cDAC enumeration handle with the legacy DAC handle produced by the same
/// operation. Handles are opaque <c>ulong</c> tokens in the IXCLRData contracts; the shim hands the
/// caller its own token and keeps the paired state alive across the Start/Enum/End sequence.
/// </summary>
internal sealed class PairedHandle
{
    internal ulong CDacHandle;
    internal ulong DacHandle;
    internal bool HasDacHandle;
}

/// <summary>
/// Per-instance state shared by every proxy created from one <c>CLRDataCreateInstance</c> (or
/// equivalent) call: the object pairing cache and the enumeration handle registry.
/// </summary>
internal sealed class ValidationSession
{
    private readonly ConditionalWeakTable<object, object> _proxiesByCDacObject = new();
    private readonly Dictionary<ulong, PairedHandle> _handles = [];
    private readonly object _handleLock = new();
    private ulong _nextHandle = 1;

    /// <summary>
    /// Returns the shim proxy paired with <paramref name="key"/>, creating it via
    /// <paramref name="factory"/> on first use. Reusing one proxy per production object keeps
    /// reference identity stable for callers that compare interface pointers.
    /// </summary>
    internal T GetOrCreateProxy<T>(object key, object? cdacObject, object? dacObject, Func<ValidationSession, object?, object?, T> factory)
        where T : class
    {
        if (_proxiesByCDacObject.TryGetValue(key, out object? existing))
            return (T)existing;

        T created = factory(this, cdacObject, dacObject);
        return (T)_proxiesByCDacObject.GetValue(key, _ => created);
    }

    /// <summary>Registers a cDAC/DAC handle pair and returns the token handed to the caller.</summary>
    internal ulong RegisterHandle(ulong cdacHandle, ulong dacHandle, bool hasDacHandle)
    {
        lock (_handleLock)
        {
            ulong token = _nextHandle++;
            _handles[token] = new PairedHandle
            {
                CDacHandle = cdacHandle,
                DacHandle = dacHandle,
                HasDacHandle = hasDacHandle,
            };
            return token;
        }
    }

    internal PairedHandle? LookupHandle(ulong token)
    {
        lock (_handleLock)
        {
            return _handles.TryGetValue(token, out PairedHandle? handle) ? handle : null;
        }
    }

    internal PairedHandle? ReleaseHandle(ulong token)
    {
        lock (_handleLock)
        {
            if (!_handles.Remove(token, out PairedHandle? handle))
                return null;

            return handle;
        }
    }
}
