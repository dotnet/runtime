// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
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
            safeHandleType)
    { }

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
    private GCHandle _context;

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

internal sealed class MsQuicConfigurationSafeHandle : MsQuicSafeHandle, ISafeHandleCachable
{
    // MsQuicConfiguration handles are cached, so we need to keep track of the
    // number of times a handle is rented. Once we decide to dispose the handle,
    // we set the _rentCount to -1.
    private volatile int _rentCount;

    public unsafe MsQuicConfigurationSafeHandle(QUIC_HANDLE* handle)
        : base(handle, SafeHandleType.Configuration) { }

    public bool TryAddRentCount()
    {
        int oldCount;

        do
        {
            oldCount = _rentCount;
            if (oldCount < 0)
            {
                // The handle is already disposed.
                return false;
            }
        } while (Interlocked.CompareExchange(ref _rentCount, oldCount + 1, oldCount) != oldCount);

        return true;
    }

    public bool TryMarkForDispose()
    {
        return Interlocked.CompareExchange(ref _rentCount, -1, 0) == 0;
    }

    protected override void Dispose(bool disposing)
    {
        if (Interlocked.Decrement(ref _rentCount) < 0)
        {
            // _rentCount is 0 if the handle was never rented (e.g. failure during creation),
            // and is -1 when evicted from cache.
            base.Dispose(disposing);
        }
    }
}
