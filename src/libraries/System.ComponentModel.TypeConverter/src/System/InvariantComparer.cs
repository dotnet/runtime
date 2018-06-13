// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;

namespace System
{
    internal class InvariantComparer : IComparer
    {
        private readonly CompareInfo _compareInfo;
        internal static readonly InvariantComparer Default = new InvariantComparer();

        internal InvariantComparer()
        {
            _compareInfo = CultureInfo.InvariantCulture.CompareInfo;
        }

        public int Compare(object a, object b)
        {
            if (a is string sa && b is string sb)
            {
                return _compareInfo.Compare(sa, sb);
            }
            
            return Comparer.Default.Compare(a, b);
        }
    }
}
