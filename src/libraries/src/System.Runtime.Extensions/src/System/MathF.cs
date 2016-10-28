// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// We normally shouldn't have to manually specify the
// TypeForwardedToAttribute. The following bug is tracking
// this: https://github.com/dotnet/buildtools/issues/1041
[assembly: TypeForwardedTo(typeof(MathF))]
