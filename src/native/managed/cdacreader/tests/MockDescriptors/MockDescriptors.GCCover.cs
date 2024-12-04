// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class GCCover
    {
        internal readonly MockMemorySpace.Builder Builder;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        private CodeVersions _codeVersions { get; }
        public GCCover(MockTarget.Architecture arch)
            : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)))
        { }

        public GCCover(MockMemorySpace.Builder builder)
        {
            Builder = builder;

            _codeVersions = new CodeVersions(Builder);

            Types = CodeVersions.GetTypes(builder.TargetTestHelpers);
        }

        public NativeCodeVersionHandle AddExplicitNativeCodeVersion(TargetPointer gcCoverPointer)
        {
            TargetPointer nativeCodeVersionNode = _codeVersions.AddNativeCodeVersionNode();
            _codeVersions.FillNativeCodeVersionNode(
                nativeCodeVersionNode,
                TargetPointer.Null,
                TargetCodePointer.Null,
                TargetPointer.Null,
                true,
                new(1),
                gcCoverPointer);

            return NativeCodeVersionHandle.CreateExplicit(nativeCodeVersionNode);
        }
    }
}
