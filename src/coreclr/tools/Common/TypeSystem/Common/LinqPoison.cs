// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    //
    // The type system needs to be low level enough to be usable as
    // an actual runtime type system.
    //
    // LINQ is not low level enough to be allowable in the type system.
    //
    // It also has performance characteristics that make it a poor choice
    // in high performance components such as the type system.
    //
    // If you get an error pointing to here, the fix is to remove
    // "using System.Linq" from your file. Do not modify this file or
    // remove it from the project.
    //
    internal sealed class Linq { }
}
