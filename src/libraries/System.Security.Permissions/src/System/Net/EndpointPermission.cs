// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    public class EndpointPermission
    {
        internal EndpointPermission() { }
        public string Hostname { get { return null; } }
        public int Port { get { return 0; } }
        public TransportType Transport { get; }
        public override bool Equals(object obj) { return false; }
        public override int GetHashCode() { return 0; }

    }
}

