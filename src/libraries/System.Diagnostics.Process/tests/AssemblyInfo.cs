// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

#if TARGET_OSX
// Retain serialization until process-management hangs under concurrent test classes on macOS are resolved.
// https://github.com/dotnet/runtime/issues/135294
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
#endif

[assembly: SkipOnPlatform(TestPlatforms.Browser, "System.Diagnostics.Process is not supported on Browser.")]
