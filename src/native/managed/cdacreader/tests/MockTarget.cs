// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class MockTarget
{
    public record struct Architecture
    {
        public bool IsLittleEndian { get; init; }
        public bool Is64Bit { get; init; }
    }

    /// <summary>
    /// Xunit enumeration of standard test architectures
    /// </summary>
    /// <example>
    /// [Theory]
    /// [ClassData(typeof(MockTarget.StdArch))]
    /// public void TestMethod(MockTarget.Architecture arch)
    /// {
    ///    ...
    /// }
    /// </example>
    public class StdArch : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [new Architecture { IsLittleEndian = true, Is64Bit = true }];
            yield return [new Architecture { IsLittleEndian = true, Is64Bit = false }];
            yield return [new Architecture { IsLittleEndian = false, Is64Bit = true }];
            yield return [new Architecture { IsLittleEndian = false, Is64Bit = false }];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
