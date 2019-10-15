// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    // TODO #3562: Unify ErrorCodes.
    internal static partial class HttpApi
    {
        public const uint ERROR_SUCCESS = 0;
        public const uint ERROR_HANDLE_EOF = 38;
        public const uint ERROR_INVALID_PARAMETER = 87;
        public const uint ERROR_ALREADY_EXISTS = 183;
        public const uint ERROR_MORE_DATA = 234;
        public const uint ERROR_IO_PENDING = 997;
        public const uint ERROR_NOT_FOUND = 1168;
        public const uint ERROR_CONNECTION_INVALID = 1229;
    }
}
