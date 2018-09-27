// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using System.Threading;

namespace System.Runtime.Loader.Tests
{

    public partial class AssemblyLoadContextTest
    {
        class COMInteropTest : TestBase
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Execute()
            {
                // COM Interop is not supported for collectible types.
                Assert.Throws<NotSupportedException>(() =>
                {
                    Type contextMenuType = _testClassTypes[0].GetNestedType("COMClass");
                    Assert.NotNull(contextMenuType);
                    Assert.True(contextMenuType.IsCOMObject);
                    object contextMenu = Activator.CreateInstance(contextMenuType);
                    Assert.NotNull(contextMenu);
                });
            }
        }

        [Fact]
        public static void Unsupported_COMInterop()
        {
            var test = new COMInteropTest();
            test.CreateContextAndLoadAssembly();
            test.Execute();
            test.UnloadAndClearContext();
            test.CheckContextUnloaded();
        }

    }
}
