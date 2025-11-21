// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const int WAIT_TIMEOUT = 0x00000102;
        internal const int WAIT_OBJECT_0 = 0x00000000;
        internal const int WAIT_ABANDONED = 0x00000080;

        internal const int MAXIMUM_ALLOWED = 0x02000000;
        internal const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        internal const int READ_CONTROL = 0x00020000;
        internal const int WRITE_DAC = 0x00040000;
        internal const int WRITE_OWNER = 0x00080000;
        internal const int SYNCHRONIZE = 0x00100000;
        internal const int MUTEX_MODIFY_STATE = 0x00000001;
        internal const int SEMAPHORE_MODIFY_STATE = 0x00000002;
        internal const int EVENT_MODIFY_STATE = 0x00000002;
    }
}
