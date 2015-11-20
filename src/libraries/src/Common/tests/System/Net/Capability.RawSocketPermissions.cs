// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Test.Common
{
    public static partial class Capability
    {
        public static bool CanUseRawSockets()
        {
            return RawSocketPermissions.CanUseRawSockets();
        }
    }
}
