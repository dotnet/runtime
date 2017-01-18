// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;

namespace System.Configuration
{
    internal class ReadOnlyNameValueCollection : NameValueCollection
    {

        internal ReadOnlyNameValueCollection(IEqualityComparer equalityComparer) : base(equalityComparer)
        {
        }

        internal ReadOnlyNameValueCollection(ReadOnlyNameValueCollection value) : base(value)
        {
        }

        internal void SetReadOnly()
        {
            IsReadOnly = true;
        }
    }

}
