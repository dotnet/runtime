// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Data.Common;

using Xunit;

namespace System.Data.Tests.Common
{
    public class DbDataAdapterTest
    {
        [Fact]
        public void UpdateBatchSize()
        {
            MyAdapter da = new MyAdapter();
            try
            {
                da.UpdateBatchSize = 0;
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                // Specified method is not supported
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
            Assert.Equal(1, da.UpdateBatchSize);

            try
            {
                da.UpdateBatchSize = int.MaxValue;
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                // Specified method is not supported
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
            Assert.Equal(1, da.UpdateBatchSize);

            da.UpdateBatchSize = 1;
            Assert.Equal(1, da.UpdateBatchSize);
        }

        [Fact]
        public void UpdateBatchSize_Negative()
        {
            MyAdapter da = new MyAdapter();
            try
            {
                da.UpdateBatchSize = -1;
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                // Specified method is not supported
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void ClearBatch()
        {
            MyAdapter da = new MyAdapter();
            try
            {
                da.ClearBatch();
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void ExecuteBatch()
        {
            MyAdapter da = new MyAdapter();
            try
            {
                da.ExecuteBatch();
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void GetBatchedParameter()
        {
            MyAdapter da = new MyAdapter();
            try
            {
                da.GetBatchedParameter(1, 1);
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void GetBatchedRecordsAffected()
        {
            MyAdapter da = new MyAdapter();
            int recordsAffected = 0;
            Exception error = null;

            Assert.True(da.GetBatchedRecordsAffected(int.MinValue, out recordsAffected, out error));
            Assert.Equal(1, recordsAffected);
            Assert.Null(error);
        }

        [Fact]
        public void InitializeBatching()
        {
            MyAdapter da = new MyAdapter();
            try
            {
                da.InitializeBatching();
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void TerminateBatching()
        {
            MyAdapter da = new MyAdapter();
            try
            {
                da.TerminateBatching();
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        private class MyAdapter : DbDataAdapter
        {
            public new int AddToBatch(IDbCommand command)
            {
                return base.AddToBatch(command);
            }

            public new void ClearBatch()
            {
                base.ClearBatch();
            }

            public new void ExecuteBatch()
            {
                base.ClearBatch();
            }

            public new IDataParameter GetBatchedParameter(int commandIdentifier, int parameterIndex)
            {
                return base.GetBatchedParameter(commandIdentifier, parameterIndex);
            }

            public new bool GetBatchedRecordsAffected(int commandIdentifier, out int recordsAffected, out Exception error)
            {
                return base.GetBatchedRecordsAffected(commandIdentifier, out recordsAffected, out error);
            }

            public new void InitializeBatching()
            {
                base.InitializeBatching();
            }

            public new void TerminateBatching()
            {
                base.TerminateBatching();
            }
        }
    }
}
