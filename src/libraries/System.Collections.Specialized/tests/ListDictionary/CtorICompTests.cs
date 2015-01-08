// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    internal class SensitiveComparer : IComparer
    {
        public Int32 Compare(Object obj1, Object obj2)
        {
            if (!(obj1 is String) || !(obj2 is String))
                throw new Exception("Err! object needs to be String");
            return String.Compare((String)obj1, (String)obj2);
        }
    }

    internal class CaseInsensitiveComparer : IComparer
    {
        public Int32 Compare(Object obj1, Object obj2)
        {
            if (!(obj1 is String) || !(obj2 is String))
                throw new Exception("Err! object needs to be String");
            return String.Compare((String)obj1, (String)obj2, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    public class CtorICompTests
    {
        [Fact]
        public void Test01()
        {
            ListDictionary ld;

            // simple string values
            string[] values =
            {
                "item",
                "Item",
                "iTem"
            };

            // names(keys) for simple string values
            string[] names =
            {
                "key",
                "Key",
                "kEy"
            };

            // [] ListDictionary is constructed as expected
            //-----------------------------------------------------------------


            //
            //  create dictionary
            // [] ctor(case-insensitive-comparer)
            //
            ld = new ListDictionary(new CaseInsensitiveComparer());
            int len = values.Length;
            ld.Add(names[0], values[0]);
            if (ld.Count != 1)
            {
                Assert.False(true, string.Format("Error, Count is {0} instead of {1}", ld.Count, 1));
            }
            Assert.Throws<ArgumentException>(() => { ld.Add(names[1], values[1]); });

            if (String.Compare(ld[names[0]].ToString(), values[0]) != 0)
            {
                Assert.False(true, string.Format("Error, returned {0} instead of {1}", ld[names[0]], values[0]));
            }
            if (String.Compare(ld[names[0].ToUpper()].ToString(), values[0]) != 0)
            {
                Assert.False(true, string.Format("Error, failed for uppercase key"));
            }

            //
            //  create ListDictionary
            // [] ctor(case-sensitive-comparer)
            //
            ld = new ListDictionary(new SensitiveComparer());
            for (int i = 0; i < len; i++)
            {
                ld.Add(names[i], values[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, Count is {0} instead of {1}", ld.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                if (String.Compare(ld[names[i]].ToString(), values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {0} instead of {1}", ld[names[i]], values[i], i));
                }
            }
            if (ld[names[0].ToUpper()] != null)
            {
                Assert.False(true, string.Format("Error, returned non-null for non-existing uppercase key"));
            }

            //
            //  null parameter  - no exception - case-sensitive comparer
            // [] ctor(null)
            //
            ld = new ListDictionary(null);
            // add some items
            for (int i = 0; i < len; i++)
            {
                ld.Add(names[i], values[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, Count is {0} instead of {1}", ld.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                if (String.Compare(ld[names[i]].ToString(), values[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned {0} instead of {1}", ld[names[i]], values[i], i));
                }
            }
        }
    }
}
