// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker
{
    /// <summary>
    /// Represents the index of arguments passed to a function in IL (i.e. (ILParameterIndex)0 represents `this` for non-static methods.
    /// This is used to enforce a differentiation between scenarios where the 0 index should be `this` and when the 0 index should be the first non-this parameter in the type system.
    /// There are no named enum values, the underlying integer value represents the index value.
    /// Generally prefer to use <see cref="ILLink.Shared.SourceParameterIndex"/> when possible.
    /// See also <seealso cref="Mono.Linker.ParameterHelpers"/>.
    /// </summary>
    /// <example>
    /// In a call to a non-static function Foo(int a, int b, int c)
    /// 0 refers to `this`,
    /// 1 refers to a,
    /// 2 refers to b.
    /// 3 referes to c.
    /// </example>
    public enum ILParameterIndex
    { }
}
