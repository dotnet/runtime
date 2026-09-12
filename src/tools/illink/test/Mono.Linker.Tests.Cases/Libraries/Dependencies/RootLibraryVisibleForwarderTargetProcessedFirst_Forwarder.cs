// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

[assembly: TypeForwardedTo(typeof(RootLibraryVisibleForwarderTargetProcessedFirst))]
