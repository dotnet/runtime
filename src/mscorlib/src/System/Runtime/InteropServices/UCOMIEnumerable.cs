// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*==========================================================================
**
** Interface:  UCOMIEnumerable
**
**
** Purpose: 
** This interface is redefined here since the original IEnumerable interface 
** has all its methods marked as ecall's since it is a managed standard 
** interface. This interface is used from within the runtime to make a call 
** on the COM server directly when it implements the IEnumerable interface.
**
** 
==========================================================================*/
namespace System.Runtime.InteropServices
{    
    using System;
    using System.Collections;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumerable instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
    internal interface UCOMIEnumerable
    {
        [DispId(-4)]
        IEnumerator GetEnumerator();
    }
}
