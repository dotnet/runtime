// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class RemoteBase_InSeparateAssembly
{
    protected interface IProtected_InSeparateAssembly { string Touch(); }
    protected static string UseIProtected(IProtected_InSeparateAssembly intrf) { return intrf.Touch(); }
}
