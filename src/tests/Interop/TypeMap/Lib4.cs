// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

[assembly: TypeMap<MultipleTypeMapAssemblies>("3", typeof(object))]
[assembly: TypeMap<MultipleTypeMapAssemblies>("4", typeof(string))]

[assembly: TypeMapAssociation<MultipleTypeMapAssemblies>(typeof(C1), typeof(string))]
[assembly: TypeMapAssociation<MultipleTypeMapAssemblies>(typeof(S1), typeof(object))]

[assembly: TypeMapAssemblyTarget<MultipleTypeMapAssemblies>("TypeMapLib3")] // Circular check