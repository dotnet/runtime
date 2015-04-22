// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
namespace JitInliningTest
{
    class IndexerClass
    {
        private int[] myArray = new int[100];
        public int this[int index]   // indexer declaration
        {
            get
            {
                // Check the index limits
                if (index < 0 || index >= 100)
                    return 0;
                else
                    return myArray[index];
            }
            set
            {
                if (!(index < 0 || index >= 100))
                    myArray[index] = value;
            }
        }
    }

    public class Indexer
    {
        public static int Main()
        {
            int a = -1;
            IndexerClass b = new IndexerClass();
            // call the indexer to initialize the elements #3 and #5:
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

