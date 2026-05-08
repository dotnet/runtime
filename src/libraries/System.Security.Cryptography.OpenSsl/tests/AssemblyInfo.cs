// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

[assembly: SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst | TestPlatforms.Android, "OpenSSL is not supported on Apple platforms and Android")]