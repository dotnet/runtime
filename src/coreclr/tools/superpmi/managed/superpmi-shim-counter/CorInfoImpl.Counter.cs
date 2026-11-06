// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SuperPmi.Shim;

namespace Internal.JitInterface;

internal sealed partial class CorInfoImpl
{
    partial void OnCall(string name) => Counter.AddCall(name);
}
