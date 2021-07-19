// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public partial class AppDomain
    {
#if TARGET_ANDROID
        internal void DoUnhandledException (UnhandledExceptionEventArgs args)
        {
            AppContext.DoUnhandledException (this, args);
        }
#endif
    }
}
