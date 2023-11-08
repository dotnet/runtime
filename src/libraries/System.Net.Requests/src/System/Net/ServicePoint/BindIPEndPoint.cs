// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Net.ServicePoint, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51")]
    public delegate IPEndPoint BindIPEndPoint(ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount);
}
