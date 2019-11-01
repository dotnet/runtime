// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class ComponentModelTests
    {
        [Fact]
        public static void SystemComponentModel_Interfaces_LoadedSuccessfully()
        {
            IRevertibleChangeTracking iRevertibleChangeTracking = null;
            IChangeTracking iChangeTracking = iRevertibleChangeTracking;
            Assert.Null(iChangeTracking);

            IEditableObject iEditableObject = null;
            Assert.Null(iEditableObject);

            IServiceProvider iServiceProvider = null;
            Assert.Null(iServiceProvider);
        }
    }
}
