// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    // TODO #3562: Unify ErrorCodes.
    internal static partial class IpHlpApi
    {
        public const uint ERROR_SUCCESS = 0;
        public const uint ERROR_INVALID_FUNCTION = 1;
        public const uint ERROR_NO_SUCH_DEVICE = 2;
        public const uint ERROR_INVALID_DATA = 13;
        public const uint ERROR_INVALID_PARAMETER = 87;
        public const uint ERROR_BUFFER_OVERFLOW = 111;
        public const uint ERROR_INSUFFICIENT_BUFFER = 122;
        public const uint ERROR_NO_DATA = 232;
        public const uint ERROR_IO_PENDING = 997;
        public const uint ERROR_NOT_FOUND = 1168;
    }
}
