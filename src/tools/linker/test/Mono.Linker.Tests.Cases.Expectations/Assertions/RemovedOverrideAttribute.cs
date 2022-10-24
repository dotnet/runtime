// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    /// <Summary>
    /// Used to ensure that a method should remove an 'override' annotation for a method in the supplied base type.
    /// Fails in tests if the method has the override method in the linked assembly,
    ///		or if the override is not found in the original assembly
    /// </Summary>
    /// <seealso cref="KeptOverrideAttribute" />
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class RemovedOverrideAttribute : BaseInAssemblyAttribute
    {
        public Type TypeWithOverriddenMethodDeclaration;
        public RemovedOverrideAttribute(Type typeWithOverriddenMethod)
        {
            if (typeWithOverriddenMethod == null)
                throw new ArgumentException("Value cannot be null or empty.", nameof(typeWithOverriddenMethod));
            TypeWithOverriddenMethodDeclaration = typeWithOverriddenMethod;
        }
    }
}
