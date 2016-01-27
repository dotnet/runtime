// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Concurrent.Tests
{
    public class PartitionerTests
    {
        [Fact]
        public void BaseVirtuals_VerifyDefaults()
        {
            CustomPartitioner<int> p = new CustomPartitioner<int>();
            Assert.False(p.SupportsDynamicPartitions);
            Assert.Throws<NotSupportedException>(() => p.GetDynamicPartitions());
        }

        private class CustomPartitioner<T> : Partitioner<T>
        {
            public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
            {
                throw new NotImplementedException();
            }
        }
    }
}
