// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static partial class AppContext
    {
        internal static void DoUnhandledException (object sender, UnhandledExceptionEventArgs args)
        {
            if (UnhandledException != null)
                UnhandledException (sender, args);
        }
    }
}
