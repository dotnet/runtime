// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Tests
{
    public class HashSet_RegressionTests
    {
        [Fact]
        public static void Regression_Dev10_609271()
        {
            HashSet<object> hashset = new HashSet<object>();
            ISet<object> iset = (hashset as ISet<object>);
            Assert.NotNull(iset); //"Should not be null."
        }

        [Fact]
        public static void Regression_Dev10_624201()
        {
            Predicate<object> predicate = (Object o) => { return false; };

            object obj = new object();
            HashSet<object> hashset;
            Object[] oa = new Object[2];

            hashset = new HashSet<object>();
            hashset.Add(obj);
            hashset.Remove(obj);

            //Regression test: make sure these don't throw.
            foreach (object o in hashset)
            { }
            hashset.CopyTo(oa, 0, 2);
            hashset.RemoveWhere(predicate);

            // we just want to make sure it doesn't throw.
        }
    }
}