// -*- indent-tabs-mode: nil -*-
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Globalization;
using Xunit;

public class Test
{
    private const int NumberOfElements = 100;
    private static Stack _stackDaughter;
    private static Stack _stackGrandDaughter;
    public static async Task GetSyncRootBasic () {
        var stackMother = new Stack ();
        for (int i = 0; i < NumberOfElements; i++)
        {
            stackMother.Push(i);
        }

        Assert.IsType<Stack>(stackMother.SyncRoot);
        Stack stackSon = Stack.Synchronized(stackMother);
        _stackGrandDaughter = Stack.Synchronized(stackSon);
        _stackDaughter = Stack.Synchronized(stackMother);

        Assert.Equal(stackSon.SyncRoot, stackMother.SyncRoot);
        Assert.Equal(_stackGrandDaughter.SyncRoot, stackMother.SyncRoot);
        Assert.Equal(_stackDaughter.SyncRoot, stackMother.SyncRoot);
        Assert.Equal(stackSon.SyncRoot, stackMother.SyncRoot);

        Action ts1 = SortElements;
        Action ts2 = ReverseElements;
        var tasks = new Task[4];
        for (int iThreads = 0; iThreads < tasks.Length; iThreads += 2)
        {
            tasks[iThreads] = Task.Run(ts1);
            tasks[iThreads + 1] = Task.Run(ts2);
        }

        await Task.WhenAll(tasks);
    }

    private static void SortElements()
    {
        _stackGrandDaughter.Clear();
        for (int i = 0; i < NumberOfElements; i++)
        {
            _stackGrandDaughter.Push(i);
        }
    }

    private static void ReverseElements()
    {
        _stackDaughter.Clear();
        for (int i = 0; i < NumberOfElements; i++)
        {
            _stackDaughter.Push(i);
        }
    }

    public static async Task ThreadPoolTest () {
        bool min = ThreadPool.SetMinThreads(1,1);
        bool max = ThreadPool.SetMaxThreads(1,1);

        int workerThreads;
        int portThreads;

        Console.WriteLine("SetMinThreads returns: {0} \nSetMaxThreads returns {1}",
            min, max);

        ThreadPool.GetMaxThreads(out workerThreads, out portThreads);
        Console.WriteLine("\nMaximum worker threads: \t{0}" +
            "\nMaximum completion port threads: {1}",
            workerThreads, portThreads);

        ThreadPool.GetMinThreads(out workerThreads, 
            out portThreads);
        Console.WriteLine("\nMinimum worker threads: \t{0}" +
            "\nAvailable completion port threads: {1}\n",
            workerThreads, portThreads);

        ThreadPool.GetAvailableThreads(out workerThreads, 
            out portThreads);
        Console.WriteLine("\nAvailable worker threads: \t{0}" +
            "\nAvailable completion port threads: {1}\n",
            workerThreads, portThreads);

        await GetSyncRootBasic ();
    }

    private static TimeZoneInfo CreateCustomLondonTimeZone()
    {
        TimeZoneInfo.TransitionTime start = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 1, 0, 0), 3, 5, DayOfWeek.Sunday);
        TimeZoneInfo.TransitionTime end = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 5, DayOfWeek.Sunday);
        TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(DateTime.MinValue.Date, DateTime.MaxValue.Date, new TimeSpan(1, 0, 0), start, end);
        return TimeZoneInfo.CreateCustomTimeZone("Europe/London", new TimeSpan(0), "Europe/London", "British Standard Time", "British Summer Time", new TimeZoneInfo.AdjustmentRule[] { rule });
    }

    public static void ConvertTimeFromToUtc () {
        TimeZoneInfo london = CreateCustomLondonTimeZone ();

        DateTime utc = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, utc.Kind);

        DateTime converted = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Utc);
        Assert.Equal(DateTimeKind.Utc, converted.Kind);
        DateTime back = TimeZoneInfo.ConvertTimeToUtc(converted, TimeZoneInfo.Utc);
        Assert.Equal(DateTimeKind.Utc, back.Kind);
        Assert.Equal(utc, back);

        Console.WriteLine("converted date {0}", converted);

        converted = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
        DateTimeKind expectedKind = (TimeZoneInfo.Local == TimeZoneInfo.Utc) ? DateTimeKind.Utc : DateTimeKind.Local;
        Assert.Equal(expectedKind, converted.Kind);
        back = TimeZoneInfo.ConvertTimeToUtc(converted, TimeZoneInfo.Local);
        Assert.Equal(DateTimeKind.Utc, back.Kind);
        Assert.Equal(utc, back);
    }

    public static void HasSameRules_RomeAndVatican()
    {
        TimeZoneInfo rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        TimeZoneInfo vatican = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vatican");
        Console.WriteLine("ROME: {0}", rome.StandardName);
        // Assert.True(rome.HasSameRules(vatican));
    }

    public static void Main (String[] args) {
        Console.WriteLine ("Hello, World!");

        // ConvertTimeFromToUtc ();
        TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        HasSameRules_RomeAndVatican ();
        

    }
}
