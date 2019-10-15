// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Add any internal types that we need to forward from System.Data.

// These types are required for Desktop to Core serialization as they are not covered by GenAPI because they are not exposed in the ref assembly.
#if netcoreapp
// System.Data.Odbc is only supported on netcoreapp
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.Odbc.ODBC32))]
#endif