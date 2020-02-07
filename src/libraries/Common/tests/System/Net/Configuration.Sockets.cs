// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Sockets
        {
           public static Uri SocketServer => GetUriValue("COREFX_NET_SOCKETS_SERVERURI", new Uri("http://" + DefaultAzureServer));

           public static string InvalidHost => GetValue("COREFX_NET_SOCKETS_INVALIDSERVER", "notahostname.invalid.corp.microsoft.com");

           private static Lazy<(int Min, int Max)> s_portPoolRangeLazy = new Lazy<(int Min, int Max)>(() =>
           {
               string configString = GetValue("COREFX_NET_SOCKETS_PORTPOOLRANGE", "17000 22000");
               string[] portRange = configString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
               int minPort = int.Parse(portRange[0].Trim());
               int maxPort = int.Parse(portRange[1].Trim());
               return (minPort, maxPort);
           }, LazyThreadSafetyMode.PublicationOnly);

           /// <summary>
           /// Min: inclusive, Max: exclusive.
           /// </summary>
           public static (int Min, int Max) TestPoolPortRange => s_portPoolRangeLazy.Value;
        }
    }
}
