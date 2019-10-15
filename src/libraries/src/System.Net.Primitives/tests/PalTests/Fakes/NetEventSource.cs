// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    public static class NetEventSource
    {
        public static void Enter(object thisOrContextObject, object arg1 = null, object arg2 = null, object arg3 = null) { }
        public static void Fail(object thisOrContextObject, object arg) { }
        public static void Info(object thisOrContextObject, object arg) { }
        public static bool IsEnabled => false;
    }
}
