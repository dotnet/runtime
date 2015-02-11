// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;

namespace System.Collections.HashtableTests
{
    public class IsFixedSizeTests
    {
        [Fact]
        public void TestGetFixedSizeBasic()
        {
            Hashtable hsh1, hsh2;

            //[]vanila - IsFixedSize should return false and we should be able to add items to the HT
            hsh1 = new Hashtable();
            hsh2 = Hashtable.Synchronized(new Hashtable());
            Hashtable[] hashtables = { hsh1, hsh2 };

            foreach (Hashtable hsh in hashtables)
            {
                Assert.False(hsh.IsFixedSize);

                for (int i = 0; i < 100; i++)
                    hsh.Add(i, i);

                Assert.Equal(hsh.Count, 100);
                for (int i = 0; i < hsh.Count; i++)
                {
                    Assert.True(hsh.ContainsKey(i));
                    Assert.Equal((int)hsh[i], i);
                }
            }
        }
    }
}
