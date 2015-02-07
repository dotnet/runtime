// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


/*** The goal of this program is to call the methods of the base class on the wrapper
    classes to ensure that the right thing is happening in ArrayList
 */

using System;
using System.Collections;
using Xunit;

public class WrapperTests
{
    [Fact]
    public void TestFixedSize()
    {
        ArrayList alst = null;
        ArrayList tst = null;
        IList ilst1 = null;
        IList ilst2 = null;

        Hashtable hsh1 = null;

        //[] Adapter
        alst = new ArrayList();
        tst = ArrayList.Adapter(alst);
        hsh1 = new Hashtable();

        CompareObjects(alst, tst, hsh1);

        // We should always do a string comparison
        Assert.True(hsh1.Count < 3);
        Assert.Equal("get", (string)hsh1["Capacity"]);
        Assert.Equal("Monekeyed, 301 301", (string)hsh1["TrimToSize"]);

        alst = new ArrayList();
        for (int i = 0; i < 100; i++)
            alst.Add(i);
        tst = ArrayList.Adapter(alst);

        hsh1 = new Hashtable();
        CompareObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 3);
        Assert.Equal("get", (string)hsh1["Capacity"]);
        Assert.Equal("Monekeyed, 301 301", (string)hsh1["TrimToSize"]);

        //[] FixedSize - ArrayList
        alst = new ArrayList();
        tst = ArrayList.FixedSize(alst);

        hsh1 = new Hashtable();
        CompareObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("Fixed"));

        alst = new ArrayList();
        for (int i = 0; i < 100; i++)
            alst.Add(i);
        tst = ArrayList.FixedSize(alst);

        hsh1 = new Hashtable();
        CompareObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("Fixed"));

        //[] FixedSize - IList
        ilst1 = new ArrayList();
        ilst2 = ArrayList.FixedSize(ilst1);

        hsh1 = new Hashtable();
        DoIListTests(ilst1, ilst2, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("Fixed"));

        ilst1 = new ArrayList();
        for (int i = 0; i < 100; i++)
        {
            ilst1.Add(i);
        }
        ilst2 = ArrayList.FixedSize(ilst1);

        hsh1 = new Hashtable();
        DoIListTests(ilst1, ilst2, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("Fixed"));
    }

    [Fact]
    public void TestReadOnly()
    {
        //[] ReadOnly - ArrayList
        ArrayList alst = new ArrayList();
        ArrayList tst = ArrayList.ReadOnly(alst);

        Hashtable hsh1 = new Hashtable();
        CompareObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 3);
        Assert.True(hsh1.ContainsKey("IsReadOnly"));
        Assert.Equal("Exception not thrown, (Object, IComparer)", (string)hsh1["BinarySearch"]);

        alst = new ArrayList();
        for (int i = 0; i < 100; i++)
            alst.Add(i);
        tst = ArrayList.ReadOnly(alst);

        hsh1 = new Hashtable();
        CompareObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 3);
        Assert.True(hsh1.ContainsKey("IsReadOnly"));
        Assert.Equal("Exception not thrown, (Object, IComparer)", (string)hsh1["BinarySearch"]);

        //[] ReadOnly - IList
        IList ilst1 = new ArrayList();
        IList ilst2 = ArrayList.ReadOnly(ilst1);

        hsh1 = new Hashtable();
        DoIListTests(ilst1, ilst2, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("IsReadOnly"));

        ilst1 = new ArrayList();
        for (int i = 0; i < 100; i++)
        {
            ilst1.Add(i);
        }
        ilst2 = ArrayList.ReadOnly(ilst1);

        hsh1 = new Hashtable();
        DoIListTests(ilst1, ilst2, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("IsReadOnly"));
    }

    [Fact]
    public void TestSynchronized()
    {
        //[] Synchronized - ArrayList
        ArrayList alst = new ArrayList();
        ArrayList tst = ArrayList.Synchronized(alst);

        Hashtable hsh1 = new Hashtable();
        CompareObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("IsSynchronized"));

        alst = new ArrayList();
        for (int i = 0; i < 100; i++)
            alst.Add(i);
        tst = ArrayList.Synchronized(alst);

        hsh1 = new Hashtable();
        CompareObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("IsSynchronized"));

        //[] Synchronized - IList
        IList ilst1 = new ArrayList();
        IList ilst2 = ArrayList.Synchronized(ilst1);

        hsh1 = new Hashtable();
        DoIListTests(ilst1, ilst2, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("IsSynchronized"));

        ilst1 = new ArrayList();
        for (int i = 0; i < 100; i++)
        {
            ilst1.Add(i);
        }
        ilst2 = ArrayList.Synchronized(ilst1);

        hsh1 = new Hashtable();
        DoIListTests(ilst1, ilst2, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("IsSynchronized"));
    }

    [Fact]
    public void TestRange()
    {
        //[]Range
        //ArrayList changes in any way for all operation in the Range ArrayList.
        //Rather than change CompareObjects which is working for all the other collections, we will implement our version
        //of ComapreObjects for Range
        ArrayList alst = new ArrayList();
        ArrayList tst = alst.GetRange(0, 0);

        Hashtable hsh1 = new Hashtable();
        CompareRangeObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("TrimToSize"));

        alst = new ArrayList();
        for (int i = 0; i < 100; i++)
            alst.Add(i);
        tst = alst.GetRange(0, 100);

        hsh1 = new Hashtable();
        CompareRangeObjects(alst, tst, hsh1);
        Assert.True(hsh1.Count < 2);
        Assert.True(hsh1.ContainsKey("TrimToSize"));
    }

    [Fact]
    public void TestMultiDataTypes()
    {
        short i16 = 1;
        int i32 = 2;
        long i64 = 3;
        ushort ui16 = 4;
        uint ui32 = 5;
        ulong ui64 = 6;

        ArrayList alst = new ArrayList();
        alst.Add(i16);
        alst.Add(i32);
        alst.Add(i64);
        alst.Add(ui16);
        alst.Add(ui32);
        alst.Add(ui64);

        //[] we make sure that ArrayList only return true for Contains() when both the value and the type match 
        //in numeric types
        for (int i = 0; i < alst.Count; i++)
        {
            Assert.True(!alst.Contains(i) || i == 2);
        }

        //[]IndexOf should also work in this context
        for (int i = 0; i < alst.Count; i++)
        {
            Assert.True((alst.IndexOf(i) != -1) || (i != 2));
            Assert.True((alst.IndexOf(i) == -1) || (i == 2));
        }

        //[]Sort should fail cause the objects are of different types
        Assert.Throws<InvalidOperationException>(() => alst.Sort());
    }

    private void CompareObjects(ArrayList good, ArrayList bad, Hashtable hsh1)
    {
        //IList, this includes ICollection tests as well!!
        DoIListTests(good, bad, hsh1);

        //we will now test ArrayList specific methods
        good.Clear();
        for (int i = 0; i < 100; i++)
            good.Add(i);

        //AL's CopyTo methods
        int[] iArr1 = null;
        int[] iArr2 = null;
        iArr1 = new int[100];
        iArr2 = new int[100];
        good.CopyTo(iArr1);
        bad.CopyTo(iArr2);
        for (int i = 0; i < 100; i++)
        {
            if (iArr1[i] != iArr2[i])
                hsh1["CopyTo"] = "()";
        }

        iArr1 = new int[100];
        iArr2 = new int[100];
        good.CopyTo(0, iArr1, 0, 100);
        try
        {
            bad.CopyTo(0, iArr2, 0, 100);
            for (int i = 0; i < 100; i++)
            {
                if (iArr1[i] != iArr2[i])
                    hsh1["CopyTo"] = "()";
            }
        }
        catch
        {
            hsh1["CopyTo"] = "(int, Array, int, int)";
        }

        iArr1 = new int[200];
        iArr2 = new int[200];
        for (int i = 0; i < 200; i++)
        {
            iArr1[i] = 50;
            iArr2[i] = 50;
        }

        good.CopyTo(50, iArr1, 100, 20);
        try
        {
            bad.CopyTo(50, iArr2, 100, 20);
            for (int i = 0; i < 200; i++)
            {
                if (iArr1[i] != iArr2[i])
                    hsh1["CopyTo"] = "(Array, int, int)";
            }
        }
        catch
        {
            hsh1["CopyTo"] = "(int, Array, int, int)";
        }

        //Clone()
        ArrayList alstClone = (ArrayList)bad.Clone();
        //lets make sure that the clone is what it says it is
        if (alstClone.Count != bad.Count)
            hsh1["Clone"] = "Count";
        for (int i = 0; i < bad.Count; i++)
        {
            if (alstClone[i] != bad[i])
                hsh1["Clone"] = "[]";
        }

        //GerEnumerator()
        IEnumerator ienm1 = null;
        IEnumerator ienm2 = null;

        ienm1 = good.GetEnumerator(0, 100);
        try
        {
            ienm2 = bad.GetEnumerator(0, 100);
            DoIEnumerableTest(ienm1, ienm2, good, bad, hsh1, false);
        }
        catch
        {
            hsh1["GetEnumerator"] = "(int, int)";
        }

        ienm1 = good.GetEnumerator(50, 50);
        try
        {
            ienm2 = bad.GetEnumerator(50, 50);
            DoIEnumerableTest(ienm1, ienm2, good, bad, hsh1, false);
        }
        catch
        {
            hsh1["GetEnumerator"] = "(int, int)";
        }

        try
        {
            bad.GetEnumerator(50, 150);
            hsh1["GetEnumerator"] = "(int, int)";
        }
        catch (Exception)
        {
        }

        ienm1 = good.GetEnumerator(0, 100);
        try
        {
            ienm2 = bad.GetEnumerator(0, 100);
            good.RemoveAt(0);
            DoIEnumerableTest(ienm1, ienm2, good, bad, hsh1, true);
        }
        catch
        {
            hsh1["GetEnumerator"] = "(int, int)";
        }

        //GetRange
        good.Clear();
        for (int i = 0; i < 100; i++)
            good.Add(i);

        ArrayList alst1 = good.GetRange(0, good.Count);
        try
        {
            ArrayList alst2 = bad.GetRange(0, good.Count);
            for (int i = 0; i < good.Count; i++)
            {
                if (alst1[i] != alst2[i])
                    hsh1["GetRange"] = i;
            }
        }
        catch
        {
            hsh1["Range"] = "(int, int)";
        }

        //IndexOf(Object, int)

        if (bad.Count > 0)
        {
            for (int i = 0; i < good.Count; i++)
            {
                if (good.IndexOf(good[i], 0) != bad.IndexOf(good[i], 0))
                {
                    hsh1["IndexOf"] = "(Object, int)";
                }
                if (good.IndexOf(good[i], i) != bad.IndexOf(good[i], i))
                {
                    hsh1["IndexOf"] = "(Object, int)";
                }
                if (i < (good.Count - 1))
                {
                    if (good.IndexOf(good[i], i + 1) != bad.IndexOf(good[i], i + 1))
                    {
                        hsh1["IndexOf"] = "(Object, int)";
                    }
                }
            }

            try
            {
                bad.IndexOf(1, -1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            try
            {
                bad.IndexOf(1, bad.Count);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            // IndexOf(Object, int, int)
            // The semantics of this method has changed, the 3rd parameter now refers to count instead of length
            for (int i = 0; i < good.Count; i++)
            {
                if (good.IndexOf(good[i], 0, good.Count - 1) != bad.IndexOf(good[i], 0, good.Count - 1))
                {
                    hsh1["IndexOf"] = "(Object, int, int)";
                }
                if (good.IndexOf(good[i], i, good.Count - i) != bad.IndexOf(good[i], i, good.Count - i))
                {
                    hsh1["IndexOf"] = "(Object, int)";
                }
                if (good.IndexOf(good[i], i, 0) != bad.IndexOf(good[i], i, 0))
                {
                    hsh1["IndexOf"] = "(Object, int)";
                }
                if (i < (good.Count - 1))
                {
                    if (good.IndexOf(good[i], i + 1, good.Count - (i + 1)) != bad.IndexOf(good[i], i + 1, good.Count - (i + 1)))
                    {
                        hsh1["IndexOf"] = "(Object, int)";
                    }
                }
            }

            try
            {
                bad.IndexOf(1, 0, -1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            try
            {
                bad.IndexOf(1, 0, bad.Count);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            try
            {
                bad.IndexOf(1, bad.Count - 1, bad.Count - 2);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            //LastIndexOf(Object)
            for (int i = 0; i < good.Count; i++)
            {
                if (good.LastIndexOf(good[i]) != bad.LastIndexOf(good[i]))
                {
                    hsh1["LastIndexOf"] = "(Object)";
                }
                if (good.LastIndexOf(i + 1000) != bad.LastIndexOf(i + 1000))
                {
                    hsh1["LastIndexOf"] = "(Object)";
                }
            }

            try
            {
                bad.LastIndexOf(null);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }

            //LastIndexOf(Object, int)
            for (int i = 0; i < good.Count; i++)
            {
                if (good.LastIndexOf(good[i], good.Count - 1) != bad.LastIndexOf(good[i], good.Count - 1))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (good.LastIndexOf(good[i], 0) != bad.LastIndexOf(good[i], 0))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (good.LastIndexOf(good[i], i) != bad.LastIndexOf(good[i], i))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (i < (good.Count - 1))
                {
                    if (good.LastIndexOf(good[i], i + 1) != bad.LastIndexOf(good[i], i + 1))
                    {
                        hsh1["LastIndexOf"] = "(Object, int)";
                    }
                }
            }

            try
            {
                bad.LastIndexOf(1, -1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }

            try
            {
                bad.LastIndexOf(1, bad.Count);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }

            //LastIndexOf(Object, int, int)
            for (int i = 0; i < good.Count; i++)
            {
                if (good.LastIndexOf(good[i], good.Count - 1, 0) != bad.LastIndexOf(good[i], good.Count - 1, 0))
                {
                    hsh1["LastIndexOf"] = "(Object, int, int)";
                }
                if (good.LastIndexOf(good[i], good.Count - 1, i) != bad.LastIndexOf(good[i], good.Count - 1, i))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (good.LastIndexOf(good[i], i, i) != bad.LastIndexOf(good[i], i, i))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (i < (good.Count - 1))
                {
                    if (good.LastIndexOf(good[i], good.Count - 1, i + 1) != bad.LastIndexOf(good[i], good.Count - 1, i + 1))
                    {
                        hsh1["LastIndexOf"] = "(Object, int)";
                    }
                }
            }

            try
            {
                bad.LastIndexOf(1, 1, -1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }

            try
            {
                bad.LastIndexOf(1, bad.Count - 2, bad.Count - 1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }
        }

        //ReadOnly()
        ArrayList alst3 = ArrayList.ReadOnly(bad);
        if (!alst3.IsReadOnly)
            hsh1["ReadOnly"] = "Not";

        IList ilst1 = ArrayList.ReadOnly((IList)bad);
        if (!ilst1.IsReadOnly)
            hsh1["ReadOnly"] = "Not";

        //Synchronized()
        alst3 = ArrayList.Synchronized(bad);
        if (!alst3.IsSynchronized)
            hsh1["Synchronized"] = "Not";

        ilst1 = ArrayList.Synchronized((IList)bad);
        if (!ilst1.IsSynchronized)
            hsh1["Synchronized"] = "Not";

        //ToArray()
        if (good.Count == bad.Count)
        {
            object[] oArr1 = good.ToArray();
            object[] oArr2 = bad.ToArray();
            for (int i = 0; i < good.Count; i++)
            {
                if ((int)oArr1[i] != (int)oArr2[i])
                    hsh1["ToArray"] = "()";
            }

            //ToArray(type)
            iArr1 = (int[])good.ToArray(typeof(int));
            iArr2 = (int[])bad.ToArray(typeof(int));
            for (int i = 0; i < good.Count; i++)
            {
                if (iArr1[i] != iArr2[i])
                    hsh1["ToArray"] = "(Type)";
            }
        }

        //Capacity - get
        if (good.Capacity != bad.Capacity)
        {
            hsh1["Capacity"] = "get";
        }

        //Fixed size methods
        if (!hsh1.ContainsKey("IsReadOnly"))
        {
            good.Clear();
            for (int i = 100; i > 0; i--)
                good.Add(i);
            //Sort() & BinarySearch(Object)
            bad.Sort();
            for (int i = 0; i < bad.Count - 1; i++)
            {
                if ((int)bad[i] > (int)bad[i + 1])
                    hsh1["Sort"] = "()";
            }

            for (int i = 0; i < bad.Count; i++)
            {
                if (bad.BinarySearch(bad[i]) != i)
                    hsh1["BinarySearch"] = "(Object)";
            }

            //Reverse()
            bad.Reverse();
            if (bad.Count > 0)
            {
                for (int i = 0; i < 99; i++)
                {
                    if ((int)bad[i] < (int)bad[i + 1])
                        hsh1["Reverse"] = "()";
                }

                good.Clear();
                for (int i = 100; i > 0; i--)
                    good.Add(i.ToString());
            }

            good.Clear();
            for (int i = 90; i > 64; i--)
                good.Add(((Char)i).ToString());

            try
            {
                bad.Sort(new CaseInsensitiveComparer());
                if (bad.Count > 0)
                {
                    for (int i = 0; i < (bad.Count - 1); i++)
                    {
                        if (((String)bad[i]).CompareTo(((String)bad[i + 1])) >= 0)
                            hsh1["Sort"] = "(IComparer)";
                    }
                    for (int i = 0; i < bad.Count; i++)
                    {
                        if (bad.BinarySearch(bad[i], new CaseInsensitiveComparer()) != i)
                            hsh1["BinarySearch"] = "(Object, IComparer)";
                    }
                }
                bad.Reverse();

                good.Clear();
                for (int i = 65; i < 91; i++)
                    good.Add(((Char)i).ToString());

                if (bad.Count > 0)
                {
                    for (int i = 0; i < good.Count; i++)
                    {
                        if (bad.BinarySearch(0, bad.Count, bad[i], new CaseInsensitiveComparer()) != i)
                            hsh1["BinarySearch"] = "(int, int, Object, IComparer)";
                    }
                }
            }
            catch (Exception)
            {
            }

            good.Clear();
            for (int i = 0; i < 100; i++)
                good.Add(i);

            Queue que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 5000);

            try
            {
                bad.SetRange(0, que);
            }
            catch (Exception ex)
            {
                hsh1["SetRange"] = "Copy_ExceptionType, " + ex.GetType().Name;
            }
            for (int i = bad.Count; i < bad.Count; i++)
            {
                if ((int)bad[i] != (i + 5000))
                {
                    hsh1["SetRange"] = i;
                }
            }
        }
        else
        {
            //we make sure that the above methods throw here
            good.Clear();
            for (int i = 100; i > 0; i--)
                good.Add(i);

            try
            {
                bad.Sort();
                hsh1["Sort"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["Sort"] = "Copy_ExceptionType";
            }

            try
            {
                bad.Reverse();
                hsh1["Reverse"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Reverse"] = "Copy_ExceptionType, " + ex.GetType().Name;
            }

            try
            {
                bad.Sort(new CaseInsensitiveComparer());
                hsh1["Sort"] = "Copy - Icomparer";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["Sort"] = "Copy_ExceptionType";
            }

            try
            {
                bad.Sort(0, 0, new CaseInsensitiveComparer());
                hsh1["Sort"] = "Copy - int, int, IComparer";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["Sort"] = "Copy_ExceptionType";
            }

            //BinarySearch
            try
            {
                for (int i = 0; i < bad.Count; i++)
                {
                    if (bad.BinarySearch(bad[i]) != i)
                        hsh1["BinarySearch"] = "(Object)";
                }
                hsh1["BinarySearch"] = "(Object)";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["BinarySearch"] = ex.GetType().Name;
            }

            try
            {
                for (int i = 0; i < bad.Count; i++)
                {
                    if (bad.BinarySearch(bad[i], new CaseInsensitiveComparer()) != i)
                        hsh1["BinarySearch"] = "(Object)";
                }

                hsh1["BinarySearch"] = "Exception not thrown, (Object, IComparer)";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["BinarySearch"] = ex.GetType().Name;
            }

            try
            {
                for (int i = 0; i < bad.Count; i++)
                {
                    if (bad.BinarySearch(0, bad.Count, bad[i], new CaseInsensitiveComparer()) != i)
                        hsh1["BinarySearch"] = "(Object)";
                }

                hsh1["BinarySearch"] = "Exception not thrown, (Object, IComparer)";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["BinarySearch"] = ex.GetType().Name;
            }

            good.Clear();
            for (int i = 0; i < 100; i++)
                good.Add(i);

            Queue que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 5000);

            try
            {
                bad.SetRange(0, que);
                hsh1["Sort"] = "Copy - int, int, IComparer";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["Sort"] = "Copy_ExceptionType";
            }
        }

        //Modifiable methods
        if (!hsh1.ContainsKey("IsReadOnly") && !hsh1.ContainsKey("Fixed"))
        {
            good.Clear();
            for (int i = 0; i < 100; i++)
                good.Add(i);

            Queue que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 5000);
            bad.InsertRange(0, que);
            for (int i = 0; i < 100; i++)
            {
                if ((int)bad[i] != i + 5000)
                {
                    hsh1["InsertRange"] = i;
                }
            }

            //AddRange()
            que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 2222);
            bad.AddRange(que);
            for (int i = bad.Count - 100; i < bad.Count; i++)
            {
                if ((int)bad[i] != (i - (bad.Count - 100)) + 2222)
                {
                    hsh1["AddRange"] = i + " " + (int)bad[i];
                }
            }

            bad.RemoveRange(0, que.Count);
            for (int i = 0; i < 100; i++)
            {
                if ((int)bad[i] != i)
                {
                    hsh1["RemoveRange"] = i + " " + (int)bad[i];
                }
            }

            //Capacity
            try
            {
                bad.Capacity = bad.Capacity * 2;
            }
            catch (Exception ex)
            {
                hsh1["Capacity"] = ex.GetType().Name;
            }

            try
            {
                bad.Capacity = -1;
                hsh1["Capacity"] = "No_Exception_Thrown, -1";
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Capacity"] = ex.GetType().Name;
            }

            int iMakeSureThisDoesNotCause = 0;
            while (bad.Capacity == bad.Count)
            {
                if (iMakeSureThisDoesNotCause++ > 100)
                    break;
                bad.Add(bad.Count);
            }
            if (iMakeSureThisDoesNotCause > 100)
                hsh1["TrimToSize"] = "Monekeyed, " + bad.Count + " " + bad.Capacity;

            //TrimToSize()
            try
            {
                bad.TrimToSize();
                if (bad.Capacity != bad.Count)
                {
                    hsh1["TrimToSize"] = "Problems baby";
                }
            }
            catch (Exception ex)
            {
                hsh1["TrimToSize"] = ex.GetType().Name;
            }
        }
        else
        {
            Queue que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 5000);
            try
            {
                bad.AddRange(que);
                hsh1["AddRange"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["AddRange"] = "Copy_ExceptionType";
            }

            try
            {
                bad.InsertRange(0, que);
                hsh1["InsertRange"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["InsertRange"] = "Copy_ExceptionType";
            }

            good.Clear();
            for (int i = 0; i < 10; i++)
                good.Add(i);
            try
            {
                bad.RemoveRange(0, 10);
                hsh1["RemoveRange"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["RemoveRange"] = "Copy_ExceptionType, " + ex.GetType().Name;
            }

            try
            {
                bad.Capacity = bad.Capacity * 2;
                hsh1["Capacity"] = "No_Exception_Thrown, bad.Capacity*2";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Capacity"] = ex.GetType().Name;
            }

            try
            {
                bad.TrimToSize();
                hsh1["TrimToSize"] = "No_Exception_Thrown";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["TrimToSize"] = ex.GetType().Name;
            }
        }
    }

    private void DoIListTests(IList good, IList bad, Hashtable hsh1)
    {
        //ICollection tests
        DoICollectionTests(good, bad, hsh1);

        if (bad.IsReadOnly)
            hsh1["IsReadOnly"] = "yea";

        try
        {
            for (int i = 0; i < good.Count; i++)
            {
                if (!bad.Contains(good[i]))
                    hsh1["Contains"] = i;
                if (bad[i] != good[i])
                    hsh1["Item"] = "get";
                if (bad.IndexOf(good[i]) != i)
                    hsh1["IndexOf"] = i;
            }

            if (!hsh1.ContainsKey("IsReadOnly"))
            {
                for (int i = 100; i < 200; i++)
                {
                    try
                    {
                        good.Add(i);
                        bad.Add(i);
                    }
                    catch (Exception)
                    {
                        hsh1["Fixed"] = null;
                    }
                }
                if (!hsh1.ContainsKey("Fixed") && (bad.Count != bad.Count))
                    hsh1["Count"] = "ReadWrite";

                if (!hsh1.ContainsKey("Fixed"))
                {
                    try
                    {
                        bad.Clear();
                    }
                    catch (Exception)
                    {
                        hsh1["Clear"] = null;
                    }

                    for (int i = 0; i < 100; i++)
                    {
                        bad.Insert(0, i);
                    }

                    for (int i = 0; i < 100; i++)
                    {
                        bad.RemoveAt(0);
                    }

                    if (bad.Count != 0)
                        hsh1["Count"] = "Expected 0";

                    for (int i = 0; i < 100; i++)
                    {
                        bad.Add(i.ToString());
                    }

                    if (bad.Count != 100)
                        hsh1["Count"] = "Expected 100, " + bad.Count;

                    if (good.Count != 100)
                        hsh1["this"] = "Not the same objects, " + good.Count;

                    for (int i = 0; i < 100; i++)
                    {
                        if (!bad[i].Equals(i.ToString()))
                            hsh1["Item"] = "String";
                    }

                    for (int i = 0; i < 100; i++)
                        bad.Remove(i.ToString());

                    if (bad.Count != 0)
                        hsh1["Count"] = "Expected 0, " + bad.Count;

                    for (int i = 0; i < 100; i++)
                    {
                        bad.Add(i.ToString());
                    }

                    for (int i = 99; i > 0; i--)
                        bad[i] = i.ToString();

                    if (bad.Count != 100)
                        hsh1["Count"] = "Expected 100, " + bad.Count;

                    for (int i = 99; i > 0; i--)
                    {
                        if (!bad[i].Equals(i.ToString()))
                            hsh1["Item"] = "String";
                    }

                    bad.Clear();

                    if (bad.Count != 0)
                        hsh1["Count"] = "Expected 0, " + bad.Count;
                }
            }

            if (hsh1.ContainsKey("IsReadOnly") || hsh1.ContainsKey("Fixed"))
            {
                //we will work on the original and see the copy
                try
                {
                    good.Clear();
                }
                catch (Exception)
                {
                    hsh1["Clear"] = "original";
                }

                for (int i = 0; i < 100; i++)
                {
                    good.Insert(0, i);
                }

                if (bad.Count != 100)
                    hsh1["Count"] = "Not equal to original";

                for (int i = 0; i < 100; i++)
                {
                    good.RemoveAt(0);
                }

                if (bad.Count != 0)
                    hsh1["Count"] = "Expected 0";

                for (int i = 0; i < 100; i++)
                {
                    good.Add(i.ToString());
                }

                if (bad.Count != 100)
                    hsh1["Count"] = "Expected 100, " + bad.Count;

                if (good.Count != 100)
                    hsh1["this"] = "Not the same objects";

                for (int i = 0; i < 100; i++)
                {
                    if (!bad[i].Equals(i.ToString()))
                        hsh1["Item"] = "String";
                }

                for (int i = 0; i < 100; i++)
                    good.Remove(i.ToString());

                if (bad.Count != 0)
                    hsh1["Count"] = "Expected 0, " + bad.Count;

                for (int i = 0; i < 100; i++)
                {
                    good.Add(i.ToString());
                }

                for (int i = 99; i > 0; i--)
                    good[i] = i.ToString();

                if (bad.Count != 100)
                    hsh1["Count"] = "Expected 100, " + bad.Count;

                for (int i = 99; i > 0; i--)
                {
                    if (!bad[i].Equals(i.ToString()))
                        hsh1["Item"] = "String";
                }

                good.Clear();

                if (bad.Count != 0)
                    hsh1["Count"] = "Expected 0, " + bad.Count;

                //we will make sure that these methods throw by calling them anyways!!!
                good.Clear();
                for (int i = 0; i < 100; i++)
                {
                    good.Add(i);
                }

                try
                {
                    bad.Clear();
                    hsh1["Clear"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["Clear"] = "Copy_ExceptionType";
                }

                try
                {
                    bad.Insert(0, 1);
                    hsh1["Insert"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["Insert"] = "Copy_ExceptionType";
                }

                try
                {
                    bad.RemoveAt(0);
                    hsh1["RemoveAt"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["RemoveAt"] = "Copy_ExceptionType";
                }

                try
                {
                    bad.Add(1);
                    hsh1["Add"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["RemoveAt"] = "Copy_ExceptionType";
                }

                try
                {
                    bad.Remove(1);
                    hsh1["Remove"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["Remove"] = "Copy_ExceptionType";
                }
            }
        }
        catch (Exception ex)
        {
            hsh1["DoIListTests"] = ex.GetType().Name;
        }
    }

    private void DoICollectionTests(ICollection good, ICollection bad, Hashtable hsh1)
    {
        int[] iArr1 = null;
        int[] iArr2 = null;

        if (good.Count != bad.Count)
            hsh1.Add("Count", null);
        if (good.IsSynchronized != bad.IsSynchronized)
            hsh1.Add("IsSynchronized", null);
        if (good.SyncRoot != bad.SyncRoot)
            hsh1.Add("SyncRoot", null);

        iArr1 = new int[good.Count];
        iArr2 = new int[good.Count];
        good.CopyTo(iArr1, 0);
        bad.CopyTo(iArr2, 0);

        for (int i = 0; i < iArr1.Length; i++)
        {
            if (iArr1[i] != iArr2[i])
                hsh1["CopyTo"] = "vanila";
        }

        iArr1 = new int[good.Count + 5];
        iArr2 = new int[good.Count + 5];
        good.CopyTo(iArr1, 5);
        bad.CopyTo(iArr2, 5);

        for (int i = 5; i < iArr1.Length; i++)
        {
            if (iArr1[i] != iArr2[i])
                hsh1["CopyTo"] = "5";
        }

        DoIEnumerableTest(good.GetEnumerator(), bad.GetEnumerator(), good, bad, hsh1, false);
        IEnumerator ienm1 = good.GetEnumerator();
        IEnumerator ienm2 = bad.GetEnumerator();
        if (good.Count > 0)
        {
            ((IList)good).RemoveAt(0);
            DoIEnumerableTest(ienm1, ienm2, good, bad, hsh1, true);
        }
    }

    private void DoIEnumerableTest(IEnumerator ienm1, IEnumerator ienm2, IEnumerable ie1, IEnumerable ie2, Hashtable hsh1, Boolean fExpectToThrow)
    {
        if (!fExpectToThrow)
        {
            while (ienm1.MoveNext())
            {
                Boolean bb = ienm2.MoveNext();
                if (ienm1.Current != ienm2.Current)
                    hsh1["Enumerator"] = "Current";
            }
            ienm1.Reset();
            ienm2.Reset();
            while (ienm1.MoveNext())
            {
                ienm2.MoveNext();
                if (ienm1.Current != ienm2.Current)
                    hsh1["Enumerator"] = "Reset";
            }

            ienm1.Reset();
            ienm2.Reset();
            IEnumerator ienm1Clone = null;
            IEnumerator ienm2Clone = null;
            Boolean fPastClone = true;
            try
            {
                ienm1Clone = ie1.GetEnumerator();
            }
            catch (Exception)
            {
                fPastClone = false;
            }
            try
            {
                ienm2Clone = ie2.GetEnumerator();
            }
            catch (Exception)
            {
                fPastClone = false;
            }
            if (fPastClone)
            {
                while (ienm1Clone.MoveNext())
                {
                    Boolean bb = ienm2Clone.MoveNext();
                    if (ienm1Clone.Current != ienm2Clone.Current)
                        hsh1["Enumerator"] = "Current";
                }
                ienm1Clone.Reset();
                ienm2Clone.Reset();
                while (ienm1Clone.MoveNext())
                {
                    ienm2Clone.MoveNext();
                    if (ienm1Clone.Current != ienm2Clone.Current)
                        hsh1["Enumerator"] = "Reset";
                }
            }
        }
        else
        {
            try
            {
                ienm2.MoveNext();
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
                hsh1["Enumerator"] = "MoveNext";
            }
            try
            {
                ienm2.Reset();
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
                hsh1["Enumerator"] = "Reset";
            }
        }
    }

    private void CompareRangeObjects(ArrayList good, ArrayList bad, Hashtable hsh1)
    {
        //IList, this includes ICollection tests as well!!
        DoRangeIListTests(good, bad, hsh1);

        bad.Clear();
        for (int i = 0; i < 100; i++)
            bad.Add(i);

        //we will now test ArrayList specific methods
        //AL's CopyTo methods
        int[] iArr1 = null;
        int[] iArr2 = null;
        int goodCount = good.Count;
        iArr1 = new int[goodCount];
        iArr2 = new int[goodCount];
        good.CopyTo(iArr1);
        bad.CopyTo(iArr2);
        for (int i = 0; i < goodCount; i++)
        {
            if (iArr1[i] != iArr2[i])
                hsh1["CopyTo"] = "()";
        }

        iArr1 = new int[goodCount];
        iArr2 = new int[goodCount];
        good.CopyTo(0, iArr1, 0, goodCount);
        try
        {
            bad.CopyTo(0, iArr2, 0, goodCount);
            for (int i = 0; i < goodCount; i++)
            {
                if (iArr1[i] != iArr2[i])
                    hsh1["CopyTo"] = "()";
            }
        }
        catch
        {
            hsh1["CopyTo"] = "(int, Array, int, int)";
        }

        iArr1 = new int[200];
        iArr2 = new int[200];
        for (int i = 0; i < 200; i++)
        {
            iArr1[i] = 50;
            iArr2[i] = 50;
        }
        good.CopyTo(50, iArr1, 100, 20);
        try
        {
            bad.CopyTo(50, iArr2, 100, 20);
            for (int i = 0; i < 200; i++)
            {
                if (iArr1[i] != iArr2[i])
                    hsh1["CopyTo"] = "(Array, int, int)";
            }
        }
        catch
        {
            hsh1["CopyTo"] = "(int, Array, int, int)";
        }

        //Clone()
        ArrayList alstClone = (ArrayList)bad.Clone();
        //lets make sure that the clone is what it says it is
        if (alstClone.Count != bad.Count)
            hsh1["Clone"] = "Count";
        for (int i = 0; i < bad.Count; i++)
        {
            if (alstClone[i] != bad[i])
                hsh1["Clone"] = "[]";
        }

        //GerEnumerator()
        IEnumerator ienm1 = null;
        IEnumerator ienm2 = null;

        ienm1 = good.GetEnumerator(0, 100);
        try
        {
            ienm2 = bad.GetEnumerator(0, 100);
            DoIEnumerableTest(ienm1, ienm2, good, bad, hsh1, false);
        }
        catch
        {
            hsh1["GetEnumerator"] = "(int, int)";
        }
        ienm1 = good.GetEnumerator(50, 50);
        try
        {
            ienm2 = bad.GetEnumerator(50, 50);
            DoIEnumerableTest(ienm1, ienm2, good, bad, hsh1, false);
        }
        catch
        {
            hsh1["GetEnumerator"] = "(int, int)";
        }

        try
        {
            bad.GetEnumerator(50, 150);
            hsh1["GetEnumerator"] = "(int, int)";
        }
        catch (Exception)
        {
        }
        ienm1 = good.GetEnumerator(0, 100);
        try
        {
            ienm2 = bad.GetEnumerator(0, 100);
            bad.RemoveAt(0);
            DoIEnumerableTest(ienm1, ienm2, good, bad, hsh1, true);
        }
        catch
        {
            hsh1["GetEnumerator"] = "(int, int)";
        }

        //GetRange
        bad.Clear();
        for (int i = 0; i < 100; i++)
            bad.Add(i);

        ArrayList alst1 = good.GetRange(0, good.Count);
        try
        {
            ArrayList alst2 = bad.GetRange(0, good.Count);
            for (int i = 0; i < good.Count; i++)
            {
                if (alst1[i] != alst2[i])
                    hsh1["GetRange"] = i;
            }
        }
        catch
        {
            hsh1["Range"] = "(int, int)";
        }

        //IndexOf(Object, int)
        if (bad.Count > 0)
        {
            for (int i = 0; i < good.Count; i++)
            {
                if (good.IndexOf(good[i], 0) != bad.IndexOf(good[i], 0))
                {
                    hsh1["IndexOf"] = "(Object, int)";
                }
                if (good.IndexOf(good[i], i) != bad.IndexOf(good[i], i))
                {
                    hsh1["IndexOf"] = "(Object, int)";
                }
                if (i < (good.Count - 1))
                {
                    if (good.IndexOf(good[i], i + 1) != bad.IndexOf(good[i], i + 1))
                    {
                        hsh1["IndexOf"] = "(Object, int)";
                    }
                }
            }

            try
            {
                bad.IndexOf(1, -1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            try
            {
                bad.IndexOf(1, bad.Count);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            //IndexOf(Object, int, int)
            //Update - 2001/03/20: The semantics of this method has changed, the 3rd parameter now refers to count instead of length
            for (int i = 0; i < good.Count; i++)
            {
                if (good.IndexOf(good[i], 0, good.Count - 1) != bad.IndexOf(good[i], 0, good.Count - 1))
                {
                    hsh1["IndexOf"] = "(Object, int, int)";
                }
                if (good.IndexOf(good[i], i, good.Count - i) != bad.IndexOf(good[i], i, good.Count - i))
                {
                    hsh1["IndexOf"] = "(Object, int)";
                }
                if (good.IndexOf(good[i], i, 0) != bad.IndexOf(good[i], i, 0))
                {
                    hsh1["IndexOf"] = "(Object, int)";
                }
                if (i < (good.Count - 1))
                {
                    if (good.IndexOf(good[i], i + 1, good.Count - (i + 1)) != bad.IndexOf(good[i], i + 1, good.Count - (i + 1)))
                    {
                        hsh1["IndexOf"] = "(Object, int)";
                    }
                }
            }

            try
            {
                bad.IndexOf(1, 0, -1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            try
            {
                bad.IndexOf(1, 0, bad.Count);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            try
            {
                bad.IndexOf(1, bad.Count - 1, bad.Count - 2);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["IndexOf"] = ex;
            }

            //LastIndexOf(Object)
            for (int i = 0; i < good.Count; i++)
            {
                if (good.LastIndexOf(good[i]) != bad.LastIndexOf(good[i]))
                {
                    hsh1["LastIndexOf"] = "(Object)";
                }
                if (good.LastIndexOf(i + 1000) != bad.LastIndexOf(i + 1000))
                {
                    hsh1["LastIndexOf"] = "(Object)";
                }
            }

            try
            {
                bad.LastIndexOf(null);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }


            //LastIndexOf(Object, int)
            for (int i = 0; i < good.Count; i++)
            {
                if (good.LastIndexOf(good[i], good.Count - 1) != bad.LastIndexOf(good[i], good.Count - 1))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (good.LastIndexOf(good[i], 0) != bad.LastIndexOf(good[i], 0))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (good.LastIndexOf(good[i], i) != bad.LastIndexOf(good[i], i))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (i < (good.Count - 1))
                {
                    if (good.LastIndexOf(good[i], i + 1) != bad.LastIndexOf(good[i], i + 1))
                    {
                        hsh1["LastIndexOf"] = "(Object, int)";
                    }
                }
            }

            try
            {
                bad.LastIndexOf(1, -1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }

            try
            {
                bad.LastIndexOf(1, bad.Count);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }

            //LastIndexOf(Object, int, int)
            for (int i = 0; i < good.Count; i++)
            {
                if (good.LastIndexOf(good[i], good.Count - 1, 0) != bad.LastIndexOf(good[i], good.Count - 1, 0))
                {
                    hsh1["LastIndexOf"] = "(Object, int, int)";
                }
                if (good.LastIndexOf(good[i], good.Count - 1, i) != bad.LastIndexOf(good[i], good.Count - 1, i))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (good.LastIndexOf(good[i], i, i) != bad.LastIndexOf(good[i], i, i))
                {
                    hsh1["LastIndexOf"] = "(Object, int)";
                }
                if (i < (good.Count - 1))
                {
                    if (good.LastIndexOf(good[i], good.Count - 1, i + 1) != bad.LastIndexOf(good[i], good.Count - 1, i + 1))
                    {
                        hsh1["LastIndexOf"] = "(Object, int)";
                    }
                }
            }

            try
            {
                bad.LastIndexOf(1, 1, -1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }

            try
            {
                bad.LastIndexOf(1, bad.Count - 2, bad.Count - 1);
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["LastIndexOf"] = ex;
            }
        }

        //ReadOnly()
        ArrayList alst3 = ArrayList.ReadOnly(bad);
        if (!alst3.IsReadOnly)
            hsh1["ReadOnly"] = "Not";

        IList ilst1 = ArrayList.ReadOnly((IList)bad);
        if (!ilst1.IsReadOnly)
            hsh1["ReadOnly"] = "Not";

        //Synchronized()
        alst3 = ArrayList.Synchronized(bad);
        if (!alst3.IsSynchronized)
            hsh1["Synchronized"] = "Not";

        ilst1 = ArrayList.Synchronized((IList)bad);
        if (!ilst1.IsSynchronized)
            hsh1["Synchronized"] = "Not";

        //ToArray()
        if (good.Count == bad.Count)
        {
            Object[] oArr1 = good.ToArray();
            Object[] oArr2 = bad.ToArray();
            for (int i = 0; i < good.Count; i++)
            {
                if ((int)oArr1[i] != (int)oArr2[i])
                    hsh1["ToArray"] = "()";
            }

            //ToArray(type)
            iArr1 = (int[])good.ToArray(typeof(int));
            iArr2 = (int[])bad.ToArray(typeof(int));
            for (int i = 0; i < good.Count; i++)
            {
                if (iArr1[i] != iArr2[i])
                    hsh1["ToArray"] = "(Type)";
            }
        }

        //Capacity - get
        if (good.Capacity != bad.Capacity)
        {
            hsh1["Capacity"] = "get";
        }

        //Fixed size methods
        if (!hsh1.ContainsKey("IsReadOnly"))
        {
            bad.Clear();
            for (int i = 100; i > 0; i--)
                bad.Add(i);
            //Sort() & BinarySearch(Object)
            bad.Sort();
            for (int i = 0; i < bad.Count - 1; i++)
            {
                if ((int)bad[i] > (int)bad[i + 1])
                    hsh1["Sort"] = "()";
            }
            for (int i = 0; i < bad.Count; i++)
            {
                if (bad.BinarySearch(bad[i]) != i)
                    hsh1["BinarySearch"] = "(Object)";
            }
            //Reverse()
            bad.Reverse();
            if (bad.Count > 0)
            {
                for (int i = 0; i < 99; i++)
                {
                    if ((int)bad[i] < (int)bad[i + 1])
                        hsh1["Reverse"] = "()";
                }

                bad.Clear();
                for (int i = 100; i > 0; i--)
                    bad.Add(i.ToString());
            }

            bad.Clear();
            for (int i = 90; i > 64; i--)
                bad.Add(((Char)i).ToString());
            try
            {
                bad.Sort(new CaseInsensitiveComparer());
                if (bad.Count > 0)
                {
                    for (int i = 0; i < (bad.Count - 1); i++)
                    {
                        if (((String)bad[i]).CompareTo(((String)bad[i + 1])) >= 0)
                            hsh1["Sort"] = "(IComparer)";
                    }
                    for (int i = 0; i < bad.Count; i++)
                    {
                        if (bad.BinarySearch(bad[i], new CaseInsensitiveComparer()) != i)
                            hsh1["BinarySearch"] = "(Object, IComparer)";
                    }
                }

                bad.Reverse();

                bad.Clear();
                for (int i = 65; i < 91; i++)
                    bad.Add(((Char)i).ToString());
                //Sort(int, int, IComparer) & BinarySearch(int, int, Object, IComparer)
                if (bad.Count > 0)
                {
                    for (int i = 0; i < good.Count; i++)
                    {
                        if (bad.BinarySearch(0, bad.Count, bad[i], new CaseInsensitiveComparer()) != i)
                            hsh1["BinarySearch"] = "(int, int, Object, IComparer)";
                    }
                }
            }
            catch (Exception)
            {
            }

            bad.Clear();
            for (int i = 0; i < 100; i++)
                bad.Add(i);

            Queue que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 5000);

            try
            {
                bad.SetRange(0, que);
            }
            catch (Exception ex)
            {
                hsh1["SetRange"] = "Copy_ExceptionType, " + ex.GetType().Name;
            }
            for (int i = bad.Count; i < bad.Count; i++)
            {
                if ((int)bad[i] != (i + 5000))
                {
                    hsh1["SetRange"] = i;
                }
            }
        }
        else
        {
            //we make sure that the above methods throw here
            bad.Clear();
            for (int i = 100; i > 0; i--)
                bad.Add(i);

            try
            {
                bad.Sort();
                hsh1["Sort"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["Sort"] = "Copy_ExceptionType";
            }

            try
            {
                bad.Reverse();
                hsh1["Reverse"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Reverse"] = "Copy_ExceptionType, " + ex.GetType().Name;
            }

            try
            {
                bad.Sort(new CaseInsensitiveComparer());
                hsh1["Sort"] = "Copy - Icomparer";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["Sort"] = "Copy_ExceptionType";
            }

            try
            {
                bad.Sort(0, 0, new CaseInsensitiveComparer());
                hsh1["Sort"] = "Copy - int, int, IComparer";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["Sort"] = "Copy_ExceptionType";
            }

            //BinarySearch
            try
            {
                for (int i = 0; i < bad.Count; i++)
                {
                    if (bad.BinarySearch(bad[i]) != i)
                        hsh1["BinarySearch"] = "(Object)";
                }
                hsh1["BinarySearch"] = "(Object)";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["BinarySearch"] = ex.GetType().Name;
            }

            try
            {
                for (int i = 0; i < bad.Count; i++)
                {
                    if (bad.BinarySearch(bad[i], new CaseInsensitiveComparer()) != i)
                        hsh1["BinarySearch"] = "(Object)";
                }
                hsh1["BinarySearch"] = "Exception not thrown, (Object, IComparer)";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["BinarySearch"] = ex.GetType().Name;
            }

            try
            {
                for (int i = 0; i < bad.Count; i++)
                {
                    if (bad.BinarySearch(0, bad.Count, bad[i], new CaseInsensitiveComparer()) != i)
                        hsh1["BinarySearch"] = "(Object)";
                }
                hsh1["BinarySearch"] = "Exception not thrown, (Object, IComparer)";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["BinarySearch"] = ex.GetType().Name;
            }

            bad.Clear();
            for (int i = 0; i < 100; i++)
                bad.Add(i);
            Queue que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 5000);
            try
            {
                bad.SetRange(0, que);
                hsh1["Sort"] = "Copy - int, int, IComparer";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["Sort"] = "Copy_ExceptionType";
            }
        }

        //Modifiable methods
        if (!hsh1.ContainsKey("IsReadOnly") && !hsh1.ContainsKey("Fixed"))
        {
            bad.Clear();
            for (int i = 0; i < 100; i++)
                bad.Add(i);

            Queue que = new Queue();

            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 5000);
            bad.InsertRange(0, que);

            for (int i = 0; i < 100; i++)
            {
                if ((int)bad[i] != i + 5000)
                {
                    hsh1["InsertRange"] = i;
                }
            }

            //AddRange()
            que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 2222);

            bad.AddRange(que);
            for (int i = bad.Count - 100; i < bad.Count; i++)
            {
                if ((int)bad[i] != (i - (bad.Count - 100)) + 2222)
                {
                    hsh1["AddRange"] = i + " " + (int)bad[i];
                }
            }

            bad.RemoveRange(0, que.Count);
            for (int i = 0; i < 100; i++)
            {
                if ((int)bad[i] != i)
                {
                    hsh1["RemoveRange"] = i + " " + (int)bad[i];
                }
            }

            //Capacity
            try
            {
                bad.Capacity = bad.Capacity * 2;
            }
            catch (Exception ex)
            {
                hsh1["Capacity"] = ex.GetType().Name;
            }

            try
            {
                bad.Capacity = -1;
                hsh1["Capacity"] = "No_Exception_Thrown, -1";
            }
            catch (ArgumentException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Capacity"] = ex.GetType().Name;
            }

            int iMakeSureThisDoesNotCause = 0;
            while (bad.Capacity == bad.Count)
            {
                if (iMakeSureThisDoesNotCause++ > 100)
                    break;
                bad.Add(bad.Count);
            }
            if (iMakeSureThisDoesNotCause > 100)
                hsh1["TrimToSize"] = "Monekeyed, " + bad.Count + " " + bad.Capacity;

            //TrimToSize()
            try
            {
                bad.TrimToSize();
                if (bad.Capacity != bad.Count)
                {
                    hsh1["TrimToSize"] = "Problems baby";
                }
            }
            catch (Exception ex)
            {
                hsh1["TrimToSize"] = ex.GetType().Name;
            }

            GC.KeepAlive(typeof(NotSupportedException)); // This line will keep type metadata alive for Project N.            
        }
        else
        {
            Queue que = new Queue();
            for (int i = 0; i < 100; i++)
                que.Enqueue(i + 5000);
            try
            {
                bad.AddRange(que);
                hsh1["AddRange"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["AddRange"] = "Copy_ExceptionType";
            }

            try
            {
                bad.InsertRange(0, que);
                hsh1["InsertRange"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception)
            {
                hsh1["InsertRange"] = "Copy_ExceptionType";
            }

            bad.Clear();
            for (int i = 0; i < 10; i++)
                bad.Add(i);
            try
            {
                bad.RemoveRange(0, 10);
                hsh1["RemoveRange"] = "Copy";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["RemoveRange"] = "Copy_ExceptionType, " + ex.GetType().Name;
            }

            try
            {
                bad.Capacity = bad.Capacity * 2;
                hsh1["Capacity"] = "No_Exception_Thrown, bad.Capacity*2";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["Capacity"] = ex.GetType().Name;
            }

            try
            {
                bad.TrimToSize();
                hsh1["TrimToSize"] = "No_Exception_Thrown";
            }
            catch (NotSupportedException)
            {
            }
            catch (Exception ex)
            {
                hsh1["TrimToSize"] = ex.GetType().Name;
            }
        }
    }

    private void DoRangeIListTests(IList good, IList bad, Hashtable hsh1)
    {
        //ICollection tests
        DoRangeICollectionTests(good, bad, hsh1);

        //@Hack
        if (bad.IsReadOnly)
            hsh1["IsReadOnly"] = "yea";

        try
        {
            for (int i = 0; i < good.Count; i++)
            {
                if (!bad.Contains(good[i]))
                    hsh1["Contains"] = i;
                if (bad[i] != good[i])
                    hsh1["Item"] = "get";
                if (bad.IndexOf(good[i]) != i)
                    hsh1["IndexOf"] = i;
            }

            if (hsh1.ContainsKey("IsReadOnly") || hsh1.ContainsKey("Fixed"))
            {
                //we will work on the original and see the copy
                try
                {
                    good.Clear();
                }
                catch (Exception)
                {
                    hsh1["Clear"] = "original";
                }

                for (int i = 0; i < 100; i++)
                {
                    good.Insert(0, i);
                }

                if (bad.Count != 100)
                    hsh1["Count"] = "Not equal to original";

                for (int i = 0; i < 100; i++)
                {
                    good.RemoveAt(0);
                }

                if (bad.Count != 0)
                    hsh1["Count"] = "Expected 0";

                for (int i = 0; i < 100; i++)
                {
                    good.Add(i.ToString());
                }

                if (bad.Count != 100)
                    hsh1["Count"] = "Expected 100, " + bad.Count;

                if (good.Count != 100)
                    hsh1["this"] = "Not the same objects";

                for (int i = 0; i < 100; i++)
                {
                    if (!bad[i].Equals(i.ToString()))
                        hsh1["Item"] = "String";
                }

                for (int i = 0; i < 100; i++)
                    good.Remove(i.ToString());

                if (bad.Count != 0)
                    hsh1["Count"] = "Expected 0, " + bad.Count;

                for (int i = 0; i < 100; i++)
                {
                    good.Add(i.ToString());
                }

                for (int i = 99; i > 0; i--)
                    good[i] = i.ToString();

                if (bad.Count != 100)
                    hsh1["Count"] = "Expected 100, " + bad.Count;

                for (int i = 99; i > 0; i--)
                {
                    if (!bad[i].Equals(i.ToString()))
                        hsh1["Item"] = "String";
                }

                good.Clear();

                if (bad.Count != 0)
                    hsh1["Count"] = "Expected 0, " + bad.Count;

                //we will make sure that these methods throw by calling them anyways!!!
                good.Clear();
                for (int i = 0; i < 100; i++)
                {
                    good.Add(i);
                }

                try
                {
                    bad.Clear();
                    hsh1["Clear"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["Clear"] = "Copy_ExceptionType";
                }

                try
                {
                    bad.Insert(0, 1);
                    hsh1["Insert"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["Insert"] = "Copy_ExceptionType";
                }

                try
                {
                    bad.RemoveAt(0);
                    hsh1["RemoveAt"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["RemoveAt"] = "Copy_ExceptionType";
                }

                try
                {
                    bad.Add(1);
                    hsh1["Add"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["RemoveAt"] = "Copy_ExceptionType";
                }

                try
                {
                    bad.Remove(1);
                    hsh1["Remove"] = "Copy";
                }
                catch (NotSupportedException)
                {
                }
                catch (Exception)
                {
                    hsh1["Remove"] = "Copy_ExceptionType";
                }
            }
        }
        catch (Exception ex)
        {
            hsh1["DoIListTests"] = ex.GetType().Name;
        }
    }

    private void DoRangeICollectionTests(ICollection good, ICollection bad, Hashtable hsh1)
    {
        int[] iArr1 = null;
        int[] iArr2 = null;

        if (good.Count != bad.Count)
            hsh1.Add("Count", null);

        if (good.IsSynchronized != bad.IsSynchronized)
            hsh1.Add("IsSynchronized", null);

        if (good.SyncRoot != bad.SyncRoot)
            hsh1.Add("SyncRoot", null);

        iArr1 = new int[good.Count];
        iArr2 = new int[good.Count];
        good.CopyTo(iArr1, 0);
        bad.CopyTo(iArr2, 0);

        for (int i = 0; i < iArr1.Length; i++)
        {
            if (iArr1[i] != iArr2[i])
                hsh1["CopyTo"] = "vanila";
        }

        iArr1 = new int[good.Count + 5];
        iArr2 = new int[good.Count + 5];
        good.CopyTo(iArr1, 5);
        bad.CopyTo(iArr2, 5);

        for (int i = 5; i < iArr1.Length; i++)
        {
            if (iArr1[i] != iArr2[i])
                hsh1["CopyTo"] = "5";
        }

        DoIEnumerableTest(good.GetEnumerator(), bad.GetEnumerator(), good, bad, hsh1, false);
    }
}
