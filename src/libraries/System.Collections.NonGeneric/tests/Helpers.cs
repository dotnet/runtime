// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Collections.Tests
{
    internal static class Helpers
    {
        public static void PerformActionOnAllHashtableWrappers(Hashtable hashtable, Action<Hashtable> action)
        {
            // Synchronized returns a slightly different version of Hashtable
            Hashtable[] hashtableTypes =
            {
                (Hashtable)hashtable.Clone(),
                Hashtable.Synchronized(hashtable)
            };

            foreach (Hashtable hashtableType in hashtableTypes)
            {
                action(hashtableType);
            }
        }

        public static Hashtable CreateIntHashtable(int count, int start = 0)
        {
            var hashtable = new Hashtable();

            for (int i = start; i < start + count; i++)
            {
                hashtable.Add(i, i);
            }

            return hashtable;
        }

        public static Hashtable CreateStringHashtable(int count, int start = 0)
        {
            var hashtable = new Hashtable();

            for (int i = start; i < start + count; i++)
            {
                string key = "Key_" + i;
                string value = "Value_" + i;

                hashtable.Add(key, value);
            }

            return hashtable;
        }

        public static void PerformActionOnAllArrayListWrappers(ArrayList arrList, Action<ArrayList> action)
        {
            // Adapter, GetRange, Synchronized, ReadOnly returns a slightly different version of ArrayList.
            // The following variable contains each one of these types of array lists
            ArrayList[] arrayListTypes =
            {
                (ArrayList)arrList.Clone(),
                (ArrayList)arrList.GetRange(0, arrList.Count).Clone(),
                (ArrayList)ArrayList.Adapter(arrList).Clone(),
                (ArrayList)ArrayList.FixedSize(arrList).Clone(),
                (ArrayList)ArrayList.ReadOnly(arrList).Clone(),
                (ArrayList)ArrayList.Synchronized(arrList).Clone()
            };  

            foreach (ArrayList arrListType in arrayListTypes)
            {
                action(arrListType);
            }
        }

        public static ArrayList CreateStringArrayList(int count, int start = 0, string optionalString = null)
        {
            var arrayList = new ArrayList();

            for (int i = start; i < start + count; i++)
            {
                arrayList.Add(i.ToString() + optionalString);
            }

            return arrayList;
        }

        public static ArrayList CreateIntArrayList(int count, int start = 0)
        {
            var arrayList = new ArrayList();

            for (int i = start; i < start + count; i++)
            {
                arrayList.Add(i);
            }

            return arrayList;
        }

        public static void PerformActionOnAllQueueWrappers(Queue queue, Action<Queue> action)
        {
            // Synchronized returns a slightly different version of Queue
            Queue[] queueTypes =
            {
                (Queue)queue.Clone(),
                Queue.Synchronized(queue)
            };

            foreach (Queue queueType in queueTypes)
            {
                action(queueType);
            }
        }

        public static Queue CreateIntQueue(int count, int start = 0)
        {
            var queue = new Queue();

            for (int i = start; i < start + count; i++)
            {
                queue.Enqueue(i);
            }

            return queue;
        }

        public static void PerformActionOnAllStackWrappers(Stack stack, Action<Stack> action)
        {
            // Synchronized returns a slightly different version of Stack
            Stack[] stackTypes =
            {
                (Stack)stack.Clone(),
                Stack.Synchronized(stack)
            };

            foreach (Stack stackType in stackTypes)
            {
                action(stackType);
            }
        }

        public static Stack CreateIntStack(int count, int start = 0)
        {
            var stack = new Stack();

            for (int i = start; i < start + count; i++)
            {
                stack.Push(i);
            }

            return stack;
        }

        public static void PerformActionOnAllSortedListWrappers(SortedList sortedList, Action<SortedList> action)
        {
            // Synchronized returns a slightly different version of Stack
            SortedList[] sortedListTypes =
            {
                (SortedList)sortedList.Clone(),
                SortedList.Synchronized(sortedList)
            };

            foreach (SortedList sortedListType in sortedListTypes)
            {
                action(sortedListType);
            }
        }

        public static SortedList CreateIntSortedList(int count, int start = 0)
        {
            var sortedList = new SortedList();

            for (int i = start; i < start + count; i++)
            {
                sortedList.Add(i, i);
            }

            return sortedList;
        }

        public static SortedList CreateStringSortedList(int count, int start = 0)
        {
            var sortedList = new SortedList();

            for (int i = start; i < start + count; i++)
            {
                sortedList.Add("Key_" + i.ToString("D2"), "Value_" + i);
            }

            return sortedList;
        }

        public static int[] CreateIntArray(int count, int start = 0)
        {
            var array = new int[count];

            for (int i = start; i < start + count; i++)
            {
                array[i] = i;
            }

            return array;
        }
    }
}
