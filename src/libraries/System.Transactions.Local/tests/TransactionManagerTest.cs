// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Transactions.Tests
{
    public class TransactionManagerTest
    {
        [Fact]
        public void DefaultTimeout_MaxTimeout_Set_Get()
        {
            TimeSpan tsDefault = TimeSpan.Parse("00:02:00");
            TransactionManager.DefaultTimeout = tsDefault;
            Assert.Equal(tsDefault, TransactionManager.DefaultTimeout);

            TimeSpan tsMax = TimeSpan.Parse("00:30:00");            
            TransactionManager.MaximumTimeout = tsMax;
            Assert.Equal(tsMax, TransactionManager.MaximumTimeout);            

            TimeSpan ts = TransactionManager.MaximumTimeout.Add(TimeSpan.FromMinutes(10));
            TransactionManager.DefaultTimeout = ts;
            Assert.Equal(tsMax, TransactionManager.MaximumTimeout);
            Assert.Equal(TransactionManager.DefaultTimeout, TransactionManager.MaximumTimeout);

            ts = TimeSpan.Parse("-00:01:00");
            Assert.Throws<ArgumentOutOfRangeException>(() => TransactionManager.DefaultTimeout = ts);
            Assert.Throws<ArgumentOutOfRangeException>(() => TransactionManager.MaximumTimeout = ts);
        }
    }
}
