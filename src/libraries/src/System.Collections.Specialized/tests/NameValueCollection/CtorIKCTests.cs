// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

namespace System.Collections.Specialized.Tests
{
    public class IdiotComparer : IEqualityComparer
    {
        public new Boolean Equals(object x, object y)
        {
            // everything is equal
            return true;
        }
        public Int32 GetHashCode(object obj)
        {
            return 0;
        }
    }

    public class CtorIKCTests
    {
        [Fact]
        public void Test01()
        {
            NameValueCollection nvc;

            // simple string values
            string[] values =
            {
                "item",
                "Item",
                "\u0130tem", // capital Turkish I-dot
            };
            string exp3 = "item,Item,\u0130tem";

            // names(keys) for simple string values
            string[] names =
            {
                "key_i",
                "Key_I",
                "key_\u0130" // capital Turkish I-dot
            };

            // Set current CultureInfo to Turkish, so we can verify CurrentCulture vs. InvariantCulture
            // comparison types.  Must be done before calling any constructors.
            var prevCulture = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("tr-TR");

            // [] NameValueCollection is constructed as expected
            //-----------------------------------------------------------------

            //
            //  create collection
            //
            //  capacity=0
            //
            nvc = new NameValueCollection(new IdiotComparer());
            int len = values.Length;
            for (int i = 0; i < len; i++)
            {
                nvc.Add(names[i], values[i]);
            }
            if (nvc.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count is {0} instead of {1}", nvc.Count, 1));
            }
            for (int i = 0; i < len; i++)
            {
                if (String.Compare(nvc[names[i]], exp3) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {0} instead of {1}", nvc[names[i]], exp3));
                }
            }
            if (nvc["Everything Is Equal!"] == null)
            {
                Assert.False(true, string.Format("Error, returned null instead of {0} ", exp3));
            }
            else if (String.Compare(nvc["Everything Is Equal!"], exp3) != 0)
            {
                Assert.False(true, string.Format("Error, returned {0} instead of {1}", nvc["Everything Is Equal!"], exp3));
            }

            // Comparer is null --> use default comparer

            NameValueCollection nvc2 = new NameValueCollection((IEqualityComparer)null);
            nvc.Add("one", "one");
            nvc.Remove("one");
        }
    }
}
