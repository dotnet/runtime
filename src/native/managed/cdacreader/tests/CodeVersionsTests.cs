// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class CodeVersionsTests
{
    internal class MockExecutionManager : IExecutionManager
    {
        CodeBlockHandle? IExecutionManager.GetCodeBlockHandle(TargetCodePointer ip)
        {
            if (ip == TargetCodePointer.Null)
            {
                return null;
            }
            throw new NotImplementedException();
        }
    }
    internal class CVTestTarget : TestPlaceholderTarget
    {
        public CVTestTarget(MockTarget.Architecture arch) : base(arch) {
            IContractFactory<ICodeVersions> cvfactory = new CodeVersionsFactory();
            IExecutionManager mockExecutionManager = new MockExecutionManager();
            SetContracts(new TestRegistry() {
                CodeVersionsContract = new (() => cvfactory.CreateContract(this, 1)),
                ExecutionManagerContract = new (() => mockExecutionManager),
            });

        }


    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TestCodeVersionNull(MockTarget.Architecture arch)
    {
        var target = new CVTestTarget(arch);
        var codeVersions = target.Contracts.CodeVersions;

        Assert.NotNull(codeVersions);

        TargetCodePointer nullPointer = TargetCodePointer.Null;

        var handle = codeVersions.GetNativeCodeVersionForIP(nullPointer);
        Assert.False(handle.Valid);
    }
}
