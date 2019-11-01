// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CmsKeyAgreeKeyChoice : int
        {
            CMSG_KEY_AGREE_EPHEMERAL_KEY_CHOICE = 1,
            CMSG_KEY_AGREE_STATIC_KEY_CHOICE = 2,
        }
    }
}
