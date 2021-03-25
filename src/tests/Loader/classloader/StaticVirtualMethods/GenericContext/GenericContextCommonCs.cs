// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

static class Statics
{
    public static string String;
    public static int Failures;
    public static int Successes;
    public static volatile IntPtr FtnHolder;
    public static volatile Action ActionHolder;

    public static void CheckForFailure(string scenario, string expectedResult)
    {
        if (String != expectedResult)
        {
            Console.WriteLine($"Scenario {scenario} failed");
            Failures++;
        }
        else
        {
            Successes++;
        }
    }
    public static string MakeName(RuntimeTypeHandle t)
    {
        return MakeName(Type.GetTypeFromHandle(t));
    }
    public static int ReportResults()
    {
        Console.WriteLine($"{Successes} successes reported");
        Console.WriteLine($"{Failures} failures reported");
        if (Failures > 0)
            return 1;
        else
            return 100;
    }

    public static string MakeName(Type t)
    {
        StringBuilder sb = new StringBuilder();
        if (t == typeof(int))
            return "int32";
        if (t == typeof(string))
            return "string";
        if (t == typeof(object))
            return "object";
        if (t.IsValueType)
            sb.Append("valuetype ");
        else
            sb.Append("class ");

        sb.Append(t.Name);
        if (t.GetGenericArguments().Length > 0)
        {
            sb.Append('<');
            bool first = true;
            foreach (var inst in t.GetGenericArguments())
            {
                if (!first)
                {
                    sb.Append(',');
                }
                else
                {
                    first = false;
                }
                sb.Append(MakeName(inst));
            }
            sb.Append('>');
        }
        return sb.ToString();
    }
}
