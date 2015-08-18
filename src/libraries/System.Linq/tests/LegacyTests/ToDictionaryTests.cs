// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ToDictionaryTests
    {
        // Class which is passed as an argument for EqualityComparer
        public class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return getCanonicalString(x) == getCanonicalString(y);
            }

            public int GetHashCode(string obj)
            {
                return getCanonicalString(obj).GetHashCode();
            }

            private string getCanonicalString(string word)
            {
                char[] wordChars = word.ToCharArray();
                Array.Sort<char>(wordChars);
                return new string(wordChars);
            }
        }

        private struct Record
        {
#pragma warning disable 0649
            public string Name;
            public int Score;
#pragma warning restore 0649
        }

        public class Helper
        {
            // Helper function to verify that all elements in dictionary are matched
            public static int MatchAll<K, E>(IEnumerable<K> key, IEnumerable<E> element, Dictionary<K, E> dict)
            {
                if ((dict == null) && (key == null) && (element == null)) return 0;
                using (IEnumerator<K> k1 = key.GetEnumerator())
                using (IEnumerator<E> e1 = element.GetEnumerator())
                {
                    while (k1.MoveNext() && e1.MoveNext())
                    {
                        if (!dict.ContainsKey(k1.Current)) return 1;
                        if (!Equals(dict[k1.Current], e1.Current)) return 1;
                        if (!dict.Remove(k1.Current)) return 1;
                    }
                }
                if (dict.Count != 0) return 1;
                return 0;
            }
        }
        public class ToDictionary1
        {
            // Overload-1: 
            public static void Test1_1()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = "null"; source[2].Score = 55;

                var result = source.ToDictionary((e) => e.Name);
            }

            // Overload-1: keySelector returns null. This also proves Overload-4 is called
            public static int Test1_2()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = null; source[2].Score = 55;

                try
                {
                    var result = source.ToDictionary((e) => e.Name);
                    return 1;
                }
                catch (ArgumentNullException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                Test1_1();
                return Test1_2();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToDictionary2
        {
            // Overload-2: 
            public static void Test2_1()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = "null"; source[2].Score = 55;

                var result = source.ToDictionary((e) => e.Name, new AnagramEqualityComparer());
            }

            // Overload-2: keySelector returns null. This also proves Overload-4 is called
            public static int Test2_2()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = null; source[2].Score = 55;

                try
                {
                    var result = source.ToDictionary((e) => e.Name, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                Test2_1();
                return Test2_2();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToDictionary3
        {
            // Overload-3: 
            public static void Test3_1()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = "null"; source[2].Score = 55;

                var result = source.ToDictionary((e) => e.Name, (e) => e);
            }

            // Overload-3: keySelector returns null. This also proves Overload-4 is called
            public static int Test3_2()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = null; source[2].Score = 55;

                try
                {
                    var result = source.ToDictionary((e) => e.Name, (e) => e);
                    return 1;
                }
                catch (ArgumentNullException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                Test3_1();
                return Test3_2();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToDictionary4
        {
            // Overload-4
            public static void Test4_1()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = "null"; source[2].Score = 55;

                var result = source.ToDictionary((e) => e.Name, (e) => e, new AnagramEqualityComparer());
            }

            // Overload-4: keySelector returns null
            public static int Test4_2()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = null; source[2].Score = 55;

                try
                {
                    var result = source.ToDictionary((e) => e.Name, (e) => e, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentNullException)
                {
                    return 0;
                }
            }

            public static int Main()
            {
                Test4_1();
                return Test4_2();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToDictionary5
        {
            // Overload-4: keySelector returns duplicate values
            public static int Test5()
            {
                Record[] source = new Record[3];

                source[0].Name = "Chris"; source[0].Score = 50;
                source[1].Name = "Bob"; source[1].Score = 95;
                source[2].Name = "Bob"; source[2].Score = 55;

                try
                {
                    var result = source.ToDictionary((e) => e.Name, (e) => e, new AnagramEqualityComparer());
                    return 1;
                }
                catch (ArgumentException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test5();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToDictionary6
        {
            // Overload-4: source is empty
            public static int Test6()
            {
                int[] element = new int[] { };
                string[] key = new string[] { };
                Record[] source = new Record[] { };

                Dictionary<string, int> result = source.ToDictionary((e) => e.Name, (e) => e.Score, new AnagramEqualityComparer());

                return Helper.MatchAll<string, int>(key, element, result);
            }


            public static int Main()
            {
                return Test6();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToDictionary7
        {
            // Overload-4: source has one element and comparer is null
            public static int Test7()
            {
                int[] element = new int[] { 5 };
                string[] key = new string[] { "Bob" };
                Record[] source = new Record[] { new Record { Name = key[0], Score = element[0] } };

                var result = source.ToDictionary((e) => e.Name, (e) => e.Score, null);

                return Helper.MatchAll(key, element, result);
            }


            public static int Main()
            {
                return Test7();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToDictionary8
        {
            // Overload-2: source has limited number of elements.
            // elementSelector is not specified and comparer is not null.
            // 1st element in source is an anagram of key[0] to verify the comparer 
            // functions is called.
            public static int Test8()
            {
                string[] key = new string[] { "Bob", "Zen", "Prakash", "Chris", "Sachin" };
                Record[] source = new Record[]{new Record{Name="Bbo", Score=95}, new Record{Name=key[1], Score=45},
                                new Record{Name=key[2], Score=100}, new Record{Name=key[3], Score=90},
                                new Record{Name=key[4], Score=45}};

                var result = source.ToDictionary((e) => e.Name, new AnagramEqualityComparer());

                return Helper.MatchAll(key, source, result);
            }


            public static int Main()
            {
                return Test8();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToDictionary9
        {
            // DDB:171937
            public static int Test9()
            {
                string[] element = new string[] { null };
                string[] key = new string[] { string.Empty };
                string[] source = new string[] { null };

                // The key of a Dictionary can not be null, so add ??. This case can pass before DDB:171937 bug fixing.
                Dictionary<string, string> result = source.ToDictionary((e) => e ?? string.Empty, (e) => e, EqualityComparer<string>.Default);

                return Helper.MatchAll<string, string>(key, element, result);
            }


            public static int Main()
            {
                return Test9();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
