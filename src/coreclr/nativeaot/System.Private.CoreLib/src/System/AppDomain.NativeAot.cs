// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

public sealed partial class AppDomain
{
    static partial void SetFirstChanceExceptionHandler() =>
        AppContext.SetFirstChanceExceptionHandler();

    static partial void SetUnhandledExceptionHandler() =>
        AppContext.SetUnhandledExceptionHandler();
}
