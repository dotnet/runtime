// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Defining __BlockReflectionAttribute indicates that the .NET Native toolchain should remove metadata for all non-public APIs.
// This reduces size and disables private reflection on those APIs. 
// The attribute can also be applied to individual public APIs to similarly block them.

using System;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All)]
    internal class __BlockReflectionAttribute : Attribute { }
}
