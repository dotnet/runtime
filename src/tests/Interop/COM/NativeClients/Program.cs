// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

Console.WriteLine("This test should execute through a native entry point.");
Console.WriteLine("If you see this text, that means the test harness is misconfigured.");
Console.WriteLine("If this is on a new test run that uses a custom launcher, mark this test incompatible with the new test leg.");
Console.WriteLine("This can be done by adding to the PropertyGroup in CLRTest.Execute.targets where the $(CLRTestIsHosted) property is not true.");

return 101;
