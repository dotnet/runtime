// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: Notes a class which knows how to return formatting information
**
**
============================================================*/
namespace System {
    
    using System;

    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IFormatProvider
    {
        // Interface does not need to be marked with the serializable attribute
        Object GetFormat(Type formatType);
    }
}
