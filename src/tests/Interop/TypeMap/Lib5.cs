// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

// This library defines TypeMap entries that should be used when
// TypeMapLib5 is specified as the TypeMappingEntryAssembly
[assembly: TypeMap<AlternateEntryPoint>("lib5_type1", typeof(Lib5Type1))]
[assembly: TypeMap<AlternateEntryPoint>("lib5_type2", typeof(Lib5Type2))]

[assembly: TypeMapAssociation<AlternateEntryPoint>(typeof(Lib5Type1), typeof(Lib5Proxy1))]
[assembly: TypeMapAssociation<AlternateEntryPoint>(typeof(Lib5Type2), typeof(Lib5Proxy2))]

public class Lib5Type1 { }
public class Lib5Type2 { }
public class Lib5Proxy1 { }
public class Lib5Proxy2 { }

public class AlternateEntryPoint { }
