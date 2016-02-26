// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Composition
{
    /// <summary>
    /// When applied to a void, parameterless instance method on a part,
    /// MEF will call that method when composition of the part has
    /// completed. The method must be public or internal.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class OnImportsSatisfiedAttribute : Attribute
    {
    }
}
