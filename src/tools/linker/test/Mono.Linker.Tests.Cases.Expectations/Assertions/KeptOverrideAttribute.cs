// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    /// <summary>
    /// Used to ensure that a method should keep an 'override' annotation for a method in the supplied base type.
    /// The absence of this attribute does not enforce that the override is removed -- this is different from other Kept attributes
    /// To enforce the removal of an override, use <see cref="RemovedOverrideAttribute"/>.
    /// Fails in tests if the method doesn't have the override method in the original or linked assembly.
    /// </summary>
    /// <seealso cref="RemovedOverrideAttribute" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class KeptOverrideAttribute : KeptAttribute
    {
        public Type TypeWithOverriddenMethodDeclaration;

        public KeptOverrideAttribute(Type typeWithOverriddenMethod)
        {
            if (typeWithOverriddenMethod == null)
                throw new ArgumentNullException(nameof(typeWithOverriddenMethod));
            TypeWithOverriddenMethodDeclaration = typeWithOverriddenMethod;
        }
    }
}
