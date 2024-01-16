// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    public class JSProxyContextBase
    {
        [ThreadStatic]
        public static JSProxyContextBase? CurrentThreadContextBase;
    }
}
