// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
    /// <remarks>
    /// Kept internal for now:
    /// A user depending on this strongly-typed header is dubious, as Alt-Svc values can also be received via the ALTSVC frame in HTTP/2.
    /// This type does not conform to the typical API for header values, and should be updated if ever made public.
    /// </remarks>
    internal sealed class AltSvcHeaderValue
    {
        public static AltSvcHeaderValue Clear { get; } = new AltSvcHeaderValue("clear", host: null, port: 0, maxAge: TimeSpan.Zero, persist: false);

        public AltSvcHeaderValue(string alpnProtocolName, string? host, int port, TimeSpan maxAge, bool persist)
        {
            AlpnProtocolName = alpnProtocolName;
            Host = host;
            Port = port;
            MaxAge = maxAge;
            Persist = persist;
        }

        public string AlpnProtocolName { get; }

        /// <summary>
        /// The name of the host serving this alternate service.
        /// If null, the alternate service is on the same host this header was received from.
        /// </summary>
        public string? Host { get; }

        public int Port { get; }

        /// <summary>
        /// The time span this alternate service is valid for.
        /// If not specified by the header, defaults to 24 hours.
        /// </summary>
        /// <remarks>TODO: if made public, should this be defaulted or nullable?</remarks>
        public TimeSpan MaxAge { get; }

        /// <summary>
        /// If true, the service should persist across network changes.
        /// Otherwise, the service should be invalidated if a network change is detected.
        /// </summary>
        /// <remarks>TODO: if made public, this should be made internal as Persist is left open-ended and can be non-boolean in the future.</remarks>
        public bool Persist { get; }

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire(capacity: AlpnProtocolName.Length + (Host?.Length ?? 0) + 64);

            sb.Append(AlpnProtocolName);
            sb.Append("=\"");
            if (Host != null) sb.Append(Host);
            sb.Append(':');
            sb.Append(Port.ToString(CultureInfo.InvariantCulture));
            sb.Append('"');

            if (MaxAge != TimeSpan.FromTicks(AltSvcHeaderParser.DefaultMaxAgeTicks))
            {
                sb.Append("; ma=");
                sb.Append((MaxAge.Ticks / TimeSpan.TicksPerSecond).ToString(CultureInfo.InvariantCulture));
            }

            if (Persist)
            {
                sb.Append("; persist=1");
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}
