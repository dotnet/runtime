// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Add any internal types that we need to forward from System.

// These types are required for Desktop to Core serialization as they are not covered by GenFacades because they are marked as internal.
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Collections.Generic.TreeSet<>))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.IO.Compression.ZLibException))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.CookieVariant))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.PathList))]
