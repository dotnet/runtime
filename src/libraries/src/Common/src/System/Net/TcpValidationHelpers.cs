// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
