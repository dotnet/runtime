// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class StandardToolWindowsTests
    {
        public static IEnumerable<object[]> StandardToolWindows_TestData()
        {
            yield return new object[] { StandardToolWindows.ObjectBrowser, new Guid("970d9861-ee83-11d0-a778-00a0c91110c3") };
            yield return new object[] { StandardToolWindows.OutputWindow, new Guid("34e76e81-ee4a-11d0-ae2e-00a0c90fffc3") };
            yield return new object[] { StandardToolWindows.ProjectExplorer, new Guid("3ae79031-e1bc-11d0-8f78-00a0c9110057") };
            yield return new object[] { StandardToolWindows.PropertyBrowser, new Guid("eefa5220-e298-11d0-8f78-00a0c9110057") };
            yield return new object[] { StandardToolWindows.RelatedLinks, new Guid("66dba47c-61df-11d2-aa79-00c04f990343") };
            yield return new object[] { StandardToolWindows.ServerExplorer, new Guid("74946827-37a0-11d2-a273-00c04f8ef4ff") };
            yield return new object[] { StandardToolWindows.TaskList, new Guid("4a9b7e51-aa16-11d0-a8c5-00a0c921a4d2") };
            yield return new object[] { StandardToolWindows.Toolbox, new Guid("b1e99781-ab81-11d0-b683-00aa00a3ee26") };
        }

        [Theory]
        [MemberData(nameof(StandardToolWindows_TestData))]
        public void StandardToolWindows_Get_ReturnsExpected(Guid guid, Guid expected)
        {
            Assert.Equal(expected, guid);
        }
    }
}
