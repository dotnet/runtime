// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** Interface: AsyncCallbackDelegate
**
** Purpose: Type of callback for async operations
**
===========================================================*/
namespace System {
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public delegate void AsyncCallback(IAsyncResult ar);

}
