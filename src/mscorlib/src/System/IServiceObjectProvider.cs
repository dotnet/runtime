// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Runtime.InteropServices;

namespace System
{
    public interface IServiceProvider
    {
        // Interface does not need to be marked with the serializable attribute
        Object GetService(Type serviceType);
    }
}
