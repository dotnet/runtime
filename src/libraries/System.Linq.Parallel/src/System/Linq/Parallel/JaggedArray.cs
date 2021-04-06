// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Linq.Parallel
{
    internal static class JaggedArray<TElement>
    {
        public static TElement[][] Allocate(int size1, int size2)
        {
            TElement[][] ret = new TElement[size1][];
            for (int i = 0; i < size1; i++)
                ret[i] = new TElement[size2];

            return ret;
        }
    }
}
