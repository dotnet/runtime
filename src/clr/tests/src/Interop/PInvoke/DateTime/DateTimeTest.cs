// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Runtime.InteropServices;
using TestLibrary;

public struct DateWrapper
{
    public DateTime date;
}

class NativeDateTime
{
    [DllImport(nameof(NativeDateTime))]
    public static extern DateTime GetTomorrow(DateTime today);

    [DllImport(nameof(NativeDateTime))]
    public static extern void GetTomorrowByRef(DateTime today, out DateTime tomorrow);

    [DllImport(nameof(NativeDateTime))]
    public static extern DateWrapper GetTomorrowWrapped(DateWrapper today);
}

class DateTimeTest
{
    static int Main(string[] args)
    {
        try
        {
            DateTime currentDate = new DateTime(2019, 5, 2);

            Assert.AreEqual(currentDate.AddDays(1), NativeDateTime.GetTomorrow(currentDate));

            NativeDateTime.GetTomorrowByRef(currentDate, out DateTime nextDay);
            
            Assert.AreEqual(currentDate.AddDays(1), nextDay);

            DateWrapper wrapper = new DateWrapper { date = currentDate };

            Assert.AreEqual(currentDate.AddDays(1), NativeDateTime.GetTomorrowWrapped(wrapper).date);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }
        return 100;
    }
}
