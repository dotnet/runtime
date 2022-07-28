// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal sealed class IpcTcpSocketEndPoint
    {
        public bool DualMode { get; }
        public IPEndPoint EndPoint { get; }

        public static bool IsTcpIpEndPoint(string endPoint)
        {
            bool result = true;

            try
            {
                ParseTcpIpEndPoint(endPoint, out _, out _);
            }
            catch(Exception)
            {
                result = false;
            }

            return result;
        }

        public static string NormalizeTcpIpEndPoint(string endPoint)
        {
            ParseTcpIpEndPoint(endPoint, out string host, out int port);
            return string.Format("{0}:{1}", host, port);
        }

        public IpcTcpSocketEndPoint(string endPoint)
        {
            ParseTcpIpEndPoint(endPoint, out string host, out int port);
            EndPoint = CreateEndPoint(host, port);
            DualMode = string.CompareOrdinal(host, "*") == 0;
        }

        public static implicit operator EndPoint(IpcTcpSocketEndPoint endPoint) => endPoint.EndPoint;

        private static void ParseTcpIpEndPoint(string endPoint, out string host, out int port)
        {
            host = "";
            port = -1;

            bool usesWildcardHost = false;
            string uriToParse= "";

            if (endPoint.Contains("://"))
            {
                // Host can contain wildcard (*) that is a reserved charachter in URI's.
                // Replace with dummy localhost representation just for parsing purpose.
                if (endPoint.IndexOf("//*", StringComparison.Ordinal) != -1)
                {
                    usesWildcardHost = true;
                    uriToParse = endPoint.Replace("//*", "//localhost");
                }
                else
                {
                    uriToParse = endPoint;
                }
            }

            try
            {
                if (!string.IsNullOrEmpty(uriToParse) && Uri.TryCreate(uriToParse, UriKind.RelativeOrAbsolute, out Uri uri))
                {
                    if (string.Compare(uri.Scheme, Uri.UriSchemeNetTcp, StringComparison.OrdinalIgnoreCase) != 0 &&
                        string.Compare(uri.Scheme, "tcp", StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        throw new ArgumentException(string.Format("Unsupported Uri schema, \"{0}\"", uri.Scheme));
                    }

                    host = usesWildcardHost ? "*" : uri.Host;
                    port = uri.IsDefaultPort ? 0 : uri.Port;
                }
            }
            catch (InvalidOperationException)
            {
            }

            if (string.IsNullOrEmpty(host) || port == -1)
            {
                string[] segments = endPoint.Split(':');
                if (segments.Length > 2)
                {
                    host = string.Join(":", segments, 0, segments.Length - 1);
                    port = int.Parse(segments[segments.Length - 1]);
                }
                else if (segments.Length == 2)
                {
                    host = segments[0];
                    port = int.Parse(segments[1]);
                }

                if (string.CompareOrdinal(host, "*") != 0)
                {
                    if (!IPAddress.TryParse(host, out _))
                    {
                        if (!Uri.TryCreate(Uri.UriSchemeNetTcp + "://" + host + ":" + port, UriKind.RelativeOrAbsolute, out _))
                        {
                            host = "";
                            port = -1;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(host) || port == -1)
            {
                throw new ArgumentException(string.Format("Could not parse {0} into host, port", endPoint));
            }
        }

        private static IPEndPoint CreateEndPoint(string host, int port)
        {
            IPAddress ipAddress = null;
            try
            {
                if (string.CompareOrdinal(host, "*") == 0)
                {
                    if (Socket.OSSupportsIPv6)
                    {
                        ipAddress = IPAddress.IPv6Any;
                    }
                    else
                    {
                        ipAddress = IPAddress.Any;
                    }
                }
                else if (!IPAddress.TryParse(host, out ipAddress))
                {
                    var hostEntry = Dns.GetHostEntry(host);
                    if (hostEntry.AddressList.Length > 0)
                        ipAddress = hostEntry.AddressList[0];
                }
            }
            catch(Exception)
            {
            }

            if (ipAddress == null)
                throw new ArgumentException(string.Format("Could not resolve {0} into an IP address", host));

            return new IPEndPoint(ipAddress, port);
        }
    }
}
