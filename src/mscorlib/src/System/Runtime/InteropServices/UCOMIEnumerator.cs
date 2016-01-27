// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*==========================================================================
**
** Interface:  UCOMIEnumerator
**
**
** Purpose: 
** This interface is redefined here since the original IEnumerator interface 
** has all its methods marked as ecall's since it is a managed standard 
** interface. This interface is used from within the runtime to make a call 
** on the COM server directly when it implements the IEnumerator interface.
**
** 
==========================================================================*/
namespace System.Runtime.InteropServices
{
    using System;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumerator instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("496B0ABF-CDEE-11d3-88E8-00902754C43A")]
    internal interface UCOMIEnumerator
    {
        bool MoveNext();
        Object Current {
            get; 
        }
        void Reset();
    }
}
