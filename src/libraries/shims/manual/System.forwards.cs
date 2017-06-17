// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Add any internal types that we need to forward from System. 

// These types are required for Desktop <--> Core serialization as they are not covered by GenAPI because they are not exposed.
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Collections.Generic.TreeSet<>))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.CookieVariant))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Net.PathList))]
