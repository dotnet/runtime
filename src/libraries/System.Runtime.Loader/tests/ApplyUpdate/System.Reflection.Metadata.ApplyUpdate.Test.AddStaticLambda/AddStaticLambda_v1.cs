// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddStaticLambda
    {
        public string TestMethod()
	{
            return Double(static (s) => s + "abcd");
        }

        public string Double(Func<string,string> f) => f("") + f("1");

    }
}
