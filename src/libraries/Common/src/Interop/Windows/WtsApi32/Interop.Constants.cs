// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Wtsapi32
    {
        public const int NOTIFY_FOR_THIS_SESSION          =  0x0;

        public const int WTS_CONSOLE_CONNECT              =  0x1;
        public const int WTS_CONSOLE_DISCONNECT           =  0x2;
        public const int WTS_REMOTE_CONNECT               =  0x3;
        public const int WTS_REMOTE_DISCONNECT            =  0x4;
        public const int WTS_SESSION_LOGON                =  0x5;
        public const int WTS_SESSION_LOGOFF               =  0x6;
        public const int WTS_SESSION_LOCK                 =  0x7;
        public const int WTS_SESSION_UNLOCK               =  0x8;
        public const int WTS_SESSION_REMOTE_CONTROL       =  0x9;
    }
}
