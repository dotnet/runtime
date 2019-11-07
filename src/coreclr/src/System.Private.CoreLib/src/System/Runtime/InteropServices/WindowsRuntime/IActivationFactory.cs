// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    [ComImport]
    [Guid("00000035-0000-0000-C000-000000000046")]
    [WindowsRuntimeImport]
    public interface IActivationFactory
    {
        object ActivateInstance();
    }
}
