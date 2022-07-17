// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;
using System.Globalization;
using System.Collections;

namespace MS.Internal.Xml.XPath
{
    internal sealed class XPathMultyIterator : ResettableIterator
    {
        private ResettableIterator[] arr;
        private int firstNotEmpty;
        private int position;

        public XPathMultyIterator(ArrayList inputArray)
        {
            // NOTE: We do not clone the passed inputArray supposing that it is not changed outside of this class
            this.arr = new ResettableIterator[inputArray.Count];
            for (int i = 0; i < this.arr.Length; i++)
            {
                var iterator = (ArrayList?)inputArray[i];
                Debug.Assert(iterator != null);
                this.arr[i] = new XPathArrayIterator(iterator);
            }
            Init();
        }

        private void Init()
        {
            for (int i = 0; i < arr.Length; i++)
            {
                Advance(i);
            }
            for (int i = arr.Length - 2; firstNotEmpty <= i;)
            {
                if (SiftItem(i))
                {
                    i--;
                }
            }
        }

        // returns false is iterator at pos reached it's end & as a result head of the array may be moved
        private bool Advance(int pos)
        {
            if (!arr[pos].MoveNext())
            {
                if (firstNotEmpty != pos)
                {
                    ResettableIterator empty = arr[pos];
                    Array.Copy(arr, firstNotEmpty, arr, firstNotEmpty + 1, pos - firstNotEmpty);
                    arr[firstNotEmpty] = empty;
                }
                firstNotEmpty++;
                return false;
            }
            return true;
        }


        // Invariant: a[i] < a[i+1] for i > item
        // returns flase is head of the list was moved & as a result consistancy of list depends on head consistancy.
        private bool SiftItem(int item)
        {
            Debug.Assert(firstNotEmpty <= item && item < arr.Length);
            ResettableIterator it = arr[item];
            while (item + 1 < arr.Length)
            {
                ResettableIterator itNext = arr[item + 1];
                Debug.Assert(it.Current != null && itNext.Current != null);
                XmlNodeOrder order = Query.CompareNodes(it.Current, itNext.Current);
                if (order == XmlNodeOrder.Before)
                {
                    break;
                }
                if (order == XmlNodeOrder.After)
                {
                    arr[item] = itNext;
                    //arr[item + 1] = it;
                    item++;
                }
                else
                { // Same
                    arr[item] = it;
                    if (!Advance(item))
                    {
                        return false;
                    }
                    it = arr[item];
                }
            }
            arr[item] = it;
            return true;
        }

        public override void Reset()
        {
            firstNotEmpty = 0;
            position = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i].Reset();
            }
            Init();
        }

        public XPathMultyIterator(XPathMultyIterator it)
        {
            this.arr = (ResettableIterator[])it.arr.Clone();
            this.firstNotEmpty = it.firstNotEmpty;
            this.position = it.position;
        }

        public override XPathNodeIterator Clone()
        {
            return new XPathMultyIterator(this);
        }

        public override XPathNavigator? Current
        {
            get
            {
                Debug.Assert(position != 0, "MoveNext() wasn't called");
                Debug.Assert(firstNotEmpty < arr.Length, "MoveNext() returned false");
                return arr[firstNotEmpty].Current;
            }
        }

        public override int CurrentPosition { get { return position; } }

        public override bool MoveNext()
        {
            // NOTE: MoveNext() may be called even if the previous call to MoveNext() returned false, SQLBUDT 330810
            if (firstNotEmpty >= arr.Length)
            {
                return false;
            }
            if (position != 0)
            {
                if (Advance(firstNotEmpty))
                {
                    SiftItem(firstNotEmpty);
                }
                if (firstNotEmpty >= arr.Length)
                {
                    return false;
                }
            }
            position++;
            return true;
        }
    }
}
