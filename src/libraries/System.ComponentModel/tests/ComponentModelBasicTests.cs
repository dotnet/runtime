// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using Xunit;

namespace Test
{
    public class ComponentModelTests
    {
        [Fact]
        public static void TestComponentModelBasic()
        {
            // dummy tests to make sure the System.ComponentModel library loaded successfully
#pragma warning disable 0219
            IRevertibleChangeTracking iRevertibleChangeTracking = null;
            IChangeTracking iChangeTracking = iRevertibleChangeTracking;
            Assert.Null(iChangeTracking);

            IEditableObject iEditableObject = null;
            CancelEventArgs cancelEventArgs = new CancelEventArgs();
            Assert.NotNull(cancelEventArgs);

            IServiceProvider iServiceProvider = null;
            Assert.Null(iServiceProvider);
#pragma warning restore 0219
        }
    }
}
