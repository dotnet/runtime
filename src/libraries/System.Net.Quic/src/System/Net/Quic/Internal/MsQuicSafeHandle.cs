// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Quic;

namespace System.Net.Quic;

internal unsafe class MsQuicSafeHandle : SafeHandle
{
    // The index must correspond to SafeHandleType enum value and the value must correspond to MsQuic logging abbreviation string.
    // This is used for our logging that uses the same format of object identification as MsQuic to easily correlate log events.
    private static readonly string[] s_typeName = new string[]
    {
        " reg",
        "cnfg",
        "list",
        "conn",
        "strm"
    };

    private readonly delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> _releaseAction;
    private string? _traceId;
    private readonly SafeHandleType _type;

    public override bool IsInvalid => handle == IntPtr.Zero;

    public QUIC_HANDLE* QuicHandle => (QUIC_HANDLE*)DangerousGetHandle();

    public MsQuicSafeHandle(QUIC_HANDLE* handle, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> releaseAction, SafeHandleType safeHandleType)
        : base((IntPtr)handle, ownsHandle: true)
    {
        _releaseAction = releaseAction;
        _type = safeHandleType;

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(this, $"{this} MsQuicSafeHandle created");
        }
    }

    public MsQuicSafeHandle(QUIC_HANDLE* handle, SafeHandleType safeHandleType)
        : this(
            handle,
            safeHandleType switch
            {
                SafeHandleType.Registration => MsQuicApi.Api.ApiTable->RegistrationClose,
                SafeHandleType.Configuration => MsQuicApi.Api.ApiTable->ConfigurationClose,
                SafeHandleType.Listener => MsQuicApi.Api.ApiTable->ListenerClose,
                SafeHandleType.Connection => MsQuicApi.Api.ApiTable->ConnectionClose,
                SafeHandleType.Stream => MsQuicApi.Api.ApiTable->StreamClose,
                _ => throw new ArgumentException($"Unexpected value: {safeHandleType}", nameof(safeHandleType))
            },
            safeHandleType) { }

    protected override bool ReleaseHandle()
    {
        QUIC_HANDLE* quicHandle = QuicHandle;
        SetHandle(IntPtr.Zero);
        _releaseAction(quicHandle);

        if (NetEventSource.Log.IsEnabled())
        {
            NetEventSource.Info(this, $"{this} MsQuicSafeHandle released");
        }

        return true;
    }

    public override string ToString() => _traceId ??= $"[{s_typeName[(int)_type]}][0x{DangerousGetHandle():X11}]";
}

internal enum SafeHandleType
{
    Registration,
    Configuration,
    Listener,
    Connection,
    Stream
}

internal sealed class MsQuicContextSafeHandle : MsQuicSafeHandle
{
    /// <summary>
    /// Holds a weak reference to the managed instance.
    /// It allows delegating MsQuic events to the managed object while it still can be collected and finalized.
    /// </summary>
    private readonly GCHandle _context;

    /// <summary>
    /// Optional parent safe handle, used to increment/decrement reference count with the lifetime of this instance.
    /// </summary>
    private readonly MsQuicSafeHandle? _parent;

    /// <summary>
    /// Additional, dependent object to be disposed only after the safe handle gets released.
    /// </summary>
    private IDisposable? _disposable;

    public IDisposable Disposable
    {
        set
        {
            Debug.Assert(_disposable is null);
            _disposable = value;
        }
    }

    public unsafe MsQuicContextSafeHandle(QUIC_HANDLE* handle, GCHandle context, SafeHandleType safeHandleType, MsQuicSafeHandle? parent = null)
        : base(handle, safeHandleType)
    {
        _context = context;
        if (parent is not null)
        {
            bool success = false;
            parent.DangerousAddRef(ref success);
            _parent = parent;
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"{this} {_parent} ref count incremented");
            }
        }
    }

    protected override unsafe bool ReleaseHandle()
    {
        base.ReleaseHandle();
        if (_context.IsAllocated)
        {
            _context.Free();
        }
        if (_parent is not null)
        {
            _parent.DangerousRelease();
            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"{this} {_parent} ref count decremented");
            }
        }
        _disposable?.Dispose();
        return true;
    }
}
