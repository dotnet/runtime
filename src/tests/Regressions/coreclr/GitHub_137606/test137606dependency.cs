// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// test137606prestub compiles against this assembly but does not deploy it, so the JIT cannot compile a method that
// uses DependencyType.
namespace Dependency137606
{
    public sealed class DependencyType
    {
        public int GetValue() => 1;
    }
}
