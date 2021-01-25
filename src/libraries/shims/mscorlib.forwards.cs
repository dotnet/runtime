// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The types in this file are internal types that need to have type-forwards from full facade assemblies such as mscorlib.
// Many of these types are required by various components used by the C++/CLI compiler to live in mscorlib.

// These types are required to support the C++/CLI compiler's usage of alink for metadata linking.
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.AssemblyAttributesGoHere))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.AssemblyAttributesGoHereS))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.AssemblyAttributesGoHereM))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.AssemblyAttributesGoHereSM))]
[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.SuppressMergeCheckAttribute))]
