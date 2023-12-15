// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace VirtFunc
{
    public class CTest1
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Dictionary<object, int> myHT = new Dictionary<object, int>();

            CDerived1 c1 = new CDerived1();
            myHT.Add(c1, 1);

            CDerived2 c2 = new CDerived2();
            myHT.Add(c2, 2);

            CDerived3 c3 = new CDerived3();
            myHT.Add(c3, 3);

            CDerived4 c4 = new CDerived4();
            myHT.Add(c4, 4);

            CDerived5 c5 = new CDerived5();
            myHT.Add(c5, 5);

            CDerived6 c6 = new CDerived6();
            myHT.Add(c6, 6);

            CDerived7 c7 = new CDerived7();
            myHT.Add(c7, 7);

            CDerived8 c8 = new CDerived8();
            myHT.Add(c8, 8);

            CDerived9 c9 = new CDerived9();
            myHT.Add(c9, 9);

            CDerived10 c10 = new CDerived10();
            myHT.Add(c10, 10);

            CDerived11 c11 = new CDerived11();
            myHT.Add(c11, 11);

            CDerived12 c12 = new CDerived12();
            myHT.Add(c12, 12);

            CDerived13 c13 = new CDerived13();
            myHT.Add(c13, 13);

            CDerived14 c14 = new CDerived14();
            myHT.Add(c14, 14);

            CDerived15 c15 = new CDerived15();
            myHT.Add(c15, 15);

            CDerived16 c16 = new CDerived16();
            myHT.Add(c16, 16);

            CDerived17 c17 = new CDerived17();
            myHT.Add(c17, 17);

            CDerived18 c18 = new CDerived18();
            myHT.Add(c18, 18);

            CDerived19 c19 = new CDerived19();
            myHT.Add(c19, 19);

            CDerived20 c20 = new CDerived20();
            myHT.Add(c20, 20);

	    foreach (var item in myHT) {
	      if (item.Key.GetHashCode() != item.Value) {
		Console.WriteLine("FAILED at " + item.Value);
		return 1;
	      }
	    }

            Console.WriteLine("PASSED");
            return 100;
        }
    }
}
// csc /r:cderived1.dll,cderived2.dll,cderived3.dll,cderived4.dll,cderived5.dll,cderived6.dll,cderived7.dll,cderived8.dll,cderived9.dll,cderived10.dll,cderived11.dll,cderived12.dll,cderived13.dll,cderived14.dll,cderived15.dll,cderived16.dll,cderived17.dll,cderived18.dll,cderived19.dll,cderived20.dll ctest1.cs
