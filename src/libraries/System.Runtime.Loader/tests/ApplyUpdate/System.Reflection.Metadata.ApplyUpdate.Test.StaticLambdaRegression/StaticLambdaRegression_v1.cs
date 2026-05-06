// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test;

public class StaticLambdaRegression
{
    public int count;

    public string TestMethod()
    {
        count++;
#if true
        Message (static () => "hello2");
#endif
        return count.ToString();
    }

#if true
    public void Message (Func<string> msg) => Console.WriteLine (msg());
#endif
}
