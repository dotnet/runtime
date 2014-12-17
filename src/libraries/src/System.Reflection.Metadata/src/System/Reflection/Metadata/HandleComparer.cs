// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public sealed class HandleComparer : IEqualityComparer<Handle>, IComparer<Handle>
    {
        private static readonly HandleComparer _default = new HandleComparer();

        private HandleComparer()
        {
        }

        public static HandleComparer Default
        {
            get { return _default; }
        }

        public bool Equals(Handle x, Handle y)
        {
            return x == y;
        }

        public int GetHashCode(Handle obj)
        {
            return obj.GetHashCode();
        }

        public int Compare(Handle x, Handle y)
        {
            return TokenTypeIds.CompareTokens(x.value, y.value);
        }
    }
}