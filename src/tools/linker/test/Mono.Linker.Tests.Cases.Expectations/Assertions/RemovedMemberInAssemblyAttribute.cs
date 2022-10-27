// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
    public class RemovedMemberInAssemblyAttribute : BaseInAssemblyAttribute
    {

        public RemovedMemberInAssemblyAttribute(string assemblyFileName, Type type, params string[] memberNames)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentNullException(nameof(assemblyFileName));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (memberNames == null)
                throw new ArgumentNullException(nameof(memberNames));
        }

        public RemovedMemberInAssemblyAttribute(string assemblyFileName, string typeName, params string[] memberNames)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentNullException(nameof(assemblyFileName));
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));
            if (memberNames == null)
                throw new ArgumentNullException(nameof(memberNames));
        }
    }
}
