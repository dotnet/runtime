// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  IEnumerable
** 
** 
**
**
** Purpose: Interface for providing generic IEnumerators
**
** 
===========================================================*/

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;

namespace System.Collections.Generic
{
    // Implement this interface if you need to support foreach semantics.

    // Note that T[] : IList<T>, and we want to ensure that if you use
    // IList<YourValueType>, we ensure a YourValueType[] can be used 
    // without jitting.  Hence the TypeDependencyAttribute on SZArrayHelper.
    // This is a special workaround internally though - see VM\compile.cpp.
    // The same attribute is on IList<T> and ICollection<T>.
    [TypeDependencyAttribute("System.SZArrayHelper")]
    public interface IEnumerable<out T> : IEnumerable
    {
        // Returns an IEnumerator for this enumerable Object.  The enumerator provides
        // a simple way to access all the contents of a collection.
        /// <include file='doc\IEnumerable.uex' path='docs/doc[@for="IEnumerable.GetEnumerator"]/*' />
        new IEnumerator<T> GetEnumerator();
    }
}
