// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace List_List_FindTests
{
    public class Driver<T>
    {
        public void FindVerifyExceptions(T[] items)
        {
            List<T> list = new List<T>();

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Null match Find
            Assert.Throws<ArgumentNullException>(() => list.Find(null)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            //[] Verify Null match FindLast
            Assert.Throws<ArgumentNullException>(() => list.FindLast(null)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            //[] Verify Null match FindLastIndex
            Assert.Throws<ArgumentNullException>(() => list.FindLastIndex(null)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            //[] Verify Null match FindAll
            Assert.Throws<ArgumentNullException>(() => list.FindAll(null)); //"Err_858ahia Expected null match to throw ArgumentNullException"
        }

        #region Find

        public void Find_Verify(T[] items)
        {
            Find_VerifyVanilla(items);
            Find_VerifyDuplicates(items);
        }

        private void Find_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            T foundItem;
            Predicate<T> expectedItemDelegate = (T item) =>
            {
                return expectedItem == null
                    ? item == null : expectedItem.Equals(item);
            };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Find returns the correct index
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                foundItem = list.Find(expectedItemDelegate);

                Assert.Equal(expectedItem, foundItem); //"Err_282308ahid Verifying value retunred from Find FAILED\n"
            }

            //[] Verify Find returns the first item if the match returns true on every item
            foundItem = list.Find((T item) => { return true; });
            Assert.Equal(0 < items.Length ? items[0] : default(T), foundItem); //"Err_548ahid Verify Find returns the first item if the match returns true on every item FAILED\n"

            //[] Verify Find returns T.Default if the match returns false on every item
            foundItem = list.Find((T item) => { return false; });
            Assert.Equal(default(T), foundItem); //"Err_30848ahidi Verify Find returns T.Default if the match returns false on every item FAILED\n"

            //[] Verify with default(T)
            list.Add(default(T));
            foundItem = list.Find((T item) => { return item == null ? default(T) == null : item.Equals(default(T)); });
            Assert.Equal(default(T), foundItem); //"Err_541848ajodi Verify with default(T) FAILED\n"
            list.RemoveAt(list.Count - 1);
        }

        private void Find_VerifyDuplicates(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            T foundItem;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                foundItem = list.Find(expectedItemDelegate);
                Assert.Equal(items[0], foundItem); //"Err_2879072qaiadf  Verify first item is duplicated FAILED\n"
            }

            if (1 < items.Length)
            {
                //[] Verify second item is duplicated
                expectedItem = items[1];
                foundItem = list.Find(expectedItemDelegate);
                Assert.Equal(items[1], foundItem); //"Err_4588ajdia Verify second item is duplicated FAILED\n"

                //[] Verify with match that matches more then one item
                foundItem = list.Find((T item) => { return item != null && (item.Equals(items[0]) || item.Equals(items[1])); });
                Assert.Equal(items[0], foundItem); //"Err_4489ajodoi Verify with match that matches more then one item FAILED\n"
            }
        }

        #endregion

        #region FindLast

        public void FindLast_Verify(T[] items)
        {
            FindLast_VerifyVanilla(items);
            FindLast_VerifyDuplicates(items);
        }

        private void FindLast_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            T foundItem;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify FindLast returns the correct item
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                foundItem = list.FindLast(expectedItemDelegate);

                Assert.Equal(expectedItem, foundItem); //"Err_282308ahid Verifying value returned from find FAILED\n"
            }

            //[] Verify FindLast returns the last item if the match returns true on every item
            foundItem = list.FindLast((T item) => { return true; });
            T expected = 0 < items.Length ? items[items.Length - 1] : default(T);
            Assert.Equal(expected, foundItem); //"Err_548ahid Verify FindLast returns the last item if the match returns true on every item FAILED\n"

            //[] Verify FindLast returns default(T) if the match returns false on every item
            foundItem = list.FindLast((T item) => { return false; });
            Assert.Equal(default(T), foundItem); //"Err_30848ahidi Verify FindLast returns t.default if the match returns false on every item FAILED\n"

            //[] Verify with default(T)
            list.Add(default(T));
            foundItem = list.FindLast((T item) => { return item == null ? default(T) == null : item.Equals(default(T)); });
            Assert.Equal(default(T), foundItem); //"Err_541848ajodi Verify with default(T) FAILED\n"
            list.RemoveAt(list.Count - 1);
        }

        private void FindLast_VerifyDuplicates(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            T foundItem;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                foundItem = list.FindLast(expectedItemDelegate);
                Assert.Equal(items[0], foundItem); //"Err_2879072qaiadf  Verify first item is duplicated FAILED\n"
            }

            if (1 < items.Length)
            {
                //[] Verify second item is duplicated
                expectedItem = items[1];
                foundItem = list.FindLast(expectedItemDelegate);
                Assert.Equal(items[1], foundItem); //"Err_4588ajdia Verify second item is duplicated FAILED\n"

                //[] Verify with match that matches more then one item
                foundItem = list.FindLast((T item) => { return item != null && (item.Equals(items[0]) || item.Equals(items[1])); });
                Assert.Equal(items[1], foundItem); //"Err_4489ajodoi Verify with match that matches more then one item FAILED\n"
            }
        }

        #endregion

        #region FindIndex

        public void FindIndex_Verify(T[] items)
        {
            FindIndex_VerifyVanilla(items);
            FindIndex_VerifyDuplicates(items);
        }

        private void FindIndex_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                index = list.FindIndex(expectedItemDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindIndex to return the same."
            }

            //[] Verify FindIndex returns 0 if the match returns true on every item
            int expected = items.Length == 0 ? -1 : 0;
            index = list.FindIndex((T item) => { return true; });
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindIndex returns 0 if the match returns true on every item expected"

            //[] Verify FindIndex returns -1 if the match returns false on every item
            index = list.FindIndex((T item) => { return false; });
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindIndex returns -1 if the match returns false on every item"
        }

        private void FindIndex_VerifyDuplicates(T[] items)
        {
            List<T> list = new List<T>();
            T expectedItem = default(T);
            int index;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                index = list.FindIndex(expectedItemDelegate);
                Assert.Equal(0, index); //"Err_3282iahid Verify first item is duplicated"
            }

            if (1 < items.Length)
            {
                //[] Verify second item is duplicated
                expectedItem = items[1];
                index = list.FindIndex(expectedItemDelegate);
                Assert.Equal(1, index); //"Err_29892adewiu Verify second item is duplicated"
            }
        }

        #endregion

        #region FindIndex(int, pred<T>)

        public void FindIndexInt_Verify(T[] items)
        {
            FindIndexInt_VerifyVanilla(items);
            FindIndexInt_VerifyDuplicates(items);
        }

        public void FindIndexInt_VerifyExceptions(T[] items)
        {
            List<T> list = new List<T>();
            Predicate<T> predicate = delegate (T item) { return true; };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.FindIndex(0, null)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            /******************************************************************************
            index
            ******************************************************************************/
            //[] Verify index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(Int32.MinValue, predicate)); //"Err_948ahid Expected index=Int32.MinValue to throw ArgumentOutOfRangeException"

            //[] Verify index=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(-1, predicate)); //"Err_328ahuaw Expected index=-1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(list.Count + 1, predicate)); //"Err_488ajdi Expected index=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify index=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(Int32.MaxValue, predicate)); //"Err_238ajwisa Expected index=Int32.MaxValue to throw ArgumentOutOfRangeException"
        }

        private void FindIndexInt_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                index = list.FindIndex(0, expectedItemDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindIndex to return the same"
            }

            //[] Verify FindIndex returns 0 if the match returns true on every item
            int expected = items.Length == 0 ? -1 : 0;
            index = list.FindIndex(0, delegate (T item) { return true; });
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindIndex returns 0 if the match returns true on every item "

            //[] Verify FindIndex returns -1 if the match returns false on every item
            index = list.FindIndex(0, delegate (T item) { return false; });
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindIndex returns -1 if the match returns false on every item"

            //[] Verify FindIndex returns -1 if the index == count
            index = list.FindIndex(items.Length, delegate (T item) { return true; });
            Assert.Equal(-1, index); //"Err_4858ajodoa Verify FindIndex returns -1 if the index == count"

            if (0 < items.Length)
            {
                //[] Verify NEG FindIndex uses the index
                expectedItem = items[0];
                index = list.FindIndex(1, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_548797ahjid Verify NEG FindIndex uses the index"
            }

            if (1 < items.Length)
            {
                //[] Verify POS FindIndex uses the index LOWER
                expectedItem = items[1];
                index = list.FindIndex(1, expectedItemDelegate);
                Assert.Equal(1, index); //"Err_68797ahid Verify POS FindIndex uses the index LOWER"

                //[] Verify POS FindIndex uses the index UPPER
                expectedItem = items[items.Length - 1];
                index = list.FindIndex(1, expectedItemDelegate);
                Assert.Equal(items.Length - 1, index); //"Err_51488ajod Verify POS FindIndex uses the index UPPER"
            }
        }

        private void FindIndexInt_VerifyDuplicates(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                index = list.FindIndex(0, expectedItemDelegate);
                Assert.Equal(0, index); //"Err_3282iahid Verify first item is duplicated"

                //[] Verify first item is duplicated and index=1
                expectedItem = items[0];
                index = list.FindIndex(1, expectedItemDelegate);
                Assert.Equal(items.Length, index); //"Err_8588ahidi Verify first item is duplicated and index=1"
            }

            if (1 < items.Length)
            {
                //[] Verify second item is duplicated
                expectedItem = items[1];
                index = list.FindIndex(0, expectedItemDelegate);
                Assert.Equal(1, index); //"Err_29892adewiu Verify second item is duplicated"

                //[] Verify second item is duplicated and index=2
                expectedItem = items[1];
                index = list.FindIndex(2, expectedItemDelegate);
                Assert.Equal(items.Length + 1, index); //"Err_1580ahisdf Verify second item is duplicated and index=2 "
            }
        }

        #endregion

        #region FindIndex(int, int, pred<T>)

        public void FindIndexIntInt_Verify(T[] items)
        {
            FindIndexIntInt_VerifyVanilla(items);
            FindIndexIntInt_VerifyDuplicates(items);
        }

        public void FindIndexIntInt_VerifyExceptions(T[] items)
        {
            List<T> list = new List<T>();
            Predicate<T> predicate = delegate (T item) { return true; };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.FindIndex(0, 0, null)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            /******************************************************************************
            index
            ******************************************************************************/
            //[] Verify index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(Int32.MinValue, 0, predicate)); //"Err_948ahid Expected index=Int32.MinValue to throw ArgumentOutOfRangeException"

            //[] Verify index=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(-1, 0, predicate)); //"Err_328ahuaw Expected index=-1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(list.Count + 1, 0, predicate)); //"Err_488ajdi Expected index=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(list.Count, 1, predicate)); //"Err_9689ajis Expected index=list.Count to throw ArgumentOutOfRangeException"

            //[] Verify index=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(Int32.MaxValue, 0, predicate)); //"Err_238ajwisa Expected index=Int32.MaxValue to throw ArgumentOutOfRangeException"

            /******************************************************************************
            count
            ******************************************************************************/
            //[] Verify count=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, Int32.MinValue, predicate)); //Err_948ahid Expected count=Int32.MinValue to throw ArgumentOutOfRangeException"

            //[] Verify count=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, -1, predicate)); //"Err_328ahuaw Expected count=-1 to throw ArgumentOutOfRangeException"

            //[] Verify count=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, list.Count + 1, predicate)); //"Err_488ajdi Expected count=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify count=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, Int32.MaxValue, predicate)); //"Err_238ajwisa Expected count=Int32.MaxValue to throw ArgumentOutOfRangeException"

            /******************************************************************************
            index and count
            ******************************************************************************/
            if (0 < items.Length)
            {
                //[] Verify index=1 count=list.Length
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(1, items.Length, predicate)); //"Err_018188avbiw Expected index=1 count=list.Length to throw ArgumentOutOfRangeException"

                //[] Verify index=0 count=list.Length + 1
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(0, items.Length + 1, predicate)); //"Err_6848ajiodxbz Expected index=0 count=list.Length + 1 to throw ArgumentOutOfRangeException"
            }
        }

        private void FindIndexIntInt_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                index = list.FindIndex(0, items.Length, delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); });
                Assert.Equal(i, index); //"Err_282308ahid Expected FindIndex to return the same."
            }

            //[] Verify FindIndex returns 0 if the match returns true on every item
            index = list.FindIndex(0, items.Length, delegate (T item) { return true; });
            int expected = items.Length == 0 ? -1 : 0;
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindIndex returns 0 if the match returns true on every item"

            //[] Verify FindIndex returns -1 if the match returns false on every item
            index = list.FindIndex(0, items.Length, delegate (T item) { return false; });
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindIndex returns -1 if the match returns false on every item"

            //[] Verify FindIndex returns -1 if the index == count
            index = list.FindIndex(items.Length, 0, delegate (T item) { return true; });
            Assert.Equal(-1, index); //"Err_4858ajodoa Verify FindIndex returns -1 if the index == count"

            if (0 < items.Length)
            {
                //[] Verify NEG FindIndex uses the index
                expectedItem = items[0];
                index = list.FindIndex(1, items.Length - 1, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_548797ahjid Verify NEG FindIndex uses the index "

                //[] Verify NEG FindIndex uses the count
                expectedItem = items[items.Length - 1];
                index = list.FindIndex(0, items.Length - 1, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_7894ahoid Verify NEG FindIndex uses the count "
            }

            if (1 < items.Length)
            {
                //[] Verify POS FindIndex uses the index
                expectedItem = items[1];
                index = list.FindIndex(1, items.Length - 1, expectedItemDelegate);
                Assert.Equal(1, index); //"Err_68797ahid Verify POS FindIndex uses the index"

                //[] Verify POS FindIndex uses the count
                expectedItem = items[items.Length - 2];
                index = list.FindIndex(0, items.Length - 1, expectedItemDelegate);
                Assert.Equal(items.Length - 2, index); //"Err_28278ahdii Verify POS FindIndex uses the count"

                //[] Verify NEG FindIndex uses the index and count LOWER
                expectedItem = items[0];
                index = list.FindIndex(1, items.Length - 2, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_384984ahjiod Verify NEG FindIndex uses the index and count LOWER "

                //[] Verify NEG FindIndex uses the index and count UPPER
                expectedItem = items[items.Length - 1];
                index = list.FindIndex(1, items.Length - 2, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_1489haidid Verify NEG FindIndex uses the index and count UPPER "

                //[] Verify POS FindIndex uses the index and count LOWER
                expectedItem = items[1];
                index = list.FindIndex(1, items.Length - 2, expectedItemDelegate);
                Assert.Equal(1, index); //"Err_604890ahjid Verify POS FindIndex uses the index and count LOWER "

                //[] Verify POS FindIndex uses the index and count UPPER
                expectedItem = items[items.Length - 2];
                index = list.FindIndex(1, items.Length - 2, expectedItemDelegate);
                Assert.Equal(items.Length - 2, index); //"Err_66844ahidd Verify POS FindIndex uses the index and count UPPER "
            }
        }

        private void FindIndexIntInt_VerifyDuplicates(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = delegate (T item) { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                index = list.FindIndex(0, list.Count, expectedItemDelegate);
                Assert.Equal(0, index); //"Err_3282iahid Verify first item is duplicated"

                //[] Verify first item is duplicated and index=1
                expectedItem = items[0];
                index = list.FindIndex(1, list.Count - 1, expectedItemDelegate);
                Assert.Equal(items.Length, index); //"Err_8588ahidi Verify first item is duplicated and index=1"
            }

            if (1 < items.Length)
            {
                //[] Verify second item is duplicated
                expectedItem = items[1];
                index = list.FindIndex(0, list.Count, expectedItemDelegate);
                Assert.Equal(1, index); //"Err_29892adewiu Verify second item is duplicated"

                //[] Verify second item is duplicated and index=2
                expectedItem = items[1];
                index = list.FindIndex(2, list.Count - 2, expectedItemDelegate);
                Assert.Equal(items.Length + 1, index); //"Err_1580ahisdf Verify second item is duplicated and index=2"
            }
        }

        #endregion

        #region FindLastIndex

        public void FindLastIndex_Verify(T[] items)
        {
            FindLastIndex_VerifyVanilla(items);
            FindLastIndex_VerifyDuplicates(items);
        }

        private void FindLastIndex_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = (T item) =>
            {
                return expectedItem == null
                    ? item == null : expectedItem.Equals(item);
            };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                index = list.FindLastIndex(expectedItemDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindLastIndex to return the same."
            }

            //[] Verify FindLastIndex returns 0 if the match returns true on every item
            int expected = items.Length == 0 ? -1 : items.Length - 1;
            index = list.FindLastIndex((T item) => { return true; });
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindLastIndex returns 0 if the match returns true on every item"

            //[] Verify FindLastIndex returns -1 if the match returns false on every item
            index = list.FindLastIndex((T item) => { return false; });
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindLastIndex returns -1 if the match returns false on every item"
        }

        private void FindLastIndex_VerifyDuplicates(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                index = list.FindLastIndex(expectedItemDelegate);
                Assert.Equal(items.Length, index); //"Err_3282iahid Verify first item is duplicated"
            }

            if (1 < items.Length)
            {
                //[] Verify second item is duplicated
                expectedItem = items[1];
                index = list.FindLastIndex(expectedItemDelegate);
                Assert.Equal(items.Length + 1, index); //"Err_29892adewiu Verify second item is duplicated."
            }
        }

        #endregion

        #region FindLastIndex(int, pred<T>)

        public void FindLastIndexInt_Verify(T[] items)
        {
            FindLastIndexInt_VerifyVanilla(items);
            FindLastIndexInt_VerifyDuplicates(items);
        }

        public void FindLastIndexInt_VerifyExceptions(T[] items)
        {
            List<T> list = new List<T>();
            Predicate<T> predicate = (T item) => { return true; };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.FindLastIndex(0, null)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            /******************************************************************************
            index
            ******************************************************************************/
            //[] Verify index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(Int32.MinValue, predicate)); //"Err_948ahid Expected index=Int32.MinValue to throw ArgumentOutOfRangeException"

            if (0 < list.Count)
            {
                //[] Verify index=-1
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(-1, predicate)); //"Err_328ahuaw Expected index=-1 to throw ArgumentOutOfRangeException"
            }

            //[] Verify index=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count + 1, predicate)); //"Err_488ajdi Expected index=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count, predicate)); //"Err_9689ajis Expected index=list.Count to throw ArgumentOutOfRangeException"

            //[] Verify index=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(Int32.MaxValue, predicate)); //"Err_238ajwisa Expected index=Int32.MaxValue to throw ArgumentOutOfRangeException"
        }

        private void FindLastIndexInt_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                index = list.FindLastIndex(items.Length - 1, expectedItemDelegate);
                Assert.Equal(i, index); //"Err_282308ahid Expected FindLastIndex to return the same."
            }

            //[] Verify FindLastIndex returns 0 if the match returns true on every item
            index = list.FindLastIndex(items.Length - 1, (T item) => { return true; });
            int expected = items.Length == 0 ? -1 : items.Length - 1;
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindLastIndex returns 0 if the match returns true on every item"

            //[] Verify FindLastIndex returns -1 if the match returns false on every item
            index = list.FindLastIndex(items.Length - 1, (T item) => { return false; });
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindLastIndex returns -1 if the match returns false on every item"

            //[] Verify FindLastIndex returns 0 if the index == 0
            expected = 0 < items.Length ? items.Length - 1 : -1;
            index = list.FindLastIndex(items.Length - 1, (T item) => { return true; });
            Assert.Equal(expected, index); //"Err_4858ajodoa Verify FindLastIndex returns 0 if the index == 0 "

            if (1 < items.Length)
            {
                //[] Verify NEG FindLastIndex uses the index
                expectedItem = items[items.Length - 1];
                index = list.FindLastIndex(items.Length - 2, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_548797ahjid Verify NEG FindLastIndex uses the index"

                //[] Verify POS FindLastIndex uses the index LOWER
                expectedItem = items[0];
                index = list.FindLastIndex(items.Length - 2, expectedItemDelegate);
                Assert.Equal(0, index); //"Err_68797ahid Verify POS FindLastIndex uses the index LOWER"

                //[] Verify POS FindLastIndex uses the index UPPER
                expectedItem = items[items.Length - 2];
                expected = items.Length - 2;
                index = list.FindLastIndex(items.Length - 2, expectedItemDelegate);
                Assert.Equal(expected, index); //"Err_51488ajod Verify POS FindLastIndex uses the index UPPER"
            }
        }

        private void FindLastIndexInt_VerifyDuplicates(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                index = list.FindLastIndex(list.Count - 1, expectedItemDelegate);
                Assert.Equal(items.Length, index); //"Err_3282iahid Verify first item is duplicated"

                //[] Verify first item is duplicated and index is on less then the index of the last duplicate
                expectedItem = items[0];
                index = list.FindLastIndex(items.Length - 1, expectedItemDelegate);
                Assert.Equal(0, index); //"Err_8588ahidi Verify first item is duplicated and index is on less then the index of the last duplicate"
            }

            if (1 < items.Length)
            {
                //[] Verify second item is duplicated
                expectedItem = items[1];
                index = list.FindLastIndex(list.Count - 1, expectedItemDelegate);
                Assert.Equal(list.Count - 1, index); //"Err_29892adewiu Verify second item is duplicated"

                //[] Verify second item is duplicated and index is on less then the index of the last duplicate
                expectedItem = items[1];
                index = list.FindLastIndex(list.Count - 3, expectedItemDelegate);
                Assert.Equal(1, index); //"Err_1580ahisdf Verify second item is duplicated and index is on less then the index of the last duplicate"
            }
        }

        #endregion

        #region FindLastIndex(int, int, pred<T>)

        public void FindLastIndexIntInt_Verify(T[] items)
        {
            FindLastIndexIntInt_VerifyVanilla(items);
            FindLastIndexIntInt_VerifyDuplicates(items);
        }

        public void FindLastIndexIntInt_VerifyExceptions(T[] items)
        {
            List<T> list = new List<T>();
            Predicate<T> predicate = (T item) => { return true; };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify Null match
            Assert.Throws<ArgumentNullException>(() => list.FindLastIndex(0, 0, null)); //"Err_858ahia Expected null match to throw ArgumentNullException"

            /******************************************************************************
            index
            ******************************************************************************/
            //[] Verify index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(Int32.MinValue, 0, predicate)); //Err_948ahid Expected index=Int32.MinValue to throw ArgumentOutOfRangeException"

            if (0 < list.Count)
            {
                //[] Verify index=-1
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(-1, 0, predicate)); //"Err_328ahuaw Expected index=-1 to throw ArgumentOutOfRangeException"
            }

            //[] Verify index=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count + 1, 0, predicate)); //"Err_488ajdi Expected index=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify index=list.Count
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count, 1, predicate)); //"Err_9689ajis Expected index=list.Count to throw ArgumentOutOfRangeException"

            //[] Verify index=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(Int32.MaxValue, 0, predicate)); //"Err_238ajwisa Expected index=Int32.MaxValue to throw ArgumentOutOfRangeException"

            /******************************************************************************
            count
            ******************************************************************************/
            //[] Verify count=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count - 1, Int32.MinValue, predicate)); //"Err_948ahid Expected count=Int32.MinValue to throw ArgumentOutOfRangeException"

            //[] Verify count=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count - 1, -1, predicate)); //"Err_328ahuaw Expected count=-1 to throw ArgumentOutOfRangeException"

            //[] Verify count=list.Count + 1
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count - 1, list.Count + 1, predicate)); //"Err_488ajdi Expected count=list.Count + 1 to throw ArgumentOutOfRangeException"

            //[] Verify count=Int32.MaxValue
            Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(list.Count - 1, Int32.MaxValue, predicate)); //"Err_238ajwisa Expected count=Int32.MaxValue to throw ArgumentOutOfRangeException"

            /******************************************************************************
            index and count
            ******************************************************************************/
            if (0 < items.Length)
            {
                //[] Verify index=1 count=list.Length
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(items.Length - 2, items.Length, predicate)); //"Err_018188avbiw Expected index=1 count=list.Length to throw ArgumentOutOfRangeException"

                //[] Verify index=0 count=list.Length + 1
                Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(items.Length - 1, items.Length + 1, predicate)); //"Err_6848ajiodxbz Expected index=0 count=list.Length + 1 to throw ArgumentOutOfRangeException"
            }
        }

        private void FindLastIndexIntInt_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            for (int i = 0; i < items.Length; ++i)
                list.Add(items[i]);

            //[] Verify FinIndex returns the correct index
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                index = list.FindLastIndex(items.Length - 1, items.Length, (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); });
                Assert.Equal(i, index); //"Err_282308ahid Expected FindLastIndex to be the same."
            }

            //[] Verify FindLastIndex returns 0 if the match returns true on every item
            int expected = items.Length == 0 ? -1 : items.Length - 1;
            index = list.FindLastIndex(items.Length - 1, items.Length, (T item) => { return true; });
            Assert.Equal(expected, index); //"Err_15198ajid Verify FindLastIndex returns 0 if the match returns true on every item"

            //[] Verify FindLastIndex returns -1 if the match returns false on every item
            index = list.FindLastIndex(items.Length - 1, items.Length, (T item) => { return false; });
            Assert.Equal(-1, index); //"Err_305981ajodd Verify FindLastIndex returns -1 if the match returns false on every item"

            if (0 < items.Length)
            {
                //[] Verify FindLastIndex returns -1 if the index == 0
                index = list.FindLastIndex(0, 0, (T item) => { return true; });
                Assert.Equal(-1, index); //"Err_298298ahdi Verify FindLastIndex returns -1 if the index=0"

                //[] Verify NEG FindLastIndex uses the count
                expectedItem = items[0];
                index = list.FindLastIndex(items.Length - 1, items.Length - 1, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_7894ahoid Verify NEG FindLastIndex uses the count"
            }

            if (1 < items.Length)
            {
                //[] Verify NEG FindLastIndex uses the index
                expectedItem = items[items.Length - 1];
                index = list.FindLastIndex(items.Length - 2, items.Length - 1, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_548797ahjid Verify NEG FindLastIndex uses the index"

                //[] Verify POS FindLastIndex uses the index
                expectedItem = items[items.Length - 2];
                index = list.FindLastIndex(items.Length - 2, items.Length - 1, expectedItemDelegate);
                Assert.Equal(items.Length - 2, index); //"Err_68797ahid Verify POS FindLastIndex uses the index"

                //[] Verify POS FindLastIndex uses the count
                expectedItem = items[items.Length - 2];
                index = list.FindLastIndex(items.Length - 1, items.Length - 1, expectedItemDelegate);
                Assert.Equal(items.Length - 2, index); //"Err_28278ahdii Verify POS FindLastIndex uses the count"

                //[] Verify NEG FindLastIndex uses the index and count LOWER
                expectedItem = items[0];
                index = list.FindLastIndex(items.Length - 2, items.Length - 2, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_384984ahjiod Verify NEG FindLastIndex uses the index and count LOWER"

                //[] Verify NEG FindLastIndex uses the index and count UPPER
                expectedItem = items[items.Length - 1];
                index = list.FindLastIndex(items.Length - 2, items.Length - 2, expectedItemDelegate);
                Assert.Equal(-1, index); //"Err_1489haidid Verify NEG FindLastIndex uses the index and count UPPER"

                //[] Verify POS FindLastIndex uses the index and count LOWER
                expectedItem = items[1];
                index = list.FindLastIndex(items.Length - 2, items.Length - 2, expectedItemDelegate);
                Assert.Equal(1, index); //"Err_604890ahjid Verify POS FindLastIndex uses the index and count LOWER"

                //[] Verify POS FindLastIndex uses the index and count UPPER
                expectedItem = items[items.Length - 2];
                index = list.FindLastIndex(items.Length - 2, items.Length - 2, expectedItemDelegate);
                Assert.Equal(items.Length - 2, index); //"Err_66844ahidd Verify POS FindLastIndex uses the index and count UPPER"
            }
        }

        private void FindLastIndexIntInt_VerifyDuplicates(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            int index;
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                index = list.FindLastIndex(list.Count - 1, list.Count, expectedItemDelegate);
                Assert.Equal(items.Length, index); //"Err_3282iahid Verify first item is duplicated"

                //[] Verify first item is duplicated and  index is on less then the index of the last duplicate
                expectedItem = items[0];
                index = list.FindLastIndex(items.Length - 1, items.Length, expectedItemDelegate);
                Assert.Equal(0, index); //"Err_8588ahidi Verify first item is duplicated and  index is on less then the index of the last duplicate"
            }

            if (1 < items.Length)
            {
                //[] Verify second item is duplicated
                expectedItem = items[1];
                index = list.FindLastIndex(list.Count - 1, list.Count, expectedItemDelegate);
                Assert.Equal(items.Length + 1, index); //"Err_29892adewiu Verify second item is duplicated"

                //[] Verify second item is duplicated and  index is on less then the index of the last duplicate
                expectedItem = items[1];
                index = list.FindLastIndex(items.Length - 1, items.Length, expectedItemDelegate);
                Assert.Equal(1, index); //"Err_1580ahisdf Verify second item is duplicated and  index is on less then the index of the last duplicate"
            }
        }

        #endregion

        #region FindAll

        public void FindAll_Verify(T[] items)
        {
            FindAll_VerifyVanilla(items);
            FindAll_VerifyDuplicates(items);
        }

        public void FindAll_Verify(T[] listItems, T[] expectedItems, Predicate<T> predicate)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < listItems.Length; ++i)
                list.Add(listItems[i]);
            VerifyList(list.FindAll(predicate), expectedItems);
        }

        private void FindAll_VerifyVanilla(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };
            T[] matchingItems = new T[items.Length - (items.Length / 2)];

            for (int i = 0; i < items.Length; ++i)
            {
                list.Add(items[i]);

                if ((i & 1) == 0)
                    matchingItems[i / 2] = items[i];
            }

            //[] Verify FindAll returns the correct List with a match that matches every other item
            VerifyList(list.FindAll((T item) => { return -1 != Array.IndexOf(matchingItems, item); }), matchingItems);

            //[] Verify FindAll returns the correct List with one item
            for (int i = 0; i < items.Length; ++i)
            {
                expectedItem = items[i];
                VerifyList(list.FindAll(expectedItemDelegate), new T[] { expectedItem });
            }

            //[] Verify FindAll returns an List with all of the items if the predicate always returns true
            VerifyList(list.FindAll((T item) => { return true; }), items);

            //[] Verify FindAll returns an empty List if the match returns false on every item
            VerifyList(list.FindAll((T item) => { return false; }), new T[0]);
        }

        private void FindAll_VerifyDuplicates(T[] items)
        {
            T expectedItem = default(T);
            List<T> list = new List<T>();
            Predicate<T> expectedItemDelegate = (T item) => { return expectedItem == null ? item == null : expectedItem.Equals(item); };
            T[] matchingItems = new T[items.Length - (items.Length / 2)];
            T[] expectedItems;

            if (0 < items.Length)
            {
                for (int i = 0; i < items.Length; ++i)
                    list.Add(items[i]);

                for (int i = 0; i < items.Length && i < 2; ++i)
                    list.Add(items[i]);

                //[] Verify first item is duplicated
                expectedItem = items[0];
                VerifyList(list.FindAll(expectedItemDelegate), new T[] { expectedItem, expectedItem });

                if (1 < items.Length)
                {
                    //[] Verify second item is duplicated
                    expectedItem = items[1];
                    VerifyList(list.FindAll(expectedItemDelegate), new T[] { expectedItem, expectedItem });
                }

                //[] Verify FinIndex returns the correct List with a match that matches every other item with duplicates
                list.Clear();

                for (int i = 0; i < items.Length; ++i)
                {
                    list.Add(items[i]);

                    if ((i & 1) == 0)
                        matchingItems[i / 2] = items[i];
                }

                for (int i = 0; i < matchingItems.Length; ++i)
                    list.Add(matchingItems[i]);

                expectedItems = new T[matchingItems.Length * 2];
                Array.Copy(matchingItems, 0, expectedItems, 0, matchingItems.Length);
                Array.Copy(matchingItems, 0, expectedItems, matchingItems.Length, matchingItems.Length);

                VerifyList(list.FindAll((T item) => { return -1 != Array.IndexOf(matchingItems, item); }), expectedItems);
            }
        }

        private void VerifyList(List<T> list, T[] expectedItems)
        {
            Assert.Equal(expectedItems.Length, list.Count); //"Err_2828ahid Expected the smae lengths."

            //Only verify the indexer. List should be in a good enough state that we
            //do not have to verify consistancy with any other method.
            for (int i = 0; i < list.Count; ++i)
                Assert.Equal(expectedItems[i], list[i]); //"Err_19818ayiadb Expceted the same items at index: " + i
        }

        #endregion
    }
    public class List_FindTests
    {
        [Fact]
        public static void Find_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.Find_Verify(new int[0]);
            intDriver.Find_Verify(new int[] { 4 });
            intDriver.Find_Verify(intArray);

            stringDriver.Find_Verify(new string[0]);
            stringDriver.Find_Verify(new string[] { "7" });
            stringDriver.Find_Verify(stringArray);
        }

        [Fact]
        public static void FindLast_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.FindLast_Verify(new int[0]);
            intDriver.FindLast_Verify(new int[] { 4 });
            intDriver.FindLast_Verify(intArray);

            stringDriver.FindLast_Verify(new string[0]);
            stringDriver.FindLast_Verify(new string[] { "7" });
            stringDriver.FindLast_Verify(stringArray);
        }

        [Fact]
        public static void FindLastIndex_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.FindLastIndex_Verify(new int[0]);
            intDriver.FindLastIndex_Verify(new int[] { 4 });
            intDriver.FindLastIndex_Verify(intArray);

            stringDriver.FindLastIndex_Verify(new string[0]);
            stringDriver.FindLastIndex_Verify(new string[] { "7" });
            stringDriver.FindLastIndex_Verify(stringArray);
        }

        [Fact]
        public static void FindLastIndexInt_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                stringArray[i] = i.ToString();
            }

            intDriver.FindLastIndexInt_Verify(new int[0]);
            intDriver.FindLastIndexInt_Verify(new int[] { 1 });
            intDriver.FindLastIndexInt_Verify(intArray);

            stringDriver.FindLastIndexInt_Verify(new string[0]);
            stringDriver.FindLastIndexInt_Verify(new string[] { "1" });
            stringDriver.FindLastIndexInt_Verify(stringArray);
        }

        [Fact]
        public static void FindLastIndexInt_Tests_Negative()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                stringArray[i] = i.ToString();
            }

            intDriver.FindLastIndexInt_VerifyExceptions(intArray);
            stringDriver.FindLastIndexInt_VerifyExceptions(stringArray);
        }

        [Fact]
        public static void FindLastIndexIntInt_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                stringArray[i] = i.ToString();
            }

            intDriver.FindLastIndexIntInt_Verify(new int[0]);
            intDriver.FindLastIndexIntInt_Verify(new int[] { 1 });
            intDriver.FindLastIndexIntInt_Verify(intArray);

            stringDriver.FindLastIndexIntInt_Verify(new string[0]);
            stringDriver.FindLastIndexIntInt_Verify(new string[] { "1" });
            stringDriver.FindLastIndexIntInt_Verify(stringArray);
        }

        [Fact]
        public static void FindLastIndexIntInt_Tests_Negative()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                stringArray[i] = i.ToString();
            }

            intDriver.FindLastIndexIntInt_VerifyExceptions(intArray);
            stringDriver.FindLastIndexIntInt_VerifyExceptions(stringArray);
        }

        [Fact]
        public static void FindAll_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                stringArray[i] = i.ToString();
            }

            intDriver.FindAll_Verify(new int[0]);
            intDriver.FindAll_Verify(new int[] { 4 });
            intDriver.FindAll_Verify(intArray);
            intDriver.FindAll_Verify(new int[] { 0, 1, 2, 4 }, new int[] { 1, 2 }, delegate (int item) { return item == 1 | item == 2; });

            stringDriver.FindAll_Verify(new string[0]);
            stringDriver.FindAll_Verify(new string[] { "7" });
            stringDriver.FindAll_Verify(stringArray);
            stringDriver.FindAll_Verify(new string[] { "0", "1", "2", "4" }, new string[] { "1", "2" }, delegate (string item) { return item == "1" | item == "2"; });
        }

        [Fact]
        public static void FindIndex_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                stringArray[i] = i.ToString();
            }

            intDriver.FindIndex_Verify(new int[0]);
            intDriver.FindIndex_Verify(new int[] { 1 });
            intDriver.FindIndex_Verify(intArray);

            stringDriver.FindIndex_Verify(new string[0]);
            stringDriver.FindIndex_Verify(new string[] { "1" });
            stringDriver.FindIndex_Verify(stringArray);
        }

        [Fact]
        public static void FindIndexInt_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.FindIndexInt_Verify(new int[0]);
            intDriver.FindIndexInt_Verify(new int[] { 4 });
            intDriver.FindIndexInt_Verify(intArray);

            stringDriver.FindIndexInt_Verify(new string[0]);
            stringDriver.FindIndexInt_Verify(new string[] { "7" });
            stringDriver.FindIndexInt_Verify(stringArray);
        }

        [Fact]
        public static void FindIndexInt_Tests_Negative()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                stringArray[i] = i.ToString();
            }

            intDriver.FindIndexInt_VerifyExceptions(intArray);
            stringDriver.FindIndexInt_VerifyExceptions(stringArray);
        }

        [Fact]
        public static void FindIndexIntInt_Tests()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.FindIndexIntInt_Verify(new int[0]);
            intDriver.FindIndexIntInt_Verify(new int[] { 4 });
            intDriver.FindIndexIntInt_Verify(intArray);

            stringDriver.FindIndexIntInt_Verify(new string[0]);
            stringDriver.FindIndexIntInt_Verify(new string[] { "7" });
            stringDriver.FindIndexIntInt_Verify(stringArray);
        }

        [Fact]
        public static void FindIndexIntInt_Tests_Negative()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int arraySize = 16;
            int[] intArray = new int[arraySize];
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i;
                stringArray[i] = i.ToString();
            }

            intDriver.FindIndexIntInt_VerifyExceptions(intArray);
            stringDriver.FindIndexIntInt_VerifyExceptions(stringArray);
        }

        [Fact]
        public static void Find_Tests_Negative()
        {
            Driver<int> intDriver = new Driver<int>();
            Driver<string> stringDriver = new Driver<string>();
            int[] intArray;
            string[] stringArray;
            int arraySize = 16;

            intArray = new int[arraySize];
            stringArray = new string[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                intArray[i] = i + 1;
                stringArray[i] = (i + 1).ToString();
            }

            intDriver.FindVerifyExceptions(intArray);
            stringDriver.FindVerifyExceptions(stringArray);
        }
    }
}
