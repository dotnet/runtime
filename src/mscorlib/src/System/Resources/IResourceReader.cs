// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
** 
** 
**
**
** Purpose: Abstraction to read streams of resources.
**
** 
===========================================================*/
namespace System.Resources {    
    using System;
    using System.IO;
    using System.Collections;
    
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface IResourceReader : IEnumerable, IDisposable
    {
        // Interface does not need to be marked with the serializable attribute
        // Closes the ResourceReader, releasing any resources associated with it.
        // This could close a network connection, a file, or do nothing.
        void Close();


        new IDictionaryEnumerator GetEnumerator();
    }
}
