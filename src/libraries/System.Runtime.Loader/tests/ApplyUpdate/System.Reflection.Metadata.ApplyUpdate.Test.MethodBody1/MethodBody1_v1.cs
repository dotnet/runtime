// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class MethodBody1 {
        public static int CallCount;

        public static string StaticMethod1 () {
            CallCount++;
            return "NEW STRING";
        }
    }
}
