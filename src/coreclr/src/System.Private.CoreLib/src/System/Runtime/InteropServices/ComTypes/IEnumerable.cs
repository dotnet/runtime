// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

namespace System.Runtime.InteropServices.ComTypes
{
    /*==========================================================================
    ** Interface: IEnumerable
    ** Purpose:
    ** This interface is redefined here since the original IEnumerable interface
    ** has all its methods marked as ecall's since it is a managed standard
    ** interface. This interface is used from within the runtime to make a call
    ** on the COM server directly when it implements the IEnumerable interface.
    ==========================================================================*/
    [Guid("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
    internal interface IEnumerable
    {
        [DispId(-4)]
        System.Collections.IEnumerator GetEnumerator();
    }
}
