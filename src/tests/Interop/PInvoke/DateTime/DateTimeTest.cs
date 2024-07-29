// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

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

public class DateTimeTest
{
    [Fact]
    [SkipOnMono("needs triage")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void TestEntryPoint()
    {
        DateTime currentDate = new DateTime(2019, 5, 2);

        Assert.Equal(currentDate.AddDays(1), NativeDateTime.GetTomorrow(currentDate));

        NativeDateTime.GetTomorrowByRef(currentDate, out DateTime nextDay);

        Assert.Equal(currentDate.AddDays(1), nextDay);

        DateWrapper wrapper = new DateWrapper { date = currentDate };

        Assert.Equal(currentDate.AddDays(1), NativeDateTime.GetTomorrowWrapped(wrapper).date);
    }
}
