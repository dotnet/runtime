// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal static class TcpValidationHelpers
    {
        public static bool ValidatePortNumber(int port)
        {
            // When this method returns false, the caller should throw
            // 'new ArgumentOutOfRangeException("port")'
            return port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort;
        }
    }
}
