// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddLambdaCapturingThis
    {
        public AddLambdaCapturingThis()
        {
            field = "abcd";
        }

        public string GetField => field;

        private string field;

        public string TestMethod()
        {
            // capture 'this' but no locals
            Func<string,string> fn = s => field;
            Func<string,string> fn2 = s => "42" + s + field;
            return fn2 ("123");
        }
    }
}
