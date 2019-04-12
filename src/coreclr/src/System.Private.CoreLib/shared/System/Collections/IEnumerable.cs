// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;

namespace System.Collections
{
#if !PROJECTN // Hitting a bug in MCG, see VSO:743654
    [Guid("496B0ABE-CDEE-11d3-88E8-00902754C43A")]
    [ComVisible(true)]
#endif
    public interface IEnumerable
    {
        // Returns an IEnumerator for this enumerable Object.  The enumerator provides
        // a simple way to access all the contents of a collection.
        IEnumerator GetEnumerator();
    }
}
