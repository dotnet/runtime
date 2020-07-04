// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NativeCallManagedComVisible
{
    public static class Helpers
    {
        public static int S_OK = 0;
        public static int E_NOINTERFACE = unchecked((int)0x80004002);
        public static int COR_E_INVALIDOPERATION = unchecked((int)0x80131509);
        public static int COR_E_GENERICMETHOD = unchecked((int)0x80131535);
    }
}
