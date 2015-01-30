// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//

using System;
using System.Runtime.InteropServices;

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
