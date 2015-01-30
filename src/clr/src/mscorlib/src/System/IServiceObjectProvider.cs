// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System {
    
    using System;
    using System.Runtime.InteropServices;


    public interface IServiceProvider
    {
        // Interface does not need to be marked with the serializable attribute
        Object GetService(Type serviceType);
    }
}
