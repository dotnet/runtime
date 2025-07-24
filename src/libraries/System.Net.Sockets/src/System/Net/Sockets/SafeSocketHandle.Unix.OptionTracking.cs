// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Sockets
{
    // On Unix it is not possible to use a socket FD after a failed connect attempt for another connect, meaning that a new FD needs to be created
    // after each failing attempt during a multi-connect. When creating a new handle, we need to make sure that relevant socket options are
    // transferred to the new socket FD and we need to track which options have been changed so we know we need to transfer them.
    // We are only tracking options which are relevant for sockets that can do multi-connect. Options which are only relevant for UDP/datagram sockets
    // are not tracked, since multi-connect is not a meaningful operation for such sockets.
    public partial class SafeSocketHandle
    {
        private int _trackedOptions;

        internal void TrackSocketOption(SocketOptionLevel level, SocketOptionName name)
        {
            TrackableSocketOptions tracked = ToTrackableSocketOptions(name, level);

            // For untracked socket options, we need to remember that they were used
            // so that we can error out if a multi-connect attempt is made.
            if (tracked == TrackableSocketOptions.None)
            {
                ExposedHandleOrUntrackedConfiguration = true;
                return;
            }

            _trackedOptions |= GetFlag(tracked);
        }

        internal void GetTrackedSocketOptions(Span<int> values, out LingerOption? lingerOption)
        {
            Debug.Assert(values.Length == TrackableOptionCount);
            int trackedOptions = _trackedOptions;

            // SO_LINGER is the only tracked socket option with a non-int value.
            lingerOption = null;
            int lingerFlag = GetFlag(TrackableSocketOptions.SO_LINGER);
            if ((trackedOptions & lingerFlag) == lingerFlag)
            {
                SocketError errorCode = SocketPal.GetLingerOption(this, out lingerOption);
                if (errorCode != SocketError.Success)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetLingerOption returned errorCode:{errorCode}");

                    // Untrack this option, so we don't try to set it.
                    _trackedOptions &= ~lingerFlag;
                }

                // Ignore it during the processing of int-value options.
                trackedOptions &= ~lingerFlag;
            }

            // For DualMode, we use the value stored in the handle rather than querying the socket itself,
            // as on Unix stacks binding a dual-mode socket to an IPv6 address may cause IPV6_V6ONLY to revert to true.
            int ipv6OnlyFlag = GetFlag(TrackableSocketOptions.IPV6_V6ONLY);
            if ((trackedOptions & ipv6OnlyFlag) == ipv6OnlyFlag)
            {
                values[(int)TrackableSocketOptions.IPV6_V6ONLY - 1] = DualMode ? 0 : 1;
                trackedOptions &= ~ipv6OnlyFlag;
            }

            for (int i = 0; i < values.Length; i++)
            {
                int flag = 1 << i;
                if ((trackedOptions & flag) == flag)
                {
                    TrackableSocketOptions tracked = (TrackableSocketOptions)(i + 1);
                    (SocketOptionName name, SocketOptionLevel level) = ToSocketOptions(tracked);
                    SocketError errorCode = SocketPal.GetSockOpt(this, level, name, out values[i]);
                    if (errorCode != SocketError.Success)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetSockOpt({level},{name}) returned errorCode:{errorCode}");

                        // Untrack this option, so we don't try to set it.
                        _trackedOptions &= ~flag;
                    }
                }
            }
        }

        internal void SetTrackedSocketOptions(ReadOnlySpan<int> values, LingerOption? lingerOption)
        {
            Debug.Assert(values.Length == TrackableOptionCount);
            int lingerFlag = GetFlag(TrackableSocketOptions.SO_LINGER);
            if (lingerOption is not null)
            {
                Debug.Assert((_trackedOptions & lingerFlag) == lingerFlag);
                SocketError errorCode = SocketPal.SetLingerOption(this, lingerOption);
                if (NetEventSource.Log.IsEnabled() && errorCode != SocketError.Success) NetEventSource.Info(this, $"SetLingerOption returned errorCode:{errorCode}");
            }

            int trackedOptions = _trackedOptions & ~lingerFlag;

            for (int i = 0; i < values.Length; i++)
            {
                int mask = 1 << i;
                if ((trackedOptions & mask) == mask)
                {
                    TrackableSocketOptions tracked = (TrackableSocketOptions)(i + 1);
                    (SocketOptionName name, SocketOptionLevel level) = ToSocketOptions(tracked);
                    SocketError errorCode = SocketPal.SetSockOpt(this, level, name, values[i]);
                    if (NetEventSource.Log.IsEnabled() && errorCode != SocketError.Success) NetEventSource.Info(this, $"GetSockOpt({level},{name}) returned errorCode:{errorCode}");
                }
            }
        }

        private static int GetFlag(TrackableSocketOptions tracked) => 1 << ((int)tracked - 1);

        // The code below is auto-generated based on option names and values defined in Windows headers:
        // https://gist.github.com/antonfirsov/2cbfc37e665ad840ed7734994948c29a
        private enum TrackableSocketOptions
        {
            None = 0,
            IP_TOS,
            IP_TTL,
            IPV6_PROTECTION_LEVEL,
            IPV6_V6ONLY,
            TCP_NODELAY,
            TCP_EXPEDITED_1122,
            TCP_KEEPALIVE,
            TCP_FASTOPEN,
            TCP_KEEPCNT,
            TCP_KEEPINTVL,
            SO_DEBUG,
            SO_ACCEPTCONN,
            SO_REUSEADDR,
            SO_KEEPALIVE,
            SO_DONTROUTE,
            SO_USELOOPBACK,
            SO_LINGER,
            SO_OOBINLINE,
            SO_DONTLINGER,
            SO_EXCLUSIVEADDRUSE,
            SO_SNDBUF,
            SO_RCVBUF,
            SO_SNDLOWAT,
            SO_RCVLOWAT,
            SO_SNDTIMEO,
            SO_RCVTIMEO
        }

        internal const int TrackableOptionCount = (int)TrackableSocketOptions.SO_RCVTIMEO;

        private static TrackableSocketOptions ToTrackableSocketOptions(SocketOptionName name, SocketOptionLevel level)
            => ((int)name, level) switch
            {
                (3, SocketOptionLevel.IP) => TrackableSocketOptions.IP_TOS,
                (4, SocketOptionLevel.IP) => TrackableSocketOptions.IP_TTL,
                (23, SocketOptionLevel.IPv6) => TrackableSocketOptions.IPV6_PROTECTION_LEVEL,
                (27, SocketOptionLevel.IPv6) => TrackableSocketOptions.IPV6_V6ONLY,
                (1, SocketOptionLevel.Tcp) => TrackableSocketOptions.TCP_NODELAY,
                (2, SocketOptionLevel.Tcp) => TrackableSocketOptions.TCP_EXPEDITED_1122,
                (3, SocketOptionLevel.Tcp) => TrackableSocketOptions.TCP_KEEPALIVE,
                (15, SocketOptionLevel.Tcp) => TrackableSocketOptions.TCP_FASTOPEN,
                (16, SocketOptionLevel.Tcp) => TrackableSocketOptions.TCP_KEEPCNT,
                (17, SocketOptionLevel.Tcp) => TrackableSocketOptions.TCP_KEEPINTVL,
                (1, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_DEBUG,
                (2, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_ACCEPTCONN,
                (4, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_REUSEADDR,
                (8, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_KEEPALIVE,
                (16, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_DONTROUTE,
                (64, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_USELOOPBACK,
                (128, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_LINGER,
                (256, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_OOBINLINE,
                (-129, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_DONTLINGER,
                (-5, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_EXCLUSIVEADDRUSE,
                (4097, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_SNDBUF,
                (4098, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_RCVBUF,
                (4099, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_SNDLOWAT,
                (4100, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_RCVLOWAT,
                (4101, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_SNDTIMEO,
                (4102, SocketOptionLevel.Socket) => TrackableSocketOptions.SO_RCVTIMEO,

                _ => TrackableSocketOptions.None
            };

        private static (SocketOptionName, SocketOptionLevel) ToSocketOptions(TrackableSocketOptions options) =>
            options switch
            {
                TrackableSocketOptions.IP_TOS => ((SocketOptionName)3, SocketOptionLevel.IP),
                TrackableSocketOptions.IP_TTL => ((SocketOptionName)4, SocketOptionLevel.IP),
                TrackableSocketOptions.IPV6_PROTECTION_LEVEL => ((SocketOptionName)23, SocketOptionLevel.IPv6),
                TrackableSocketOptions.IPV6_V6ONLY => ((SocketOptionName)27, SocketOptionLevel.IPv6),
                TrackableSocketOptions.TCP_NODELAY => ((SocketOptionName)1, SocketOptionLevel.Tcp),
                TrackableSocketOptions.TCP_EXPEDITED_1122 => ((SocketOptionName)2, SocketOptionLevel.Tcp),
                TrackableSocketOptions.TCP_KEEPALIVE => ((SocketOptionName)3, SocketOptionLevel.Tcp),
                TrackableSocketOptions.TCP_FASTOPEN => ((SocketOptionName)15, SocketOptionLevel.Tcp),
                TrackableSocketOptions.TCP_KEEPCNT => ((SocketOptionName)16, SocketOptionLevel.Tcp),
                TrackableSocketOptions.TCP_KEEPINTVL => ((SocketOptionName)17, SocketOptionLevel.Tcp),
                TrackableSocketOptions.SO_DEBUG => ((SocketOptionName)1, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_ACCEPTCONN => ((SocketOptionName)2, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_REUSEADDR => ((SocketOptionName)4, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_KEEPALIVE => ((SocketOptionName)8, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_DONTROUTE => ((SocketOptionName)16, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_USELOOPBACK => ((SocketOptionName)64, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_LINGER => ((SocketOptionName)128, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_OOBINLINE => ((SocketOptionName)256, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_DONTLINGER => ((SocketOptionName)(-129), SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_EXCLUSIVEADDRUSE => ((SocketOptionName)(-5), SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_SNDBUF => ((SocketOptionName)4097, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_RCVBUF => ((SocketOptionName)4098, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_SNDLOWAT => ((SocketOptionName)4099, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_RCVLOWAT => ((SocketOptionName)4100, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_SNDTIMEO => ((SocketOptionName)4101, SocketOptionLevel.Socket),
                TrackableSocketOptions.SO_RCVTIMEO => ((SocketOptionName)4102, SocketOptionLevel.Socket),

                _ => default
            };
    }
}
