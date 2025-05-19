// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Data.Tests
{
    public class DataTableComputeTest
    {
        [Fact]
        public void Compute_TrueNotEqualsFalse()
        {
            // Test expressions mentioned in the issue
            bool result1 = (bool)new DataTable().Compute("1 NOT= 2", null);
            Assert.True(result1);
            
            bool result2 = (bool)new DataTable().Compute("true = false", null);
            Assert.False(result2);
            
            bool result3 = (bool)new DataTable().Compute("true NOT= false", null);
            Assert.True(result3);
            
            bool result4 = (bool)new DataTable().Compute("NOT(true = false)", null);
            Assert.True(result4);
        }
    }
}