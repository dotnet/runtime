// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Hosting;

var hostBuilder = new HostBuilder();
Thread.Sleep(TimeSpan.FromSeconds(30));
hostBuilder.Build();