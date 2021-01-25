// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;

        await Task.Delay(1);
        Console.WriteLine("Hello World!");
        for (int i = 0; i < args.Length; i++) {
            Console.WriteLine($"args[{i}] = {args[i]}");
        }
        return args.Length;