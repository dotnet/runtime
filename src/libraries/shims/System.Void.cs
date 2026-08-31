// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// System.void typeforward requires a special C# syntax that we choose to handle here.
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(void))]
