// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

Console.WriteLine("Hello, Wasi Console!");
for (int i = 0; i < args.Length; i ++)
    Console.WriteLine($"args[{i}] = {args[i]}");
return 42;
