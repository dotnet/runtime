// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// We don't want to run the tests in parallel because they need exclusive access to serial ports
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
[assembly: SkipOnPlatform(TestPlatforms.NetBSD | TestPlatforms.OpenBSD | TestPlatforms.illumos | TestPlatforms.Solaris | TestPlatforms.Haiku, "SerialPort.GetPortNames is not implemented on this platform yet.")]
