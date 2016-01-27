// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace JitInliningTest
{
    internal class IndexerClass
    {
        private int[] _myArray = new int[100];
        public int this[int index]
        {
            get
            {
                if (index < 0 || index >= 100)
                    return 0;
                else
                    return _myArray[index];
            }
            set
            {
                if (!(index < 0 || index >= 100))
                    _myArray[index] = value;
            }
        }
    }

    public class Indexer
    {
        public static int Main()
        {
            int a = -1;
            IndexerClass b = new IndexerClass();
            b[3] = 256;
            b[5] = 1024;
            for (int i = 0; i <= 10; i++)
                a += b[i];
            if (a == 1279)
                return 100;
            else
                return 1;
        }
    }
}

