// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using System.Collections.Generic;
namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class ExecutionManagerTests
{
    private const ulong ExecutionManagerCodeRangeMapAddress = 0x000a_fff0;

    internal class ExecutionManagerTestTarget : TestPlaceholderTarget
    {
        internal class ExecutionManagerDataCache : TestDataCache
        {
            private readonly Target _target;
            public ExecutionManagerDataCache(Target target) : base()
            {
                _target = target;
            }

            public override T GetOrAdd<T>(TargetPointer address)
            {
                switch (address) {
                case ExecutionManagerCodeRangeMapAddress:
                    if (typeof(T) == typeof(Data.RangeSectionMap))
                        return (T)(object)new Data.RangeSectionMap(_target, address);
                    break;
                default:
                     break;
                }
                return base.GetOrAdd<T>(address);
            }
        }

        public ExecutionManagerTestTarget(MockTarget.Architecture arch, ReadFromTargetDelegate dataReader) : base(arch)
        {
            SetDataReader(dataReader);
            typeInfoCache = new Dictionary<DataType, TypeInfo>() {
                [DataType.RangeSectionMap] = new TypeInfo() {
                    Fields = new Dictionary<string, FieldInfo>() {
                        [nameof(Data.RangeSectionMap.TopLevelData)] = new () {Offset = 0}},
                    },
            };
            SetDataCache(new ExecutionManagerDataCache(this));
            IContractFactory<IExecutionManager> emfactory = new ExecutionManagerFactory();
            SetContracts(new TestRegistry() {
                ExecutionManagerContract = emfactory.CreateContract(this, 1),
            });
        }

        public override TargetPointer ReadGlobalPointer(string global)
        {
            switch (global)
            {
            case Constants.Globals.ExecutionManagerCodeRangeMapAddress:
                return new TargetPointer(ExecutionManagerCodeRangeMapAddress);
            default:
                return base.ReadGlobalPointer(global);
            }
        }

    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNull(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new (arch);
        MockMemorySpace.Builder builder = new (targetTestHelpers);
        RangeSectionMapTests.Builder rsmBuilder = new (ExecutionManagerCodeRangeMapAddress, builder);
        builder.MarkCreated();
        ExecutionManagerTestTarget target = new(arch, builder.GetReadContext().ReadFromTarget);
        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(TargetCodePointer.Null);
        Assert.Null(eeInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNonNullMissing(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new (arch);
        MockMemorySpace.Builder builder = new (targetTestHelpers);
        RangeSectionMapTests.Builder rsmBuilder = new (ExecutionManagerCodeRangeMapAddress, builder);
        builder.MarkCreated();
        ExecutionManagerTestTarget target = new(arch, builder.GetReadContext().ReadFromTarget);
        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(new TargetCodePointer(0x0a0a_0000));
        Assert.Null(eeInfo);
    }

}
