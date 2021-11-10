// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading.Tasks;

namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AsyncMethodChange
    {
        public AsyncMethodChange () {}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<string> TestTaskMethod()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return "TestTaskMethod";
        }
    }
}
