// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static partial class ThrowHelper
{
    internal static TypeLoadException GetTypeLoadException(string assemblyName, string className)
    {
        return new TypeLoadException(SR.Format(SR.TypeNotFound, assemblyName, className));
    }
}
