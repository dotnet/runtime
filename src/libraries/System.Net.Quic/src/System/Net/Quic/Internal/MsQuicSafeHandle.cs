// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private SafeHandleType _type;

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

    protected override bool ReleaseHandle()
    {
        _releaseAction(QuicHandle);
        SetHandle(IntPtr.Zero);

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

    public unsafe MsQuicContextSafeHandle(QUIC_HANDLE* handle, GCHandle context, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> releaseAction, SafeHandleType safeHandleType, MsQuicSafeHandle? parent = null)
        : base(handle, releaseAction, safeHandleType)
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

    protected override bool ReleaseHandle()
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
        return true;
    }
}
