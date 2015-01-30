// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
namespace System.Runtime.InteropServices {

    using System;

[System.Runtime.InteropServices.ComVisible(true)]
    public interface ICustomFactory
    {
        MarshalByRefObject CreateInstance(Type serverType);
    }

}
