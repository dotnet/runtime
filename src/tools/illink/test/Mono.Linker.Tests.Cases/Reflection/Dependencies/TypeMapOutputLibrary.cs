// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

[assembly: TypeMap<TypeMapOutputGroup>("copied", typeof(TypeMapOutputTarget))]

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies;

public class TypeMapOutputGroup;
public class TypeMapOutputTarget;
