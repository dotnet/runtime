// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

public static class WinRTNativeComponent
{
    [DllImport(nameof(WinRTNativeComponent), PreserveSig = false)]
    private static extern IActivationFactory DllGetActivationFactory([MarshalAs(UnmanagedType.HString)] string typeName);

    public static object GetObjectFromNativeComponent(string typeName)
    {
        return DllGetActivationFactory(typeName).ActivateInstance();
    }
}
