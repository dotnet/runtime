// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Sample
{
    class Program
    {
        private static bool MyPredicate(Object o)
        {
            return false;
        }

        static int Main(string[] args)
        {
            object obj = new object();
            HashSet<object> hashset;
            Object[] oa = new Object[2];

            hashset = new HashSet<object>();
            hashset.Add(obj);
            hashset.Remove(obj);

            //Regression test: make sure these don't throw.
            foreach (object o in hashset)
            {
            }
            hashset.CopyTo(oa, 0, 2);
            hashset.RemoveWhere(MyPredicate);

            return 100;
        }
    }
}

 



