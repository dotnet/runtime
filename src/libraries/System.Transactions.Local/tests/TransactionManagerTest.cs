// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Transactions.Tests
{
    public class TransactionManagerTest
    {
        [Fact]
        public void DefaultTimeout_Set_LessThanMaximum()
        {
            TimeSpan tsDefault = TimeSpan.Parse("00:02:00");
            TransactionManager.DefaultTimeout = tsDefault;

            Assert.Equal(tsDefault, TransactionManager.DefaultTimeout);
        }

        [Fact]
        public void DefaultTimeout_Set_ExceedMaximum()
        {
            TimeSpan ts = TransactionManager.MaximumTimeout.Add(TimeSpan.FromMinutes(10));
            TransactionManager.DefaultTimeout = ts;

            Assert.Equal(TransactionManager.DefaultTimeout, TransactionManager.MaximumTimeout);
        }
       
        [Fact]
        public void DefaultTimeout_Set_Negative()
        {
            TimeSpan ts = TimeSpan.Parse("-00:01:00");
            Assert.Throws<ArgumentOutOfRangeException>(() => TransactionManager.DefaultTimeout = ts);
        }

        [Fact]
        public void MaximumTimeout_Set_Positive()
        {
            TimeSpan ts = TimeSpan.Parse("00:30:00");
            TransactionManager.MaximumTimeout = ts;

            Assert.Equal(ts, TransactionManager.MaximumTimeout);
        }

        [Fact]
        public void MaximumTimeout_Set_Negative()
        {
            TimeSpan ts = TimeSpan.Parse("-00:10:00");
            Assert.Throws<ArgumentOutOfRangeException>(() => TransactionManager.MaximumTimeout = ts);
        }
    }
}
