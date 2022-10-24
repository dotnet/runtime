// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public class ExpectedLocalsSequenceAttribute : BaseInAssemblyAttribute
    {
        public ExpectedLocalsSequenceAttribute(string[] types)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));
        }

        public ExpectedLocalsSequenceAttribute(Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));
        }
    }
}
