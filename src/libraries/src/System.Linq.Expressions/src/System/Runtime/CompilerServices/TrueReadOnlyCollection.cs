// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;

namespace System.Runtime.CompilerServices
{
    internal sealed class TrueReadOnlyCollection<T> : ReadOnlyCollection<T>
    {
        /// <summary>
        /// Creates instance of TrueReadOnlyCollection, wrapping passed in array.
        /// !!! DOES NOT COPY THE ARRAY !!!
        /// </summary>
        public TrueReadOnlyCollection(params T[] list)
            : base(list)
        {
        }
    }
}
