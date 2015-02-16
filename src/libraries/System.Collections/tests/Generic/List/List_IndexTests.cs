// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace List_List_IndexTests
{
    public class Driver<T>
    {
        #region IndexOf(T)

        public void IndexOf_Basic(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(i, list.IndexOf(items[i])); //"Expected them to have the same result."
        }

        public void IndexOf_EnsureFirstValue(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(i, list.IndexOf(items[i])); //"Expected them to have the same result."
        }

        public void IndexOf_NonExistingValuesEmpty(T[] items)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(-1, list.IndexOf(items[i])); //"Expected them to have the same result."
        }

        public void IndexOf_NonExistingValuesNonEmpty(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            for (int i = 0; i < itemsY.Length; i++)
                Assert.Equal(-1, list.IndexOf(itemsY[i])); //"Expected them to have the same result."
        }

        public void IndexOf_NullWhenReference(T[] items)
        {
            T t = (T)(object)null;
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.Add(t);
            Assert.Equal(items.Length, list.IndexOf(t)); //"Expected them to have the same length."
        }

        public void IndexOf_Basic_NonGenericIList(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            IList _ilist = list;

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(i, _ilist.IndexOf(items[i])); //"Expected them to have the same result."
        }

        public void IndexOf_EnsureFirstValue_NonGenericIList(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            IList _ilist = list;
            list.AddRange(new TestCollection<T>(items));
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(i, _ilist.IndexOf(items[i])); //"Expected them to have the same result."
        }

        public void IndexOf_NonExistingValuesEmpty_NonGenericIList(T[] items)
        {
            List<T> list = new List<T>();
            IList _ilist = list;

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(-1, _ilist.IndexOf(items[i])); //"Expected them to have the same result."
        }

        public void IndexOf_NonExistingValuesNonEmpty_NonGenericIList(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));
            IList _ilist = list;

            for (int i = 0; i < itemsY.Length; i++)
                Assert.Equal(-1, _ilist.IndexOf(itemsY[i])); //"Expected them to have the same result."
        }

        public void IndexOf_NullWhenReference_NonGenericIList(T[] items)
        {
            List<T> list;
            IList _ilist;

            T t = (T)(object)null;

            list = new List<T>(new TestCollection<T>(items));
            _ilist = list;
            list.Add(t);
            Assert.Equal(items.Length, _ilist.IndexOf(t)); //"Should have returned the same index."


            list = new List<T>(new TestCollection<T>(items));
            _ilist = list;
            Assert.Equal(-1, _ilist.IndexOf(new LinkedListNode<string>("1"))); //"Err_13390ahied Expected IndexOf to return -1 with invalid type"
        }

        #endregion

        #region IndexOf(T, int)

        public void IndexOf2_Validations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            Assert.Equal(-1, list.IndexOf(items[0], items.Length)); //"Should have returned -1 since it is not in the list"
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], items.Length + 1)); //"Expect ArgumentOutOfRangeException for index greater than length of list.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], items.Length + 10)); //"Expect ArgumentOutOfRangeException for index greater than length of list.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], -1)); //"Expect ArgumentOutOfRangeException for negative index."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], int.MinValue)); //"Expect ArgumentOutOfRangeException for negative index."
        }

        public void IndexOf2_Basic(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(i, list.IndexOf(items[i], 0)); //"Expected the same index."
            }
        }

        public void IndexOf2_EnsureFirstValue(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(i, list.IndexOf(items[i], 0)); //"Expected the same index."
            }
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(i + items.Length, list.IndexOf(items[i], items.Length)); //"Expected the same index."
            }
        }

        public void IndexOf2_EnsureFirstValueExtended(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                int index = 0;
                for (int j = 0; j < 4; j++)
                {
                    index = list.IndexOf(items[i], index);
                    Assert.Equal((items.Length * j) + i, index); //"Expected the same index"
                    index++;
                }
            }
        }

        public void IndexOf2_NonExistingValuesEmpty(T[] items)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(-1, list.IndexOf(items[i], 0)); //"Do not expect it to be in the list."
            }
        }

        public void IndexOf2_NonExistingValuesNonEmpty(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(-1, list.IndexOf(itemsY[i], 0)); //"Do not expect it to be in the list."
            }
        }

        public void IndexOf2_NullWhenReference(T[] items)
        {
            T t = (T)(object)null;

            List<T> list = new List<T>(new TestCollection<T>(items));
            list.Add(t);
            Assert.Equal(items.Length, list.IndexOf(t, 0)); //"Expect it to have the same index.."
        }

        #endregion

        #region IndexOf(T, int, int)

        public void IndexOfValidations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            Assert.Equal(-1, list.IndexOf(items[0], items.Length, 0)); //"Expect not to be in the list."
            Assert.Equal(-1, list.IndexOf(items[0], 0, 0)); //""
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], items.Length, 1)); //"ArgumentOutOfRangeException expected on index larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], items.Length + 1, 1)); //"ArgumentOutOfRangeException expected  on index larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], 0, items.Length + 1)); //"ArgumentOutOfRangeException expected  on count larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], items.Length / 2, items.Length / 2 + 1)); //"ArgumentOutOfRangeException expected.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], 0, items.Length + 1)); //"ArgumentOutOfRangeException expected  on count larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], 0, -1)); //"ArgumentOutOfRangeException expected on negative count."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(items[0], -1, 1)); //"ArgumentOutOfRangeException expected on negative index."
        }

        public void IndexOfBasic(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(i, list.IndexOf(items[i], 0, items.Length)); //"Expect same results."
            }
        }

        public void IndexOfEnsureFirstValue(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.IndexOf(items[i], 0, items.Length), i); //"Expect same results."
            }
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.IndexOf(items[i], 0, items.Length * 2), i); //"Expect same results."
            }
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.IndexOf(items[i], items.Length, items.Length), i + items.Length); //"Expect same results."
            }
        }

        public void IndexOfEnsureFirstValueExtended(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                int index = 0;
                for (int j = 0; j < 4; j++)
                {
                    index = list.IndexOf(items[i], index, list.Count - index);
                    Assert.Equal(index, (items.Length * j) + i); //"Expect same results."
                    index++;
                }
            }
        }

        public void IndexOfNonExistingValuesEmpty(T[] items)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.IndexOf(items[i], 0, 0), -1); //"Expect same results."
            }
        }

        public void IndexOfNonExistingValuesNonEmpty(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(list.IndexOf(itemsY[i], 0, itemsX.Length), -1); //"Expect same results."
            }
        }

        public void IndexOfNullWhenReference(T[] items)
        {
            try
            {
                T t = (T)(object)null;

                List<T> list = new List<T>(new TestCollection<T>(items));
                list.Add(t);
                Assert.Equal(list.IndexOf(t, 0, items.Length + 1), items.Length); //"Expect same results."
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("This testcase only applies to reference types");
            }
        }

        #endregion

        #region LastIndexOf(T)

        public void LastIndexOf_Validations(T[] items)
        {
            List<T> list = new List<T>();

            Assert.Equal(list.LastIndexOf(items[0]), -1); //"Expect them to be equal."
        }

        public void LastIndexOf_Basic(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i]), i); //"Expect them to be equal."
            }
        }

        public void LastIndexOf_EnsureLastValue(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i]), i + items.Length); //"Expect them to be equal."
            }
        }

        public void LastIndexOf_EnsureLastValueExtended(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i]), i + (3 * items.Length)); //"Expect them to be equal."
            }
        }

        public void LastIndexOf_NonExistingValuesNonEmpty(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(itemsY[i]), -1); //"Expect them to be equal."
            }
        }

        public void LastIndexOf_NullWhenReference(T[] items)
        {
            try
            {
                T t = (T)(object)null;

                List<T> list = new List<T>(new TestCollection<T>(items));
                list.Add(t);
                list.Add(t);
                list.Add(t);
                Assert.Equal(list.LastIndexOf(t), items.Length + 2); //"Expect them to be equal."
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("This testcase only applies to reference types");
            }
        }

        #endregion

        #region LastIndexOf(T, int)

        public void LastIndexOf2_Validations(T[] items)
        {
            List<T> list = new List<T>();

            Assert.Equal(list.LastIndexOf(items[0], -2), -1); //"Expected to be the same."

            list = new List<T>(new TestCollection<T>(items));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(items[0], items.Length)); //"ArgumentOutOfRangeException expected."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(items[0], -1)); //"ArgumentOutOfRangeException expected"
        }
        public void LastIndexOf2_Basic(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i], list.Count - 1), i); //"Expected to be the same."
            }
        }

        public void LastIndexOf2_EnsureLastValue(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i], list.Count - 1), i + items.Length); //"Expected to be the same."
            }
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i], items.Length - 1), i); //"Expected to be the same."
            }
        }

        public void LastIndexOf2_EnsureLastValueExtended(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                int index = list.Count - 1;
                for (int j = 3; j >= 0; j--)
                {
                    index = list.LastIndexOf(items[i], index);
                    Assert.Equal(index, i + (j * items.Length)); //"Expected to be the same."
                    index--;
                }
            }
        }

        public void LastIndexOf2_NonExistingValuesNonEmpty(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(itemsY[i], list.Count - 1), -1); //"Expected to be the same."
            }
        }

        public void LastIndexOf2_NullWhenReference(T[] items)
        {
            try
            {
                T t = (T)(object)null;

                List<T> list = new List<T>(new TestCollection<T>(items));
                list.Add(t);
                list.Add(t);
                list.Add(t);
                Assert.Equal(list.LastIndexOf(t, list.Count - 1), items.Length + 2); //"Expected to be the same."
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("This testcase only applies to reference types");
            }
        }

        #endregion

        #region LastIndexOf(T, int, int)

        public void LastIndexOfValidations(T[] items)
        {
            List<T> list = new List<T>();

            Assert.Equal(list.LastIndexOf(items[0], -2, -2), -1); //"Expected to be the same."

            list = new List<T>(new TestCollection<T>(items));

            Assert.Equal(list.LastIndexOf(items[0], 0, 0), -1); //"Expected to be the same."

            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(items[0], items.Length, 0)); //"Expected ArgumentOutOfRangeException."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(items[0], items.Length, 1)); //"Expected ArgumentOutOfRangeException."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(items[0], 0, items.Length + 1)); //"Expected ArgumentOutOfRangeException."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(items[0], items.Length / 2, items.Length / 2 + 2)); //"Expected ArgumentOutOfRangeException."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(items[0], 0, items.Length + 1)); //"Expected ArgumentOutOfRangeException."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(items[0], 0, -1)); //"Expected ArgumentOutOfRangeException."
        }

        public void LastIndexOfBasic(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i], list.Count - 1, items.Length), i); //"Expected to be the same."
            }
        }

        public void LastIndexOfEnsureLastValue(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i], list.Count - 1, list.Count), i + items.Length); //"Expected to be the same."
            }
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i], items.Length - 1, items.Length), i); //"Expected to be the same."
            }
        }

        public void LastIndexOfEnsureLastValueExtended(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));
            list.AddRange(new TestCollection<T>(items));

            for (int i = 0; i < items.Length; i++)
            {
                int index = list.Count - 1;
                for (int j = 3; j >= 0; j--)
                {
                    index = list.LastIndexOf(items[i], index, (1 + list.Count - (list.Count - index)));
                    Assert.Equal(index, i + (j * items.Length)); //"Expected to be the same."
                    index--;
                }
            }
        }

        public void LastIndexOfNonExistingValuesEmpty(T[] items)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(items[i], 0, 0), -1); //"Expected to be the same."
            }
        }

        public void LastIndexOfNonExistingValuesNonEmpty(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(list.LastIndexOf(itemsY[i], list.Count - 1, itemsX.Length), -1); //"Expected to be the same."
            }
        }

        public void LastIndexOfNullWhenReference(T[] items)
        {
            try
            {
                T t = (T)(object)null;

                List<T> list = new List<T>(new TestCollection<T>(items));
                list.Add(t);
                list.Add(t);
                list.Add(t);
                Assert.Equal(list.LastIndexOf(t, list.Count - 1, list.Count), items.Length + 2); //"Expected to be the same."
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("This testcase only applies to reference types");
            }
        }

        #endregion
    }

    public class IndexOfTests
    {
        [Fact]
        public static void IndexOf_Val()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr1[i] = i;

            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr2[i] = i + 10;

            IntDriver.IndexOf_Basic(intArr1);
            IntDriver.IndexOf_EnsureFirstValue(intArr1);
            IntDriver.IndexOf_NonExistingValuesEmpty(intArr1);
            IntDriver.IndexOf_NonExistingValuesNonEmpty(intArr1, intArr2);
            IntDriver.IndexOf_Basic_NonGenericIList(intArr1);
            IntDriver.IndexOf_EnsureFirstValue_NonGenericIList(intArr1);
            IntDriver.IndexOf_NonExistingValuesEmpty_NonGenericIList(intArr1);
            IntDriver.IndexOf_NonExistingValuesNonEmpty_NonGenericIList(intArr1, intArr2);
        }

        [Fact]
        public static void IndexOf_Ref()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr2[i] = "SomeTestString" + (i + 10).ToString();

            StringDriver.IndexOf_Basic(stringArr1);
            StringDriver.IndexOf_EnsureFirstValue(stringArr1);
            StringDriver.IndexOf_NonExistingValuesEmpty(stringArr1);
            StringDriver.IndexOf_NonExistingValuesNonEmpty(stringArr1, stringArr2);
            StringDriver.IndexOf_NullWhenReference(stringArr1);
            StringDriver.IndexOf_Basic_NonGenericIList(stringArr1);
            StringDriver.IndexOf_EnsureFirstValue_NonGenericIList(stringArr1);
            StringDriver.IndexOf_NonExistingValuesEmpty_NonGenericIList(stringArr1);
            StringDriver.IndexOf_NonExistingValuesNonEmpty_NonGenericIList(stringArr1, stringArr2);
            StringDriver.IndexOf_NullWhenReference_NonGenericIList(stringArr1);
        }

        [Fact]
        public static void IndexOf2_Val()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr1[i] = i;

            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr2[i] = i + 10;

            IntDriver.IndexOf2_Basic(intArr1);
            IntDriver.IndexOf2_EnsureFirstValue(intArr1);
            IntDriver.IndexOf2_EnsureFirstValueExtended(intArr1);
            IntDriver.IndexOf2_NonExistingValuesEmpty(intArr1);
            IntDriver.IndexOf2_NonExistingValuesNonEmpty(intArr1, intArr2);
        }

        [Fact]
        public static void IndexOf2_Ref()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr2[i] = "SomeTestString" + (i + 10).ToString();

            StringDriver.IndexOf2_Basic(stringArr1);
            StringDriver.IndexOf2_EnsureFirstValue(stringArr1);
            StringDriver.IndexOf2_EnsureFirstValueExtended(stringArr1);
            StringDriver.IndexOf2_NonExistingValuesEmpty(stringArr1);
            StringDriver.IndexOf2_NonExistingValuesNonEmpty(stringArr1, stringArr2);
            StringDriver.IndexOf2_NullWhenReference(stringArr1);
        }

        [Fact]
        public static void IndexOf2_Negative()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            StringDriver.IndexOf2_Validations(stringArr1);
        }

        [Fact]
        public static void IndexOf3_Val()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr1[i] = i;

            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr2[i] = i + 10;

            IntDriver.IndexOfBasic(intArr1);
            IntDriver.IndexOfEnsureFirstValue(intArr1);
            IntDriver.IndexOfEnsureFirstValueExtended(intArr1);
            IntDriver.IndexOfNonExistingValuesEmpty(intArr1);
            IntDriver.IndexOfNonExistingValuesNonEmpty(intArr1, intArr2);
        }

        [Fact]
        public static void IndexOf3_Ref()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr2[i] = "SomeTestString" + (i + 10).ToString();

            StringDriver.IndexOfBasic(stringArr1);
            StringDriver.IndexOfEnsureFirstValue(stringArr1);
            StringDriver.IndexOfEnsureFirstValueExtended(stringArr1);
            StringDriver.IndexOfNonExistingValuesEmpty(stringArr1);
            StringDriver.IndexOfNonExistingValuesNonEmpty(stringArr1, stringArr2);
            StringDriver.IndexOfNullWhenReference(stringArr1);
        }

        [Fact]
        public static void IndexOf3_Negative()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();
            StringDriver.IndexOfValidations(stringArr1);
        }

        [Fact]
        public static void LastIndexOf_Val()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr1[i] = i;
            }

            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr2[i] = i + 10;
            }

            IntDriver.LastIndexOf_Validations(intArr1);
            IntDriver.LastIndexOf_Basic(intArr1);
            IntDriver.LastIndexOf_EnsureLastValue(intArr1);
            IntDriver.LastIndexOf_EnsureLastValueExtended(intArr1);
            IntDriver.LastIndexOf_NonExistingValuesNonEmpty(intArr1, intArr2);
        }

        [Fact]
        public static void LastIndexOf_Ref()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr1[i] = "SomeTestString" + i.ToString();
            }
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr2[i] = "SomeTestString" + (i + 10).ToString();
            }

            StringDriver.LastIndexOf_Validations(stringArr1);
            StringDriver.LastIndexOf_Basic(stringArr1);
            StringDriver.LastIndexOf_EnsureLastValue(stringArr1);
            StringDriver.LastIndexOf_EnsureLastValueExtended(stringArr1);
            StringDriver.LastIndexOf_NonExistingValuesNonEmpty(stringArr1, stringArr2);
            StringDriver.LastIndexOf_NullWhenReference(stringArr1);
        }

        [Fact]
        public static void LastIndexOf2_Val()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr1[i] = i;
            }

            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr2[i] = i + 10;
            }

            IntDriver.LastIndexOf2_Basic(intArr1);
            IntDriver.LastIndexOf2_EnsureLastValue(intArr1);
            IntDriver.LastIndexOf2_EnsureLastValueExtended(intArr1);
            IntDriver.LastIndexOf2_NonExistingValuesNonEmpty(intArr1, intArr2);
        }

        [Fact]
        public static void LastIndexOf2_Ref()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr1[i] = "SomeTestString" + i.ToString();
            }
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr2[i] = "SomeTestString" + (i + 10).ToString();
            }

            StringDriver.LastIndexOf2_Basic(stringArr1);
            StringDriver.LastIndexOf2_EnsureLastValue(stringArr1);
            StringDriver.LastIndexOf2_EnsureLastValueExtended(stringArr1);
            StringDriver.LastIndexOf2_NonExistingValuesNonEmpty(stringArr1, stringArr2);
            StringDriver.LastIndexOf2_NullWhenReference(stringArr1);
        }

        [Fact]
        public static void LastIndexOf2_Negative()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr1[i] = i;
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            IntDriver.LastIndexOf2_Validations(intArr1);
            StringDriver.LastIndexOf2_Validations(stringArr1);
        }

        [Fact]
        public static void LastIndexOf3_Val()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr1[i] = i;
            }

            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
            {
                intArr2[i] = i + 10;
            }

            IntDriver.LastIndexOfBasic(intArr1);
            IntDriver.LastIndexOfEnsureLastValue(intArr1);
            IntDriver.LastIndexOfEnsureLastValueExtended(intArr1);
            IntDriver.LastIndexOfNonExistingValuesEmpty(intArr1);
            IntDriver.LastIndexOfNonExistingValuesNonEmpty(intArr1, intArr2);
        }

        [Fact]
        public static void LastIndexOf3_Ref()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr1[i] = "SomeTestString" + i.ToString();
            }
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
            {
                stringArr2[i] = "SomeTestString" + (i + 10).ToString();
            }

            StringDriver.LastIndexOfBasic(stringArr1);
            StringDriver.LastIndexOfEnsureLastValue(stringArr1);
            StringDriver.LastIndexOfEnsureLastValueExtended(stringArr1);
            StringDriver.LastIndexOfNonExistingValuesEmpty(stringArr1);
            StringDriver.LastIndexOfNonExistingValuesNonEmpty(stringArr1, stringArr2);
            StringDriver.LastIndexOfNullWhenReference(stringArr1);
        }

        [Fact]
        public static void LastIndexOf3_Negative()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr1[i] = i;
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            IntDriver.LastIndexOfValidations(intArr1);
            StringDriver.LastIndexOfValidations(stringArr1);
        }
    }

    #region Helper Classes

    /// <summary>
    /// Helper class that implements ICollection.
    /// </summary>
    public class TestCollection<T> : ICollection<T>
    {
        /// <summary>
        /// Expose the Items in Array to give more test flexibility...
        /// </summary>
        public readonly T[] m_items;

        public TestCollection(T[] items)
        {
            m_items = items;
        }

        public void CopyTo(T[] array, int index)
        {
            Array.Copy(m_items, 0, array, index, m_items.Length);
        }

        public int Count
        {
            get
            {
                if (m_items == null)
                    return 0;
                else
                    return m_items.Length;
            }
        }

        public Object SyncRoot { get { return this; } }

        public bool IsSynchronized { get { return false; } }

        public IEnumerator<T> GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        private class TestCollectionEnumerator<T1> : IEnumerator<T1>
        {
            private TestCollection<T1> _col;
            private int _index;

            public void Dispose() { }

            public TestCollectionEnumerator(TestCollection<T1> col)
            {
                _col = col;
                _index = -1;
            }

            public bool MoveNext()
            {
                return (++_index < _col.m_items.Length);
            }

            public T1 Current
            {
                get { return _col.m_items[_index]; }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return _col.m_items[_index]; }
            }

            public void Reset()
            {
                _index = -1;
            }
        }

        #region Non Implemented methods

        public void Add(T item) { throw new NotSupportedException(); }

        public void Clear() { throw new NotSupportedException(); }
        public bool Contains(T item) { throw new NotSupportedException(); }

        public bool Remove(T item) { throw new NotSupportedException(); }

        public bool IsReadOnly { get { throw new NotSupportedException(); } }

        #endregion
    }
    #endregion
}
