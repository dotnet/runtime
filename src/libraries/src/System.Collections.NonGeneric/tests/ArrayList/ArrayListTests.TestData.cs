// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Tests
{
    public static partial class ArrayListTests
    {
        private static readonly string[] basicTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            "Huntress",
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            "Superman",
            "Thor",
            "Wildcat",
            "Wonder Woman"
        };

        private static readonly string[] nullContainingTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null
        };

        private static readonly string[] binarySearchFindTestData = new string[]
        {
            "Batman",
            "Superman",
            "SpiderMan",
            "Wonder Woman",
            "Green Lantern",
            "Flash",
            "Steel"
        };

        private static readonly string[] setRangeTestData = new string[]
        {
            "Hardware",
            "Icon",
            "Johnny Quest",
            "Captain Sisko",
            "Captain Picard",
            "Captain Kirk",
            "Agent J",
            "Agent K",
            "Space Ghost",
            "Wolverine",
            "Cyclops",
            "Storm",
            "Lone Ranger",
            "Tonto",
            "Supergirl"
        };

        public static string[] indexOfUniqueTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            "Daniel Takacs",
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            "Gene",
            "Thor",
            "Wildcat",
            null
        };


        private static readonly string[] duplicateContainingTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Batman",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Batman",
            "Green Lantern",
            "Hawkman",
            "Huntress",
            "Ironman",
            "Nightwing",
            "Batman",
            "Robin",
            "SpiderMan",
            "Steel",
            "Superman",
            "Thor",
            "Batman",
            "Wildcat",
            "Wonder Woman",
            "Batman"
        };

        private static readonly string[] duplicateAndNullContainingTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Batman",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Batman",
            "Green Lantern",
            "Hawkman",
            "Huntress",
            "Ironman",
            "Nightwing",
            "Batman",
            "Robin",
            "SpiderMan",
            "Steel",
            "Superman",
            "Thor",
            "Batman",
            "Wildcat",
            "Wonder Woman",
            "Batman",
            null
        };

        private static readonly string[] insertRangeRangeToInsertTestData = new string[]
        {
            "Dr. Fate",
            "Dr. Light",
            "Dr. Manhattan",
            "Hardware",
            "Hawkeye",
            "Icon",
            "Spawn",
            "Spectre",
            "Supergirl"
        };

        private static readonly string[] insertRangeExpectedTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Dr. Fate",
            "Dr. Light",
            "Dr. Manhattan",
            "Hardware",
            "Hawkeye",
            "Icon",
            "Spawn",
            "Spectre",
            "Supergirl",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            "Huntress",
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            "Superman",
            "Thor",
            "Wildcat",
            "Wonder Woman"
        };

        private static readonly string[] insertObjectsToInsertTestData = new string[]
        {
            "Dr. Fate",
            "Dr. Light",
            "Dr. Manhattan",
            "Hardware",
            "Hawkeye",
            "Icon",
            "Spawn",
            "Spectre",
            "Supergirl"
        };

        private static readonly string[] insertExpectedTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Dr. Fate",
            "Dr. Light",
            "Dr. Manhattan",
            "Hardware",
            "Hawkeye",
            "Icon",
            "Spawn",
            "Spectre",
            "Supergirl",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            "Huntress",
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            "Superman",
            "Thor",
            "Wildcat",
            "Wonder Woman"
        };

        private static readonly string[] removeAtExpectedTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Superman",
            "Thor",
            "Wildcat",
            "Wonder Woman"
        };

        private static readonly string[] removeRangeExpectedTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Superman",
            "Thor",
            "Wildcat",
            "Wonder Woman"
        };

        private static readonly string[] reverseExpectedTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Flash",
            "Cyborg",
            "Catwoman",
            "Captain Atom",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            "Huntress",
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            "Superman",
            "Thor",
            "Wildcat",
            "Wonder Woman"
        };

        private static readonly string[] sortTestData = new string[]
        {
            "Green Arrow",
            "Atom",
            "Batman",
            "Steel",
            "Superman",
            "Wonder Woman",
            "Hawkman",
            "Flash",
            "Aquaman",
            "Green Lantern",
            "Catwoman",
            "Huntress",
            "Robin",
            "Captain Atom",
            "Wildcat",
            "Nightwing",
            "Ironman",
            "SpiderMan",
            "Black Canary",
            "Thor",
            "Cyborg",
            "Captain America"
        };

        private static readonly string[] sortDescendingExpectedTestData = new string[]
        {
            "Wonder Woman",
            "Wildcat",
            "Thor",
            "Superman",
            "Steel",
            "SpiderMan",
            "Robin",
            "Nightwing",
            "Ironman",
            "Huntress",
            "Hawkman",
            "Green Lantern",
            "Green Arrow",
            "Flash",
            "Cyborg",
            "Catwoman",
            "Captain Atom",
            "Captain America",
            "Black Canary",
            "Batman",
            "Atom",
            "Aquaman"
        };

        private static readonly string[] sortAscendingExpectedTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            "Huntress",
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            "Superman",
            "Thor",
            "Wildcat",
            "Wonder Woman"
        };

        private static readonly string[] sortRangeAscendingExpectedTestData = new string[]
        {
            "Green Arrow",
            "Atom",
            "Batman",
            "Flash",
            "Hawkman",
            "Steel",
            "Superman",
            "Wonder Woman",
            "Aquaman",
            "Green Lantern",
            "Catwoman",
            "Huntress",
            "Robin",
            "Captain Atom",
            "Wildcat",
            "Nightwing",
            "Ironman",
            "SpiderMan",
            "Black Canary",
            "Thor",
            "Cyborg",
            "Captain America"
        };

        public static string[] synchronizedTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America"
        };

        private class BinarySearchComparer : IComparer
        {
            public virtual int Compare(object x, object y)
            {
                if (x is string)
                {
                    return ((string)x).CompareTo((string)y);
                }

                var comparer = new Comparer(Globalization.CultureInfo.InvariantCulture);
                if (x is int || y is string)
                {
                    return comparer.Compare(x, y);
                }

                return -1;
            }
        }

        private class CompareWithNullEnabled : IComparer
        {
            public int Compare(object a, object b)
            {
                if (a == b) return 0;
                if (a == null) return -1;
                if (b == null) return 1;

                IComparable ia = a as IComparable;
                if (ia != null)
                    return ia.CompareTo(b);

                IComparable ib = b as IComparable;
                if (ib != null)
                    return -ib.CompareTo(a);

                throw new ArgumentException("Wrong stuff");
            }
        }

        private class Foo
        {
            private string _stringValue = "Hello World";
            public string StringValue
            {
                get { return _stringValue; }
                set { _stringValue = value; }
            }
        }

        private class MyCollection : ICollection
        {
            private ICollection _collection;
            private Array _array;
            private int _startIndex;

            public MyCollection(ICollection collection)
            {
                _collection = collection;
            }

            public Array Array
            {
                get
                {
                    return _array;
                }
            }

            public int StartIndex
            {
                get
                {
                    return _startIndex;
                }
            }

            public int Count
            {
                get
                {
                    return _collection.Count;
                }
            }

            public object SyncRoot
            {
                get
                {
                    return _collection.SyncRoot;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return _collection.IsSynchronized;
                }
            }

            public void CopyTo(Array array, int startIndex)
            {
                _array = array;
                _startIndex = startIndex;
                _collection.CopyTo(array, startIndex);
            }

            public IEnumerator GetEnumerator()
            {
                throw new NotSupportedException();
            }
        }

        private class AscendingComparer : IComparer
        {
            public virtual int Compare(object x, object y)
            {
                return ((string)x).CompareTo((string)y);
            }
        }

        private class DescendingComparer : IComparer
        {
            public virtual int Compare(object x, object y)
            {
                return -((string)x).CompareTo((string)y);
            }
        }

        private class DerivedArrayList: ArrayList
        {
            public DerivedArrayList(ICollection c) : base(c)
            {
            }
        }
    }
}
