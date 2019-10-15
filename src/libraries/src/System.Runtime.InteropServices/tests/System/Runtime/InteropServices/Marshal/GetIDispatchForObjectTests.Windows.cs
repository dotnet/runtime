// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetIDispatchForObjectTests
    {
        public static IEnumerable<object[]> GetIUnknownForObject_ComObject_TestData()
        {
            yield return new object[] { new ComImportObject() };

            yield return new object[] { new DualComObject() };
            yield return new object[] { new IUnknownComObject() };
            yield return new object[] { new IDispatchComObject() };
            yield return new object[] { new IInspectableComObject() };

            yield return new object[] { new NonDualComObject() };
            yield return new object[] { new AutoDispatchComObject() };
            yield return new object[] { new AutoDualComObject() };

            yield return new object[] { new NonDualComObjectEmpty() };
            yield return new object[] { new AutoDispatchComObjectEmpty() };
            yield return new object[] { new AutoDualComObjectEmpty() };
        }
    }
}
