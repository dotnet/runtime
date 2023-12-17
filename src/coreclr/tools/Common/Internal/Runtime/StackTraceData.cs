// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    internal static class StackTraceDataCommand
    {
        public const byte UpdateOwningType = 0x01;
        public const byte UpdateName = 0x02;
        public const byte UpdateSignature = 0x04;
        public const byte UpdateGenericSignature = 0x08; // Just a shortcut - sig metadata has the info

        public const byte IsStackTraceHidden = 0x10;
    }
}
