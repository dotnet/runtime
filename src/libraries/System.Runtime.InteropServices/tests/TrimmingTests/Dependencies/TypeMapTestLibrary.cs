// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using TypeMapTestLibrary;

[assembly: TypeMap<DependencyUniverse>("dependency/Target", typeof(DependencyTarget))]
[assembly: TypeMap<DependencyUniverse>("dependency/Trimmed", typeof(UnusedTarget), typeof(UnusedSource))]
[assembly: TypeMapAssociation<DependencyUniverse>(typeof(DependencySource), typeof(DependencyProxy))]
[assembly: TypeMapAssociation<DependencyUniverse>(typeof(UnusedSource), typeof(UnusedTarget))]
[assembly: TypeMap<OtherUniverse>("wrong/universe", typeof(DependencyTarget))]

namespace TypeMapTestLibrary;

public class DependencyUniverse;
public class OtherUniverse;
public class DependencySource;
public class DependencyTarget;
public class DependencyProxy;
public class UnusedSource;
public class UnusedTarget;
