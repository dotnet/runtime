// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;

KeptMethod ();

[Kept]
static void KeptMethod () { }

static void UnusedMethod () { }
