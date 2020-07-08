// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

namespace TestClass
{

    public class Test
    {
        public static int Main()
        {
            double a = new TestClass().ApplyTime();
            if (a == 5000)
            {
                return 100;
            }
            else
            {
                return 0;
            }
        }
    }
    public struct ExpenseValues
    {
        public double AnnualPaidOutsideFunds;
    }

    public class TestClass
    {
        double mPeriodicExpense = 10000.0;

        public TestClass()
        {

        }

        public double ApplyTime()
        {
            ExpenseValues values = new ExpenseValues();
            values.AnnualPaidOutsideFunds = 0.0;
            double expense = mPeriodicExpense;
            double outside = 0.50 * expense;
            expense = expense - outside;

            // if you comment the next line the rutn value == 5000 (correct)
            values.AnnualPaidOutsideFunds += outside;

            return expense;
        }
    }
}
