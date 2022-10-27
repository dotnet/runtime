// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ExpectedInstructionSequenceOnMemberInAssemblyAttribute : BaseInAssemblyAttribute
    {
        public ExpectedInstructionSequenceOnMemberInAssemblyAttribute(string assemblyFileName, Type type, string memberName, string[] opCodes)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentNullException(nameof(assemblyFileName));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(memberName))
                throw new ArgumentNullException(nameof(memberName));
            if (opCodes == null)
                throw new ArgumentNullException(nameof(opCodes));
        }

        public ExpectedInstructionSequenceOnMemberInAssemblyAttribute(string assemblyFileName, string typeName, string memberName, string[] opCodes)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentNullException(nameof(assemblyFileName));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException(nameof(typeName));
            if (string.IsNullOrEmpty(memberName))
                throw new ArgumentNullException(nameof(memberName));
            if (opCodes == null)
                throw new ArgumentNullException(nameof(opCodes));
        }
    }
}
