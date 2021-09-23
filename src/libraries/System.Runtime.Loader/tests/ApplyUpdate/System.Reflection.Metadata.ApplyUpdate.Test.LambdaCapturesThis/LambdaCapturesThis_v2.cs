// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class LambdaCapturesThis {

        string s;

        public LambdaCapturesThis () {}

        public string MethodWithLambda () {
            s = "NEWEST";
            Func<string> fn = () => s + " STRING!";
            return fn();
        }
    }
}
