// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;
using Xunit;

namespace System.Collections.ArrayListTests
{
    public class AddTests
    {
        [Fact]
        public void TestAddAndRemove()
        {
            StringBuilder sbl3 = new StringBuilder(99);
            StringBuilder sbl4 = new StringBuilder(99);

            int[] in4a = new int[9];

            // Construct, and verify small capacity
            ArrayList al2 = new ArrayList(1);
            in4a[0] = al2.Capacity;
            Assert.Equal(1, in4a[0]);

            // Add the first obj
            sbl3.Length = 0;
            sbl3.Append("hi mom");

            al2.Add(sbl3);
            sbl4 = (StringBuilder)al2[0];

            Assert.Equal(sbl4.ToString(), sbl3.ToString());

            // Add another obj, verify that Add auto increases Capacity when needed.
            sbl3.Length = 0;
            sbl3.Append("low dad");

            al2.Add(sbl3);
            in4a[1] = al2.Capacity;
            Assert.True(in4a[1] > 1);
            Assert.True(in4a[1] > 1);

            sbl3 = (StringBuilder)al2[1];
            Assert.Equal(sbl4.ToString(), sbl3.ToString());

            // 
            int p_inLoops0 = 2; 
            int p_inLoops1 = 2;
 
            al2 = new ArrayList();

            for (int aa = 0; aa < p_inLoops0; aa++)
            {
                al2.Capacity = 1;

                for (int bb = 0; bb < p_inLoops1; bb++)
                {
                    al2.Add("aa==" + aa + " ,bb==" + bb);
                }

                while (al2.Count > 0)
                {
                    al2.RemoveAt(0);
                }
            }

            Assert.Equal(0, al2.Count);
        }
    }
}
