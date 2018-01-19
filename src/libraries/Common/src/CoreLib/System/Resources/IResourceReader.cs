// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

using System;
using System.Collections;

namespace System.Resources
{
    public interface IResourceReader : IEnumerable, IDisposable
    {
        // Interface does not need to be marked with the serializable attribute
        // Closes the ResourceReader, releasing any resources associated with it.
        // This could close a network connection, a file, or do nothing.
        void Close();

        new IDictionaryEnumerator GetEnumerator();
    }
}
