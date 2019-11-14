// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//different data types, Int16, Int32, Int64, etc
using System;

class pow3
{
	public static int Main()
	{
		bool pass = true;

		TestLibrary.TestFramework.BeginTestCase("Pow3");

		TestLibrary.TestFramework.BeginScenario("Pow tests");

		double a, b, y, z;

		//Int16
		Int16 x1 = 1;
		y = Math.Sinh(x1);
		a = Math.Pow(Math.E, x1);
		b = Math.Pow(Math.E, -x1);
		z = (a-b)/2;
		if ((y-z)>10*Double.Epsilon) {
			TestLibrary.TestFramework.LogError("001", "x: "+x1+", Sinh(x): "+y+", (Pow(E,x)-Pow(E,-x))/2: " + z);
			pass = false;
			}

		//Int32
		Int32 x2 = 1;
		y = Math.Sinh(x2);
		a = Math.Pow(Math.E, x2);
		b = Math.Pow(Math.E, -x2);
		z = (a-b)/2;
		if ((y-z)>10*Double.Epsilon) {
			TestLibrary.TestFramework.LogError("001", "x: "+x2+", Sinh(x): "+y+", (Pow(E,x)-Pow(E,-x))/2: " + z);
			pass = false;
			}

		//Int64
		Int64 x3 = 1;
		y = Math.Sinh(x3);
		a = Math.Pow(Math.E, x3);
		b = Math.Pow(Math.E, -x3);
		z = (a-b)/2;
		if ((y-z)>10*Double.Epsilon) {
			TestLibrary.TestFramework.LogError("001", "x: "+x3+", Sinh(x): "+y+", (Pow(E,x)-Pow(E,-x))/2: " + z);
			pass = false;
			}

		//UInt16
		UInt16 ux1 = 1;
		y = Math.Sinh(x1);
		a = Math.Pow(Math.E, ux1);
		b = Math.Pow(Math.E, -ux1);
		z = (a-b)/2;
		if ((y-z)>10*Double.Epsilon) {
			TestLibrary.TestFramework.LogError("001", "x: "+x1+", Sinh(x): "+y+", (Pow(E,x)-Pow(E,-x))/2: " + z);
			pass = false;
			}

		//UInt32
		UInt32 ux2 = 1;
		y = Math.Sinh(ux2);
		a = Math.Pow(Math.E, ux2);
		b = Math.Pow(Math.E, -ux2);
		z = (a-b)/2;
		if ((y-z)>10*Double.Epsilon) {
			TestLibrary.TestFramework.LogError("001", "x: "+x2+", Sinh(x): "+y+", (Pow(E,x)-Pow(E,-x))/2: " + z);
			pass = false;
			}

		//UInt64
		UInt64 ux3 = 1;
		y = Math.Sinh(ux3);
		a = Math.Pow(Math.E, ux3);
		b = 1/a;
		z = (a-b)/2;
		if ((y-z)>10*Double.Epsilon) {
			TestLibrary.TestFramework.LogError("001", "x: "+x3+", Sinh(x): "+y+", (Pow(E,x)-Pow(E,-x))/2: " + z);
			pass = false;
			}

		Single x4 = 1;
		y = Math.Sinh(x4);
		a = Math.Pow(Math.E, x4);
		b = Math.Pow(Math.E, -x4);
		z = (a-b)/2;
		if ((y-z)>10*Double.Epsilon) {
			TestLibrary.TestFramework.LogError("001", "x: "+x3+", Sinh(x): "+y+", (Pow(E,x)-Pow(E,-x))/2: " + z);
			pass = false;
			}

		Decimal x5 = 1;
		y = Math.Sinh(Decimal.ToDouble(x5));
		a = Math.Pow(Math.E, Decimal.ToDouble(x5));
		b = Math.Pow(Math.E, -Decimal.ToDouble(x5));
		z = (a-b)/2;
		if ((y-z)>10*Double.Epsilon) {
			TestLibrary.TestFramework.LogError("001", "x: "+x3+", Sinh(x): "+y+", (Pow(E,x)-Pow(E,-x))/2: " + z);
			pass = false;
			}
		
		TestLibrary.TestFramework.EndTestCase();
		if (pass) {
			TestLibrary.TestFramework.LogInformation("PASS");
			return 100;
		}
		else {
			TestLibrary.TestFramework.LogInformation("FAIL");
			return 1;
		}
		
	}
}
