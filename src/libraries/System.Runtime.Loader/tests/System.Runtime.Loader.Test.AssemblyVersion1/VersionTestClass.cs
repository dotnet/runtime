// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Loader.Tests
{
    public class VersionTestClass
    {
        public static string GetVersion()
            => typeof(VersionTestClass).Assembly.GetName().Version?.ToString(3) ?? "Unknown";
    }
}