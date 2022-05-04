// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

namespace System.Collections.Specialized.Tests
{
    internal sealed class CaseInsensitiveEqualityComparer : IEqualityComparer
    {
        public new bool Equals(object x, object y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;

            string sa = x as string;
            if (sa != null)
            {
                string sb = y as string;
                if (sb != null)
                {
                    return sa.Equals(sb, StringComparison.CurrentCultureIgnoreCase);
                }
            }
            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            string s = obj as string;
            if (s != null)
            {
                return s.ToUpper().GetHashCode();
            }
            return obj.GetHashCode();
        }
    }
}
