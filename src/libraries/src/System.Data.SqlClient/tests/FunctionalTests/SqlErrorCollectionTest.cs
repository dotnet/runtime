// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlErrorCollectionTest
    {
        private const string badServer = "92B96911A0BD43E8ADA4451031F7E7CF";

        [Fact]
        public void IsSynchronized_Success()
        {
            ICollection c = CreateCollection();
            Assert.False(c.IsSynchronized);
        }

        [Fact]
        public void SyncRoot_Success()
        {
            ICollection c = CreateCollection();
            Assert.Same(c, c.SyncRoot);
        }

        [Fact]
        public void Indexer_Success()
        {
            SqlErrorCollection c = CreateCollection();
            Assert.NotNull(c[0]);
            Assert.Same(c[0], c[0]);
        }

        [Fact]
        public void Indexer_Throws()
        {
            SqlErrorCollection c = CreateCollection();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => c[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => c[c.Count]);
        }

        [Fact]
        public void CopyTo_Success()
        {
            ValidateCopyTo((collection, array, index) => collection.CopyTo(array, index));
        }

        [Fact]
        public void CopyTo_NonGeneric_Success()
        {
            ValidateCopyTo((collection, array, index) => ((ICollection)collection).CopyTo(array, index));
        }

        private static void ValidateCopyTo(Action<SqlErrorCollection, SqlError[], int> copyTo)
        {
            SqlErrorCollection c = CreateCollection();
            SqlError[] destination = new SqlError[5];

            copyTo(c, destination, 2);

            Assert.Null(destination[0]);
            Assert.Null(destination[1]);
            Assert.Same(c[0], destination[2]);
            Assert.Null(destination[3]);
            Assert.Null(destination[4]);
        }

        [Fact]
        public void CopyTo_Throws()
        {
            ValidateCopyToThrows((collection, array, index) => collection.CopyTo(array, index));
        }

        [Fact]
        public void CopyTo_NonGeneric_Throws()
        {
            ValidateCopyToThrows((collection, array, index) => ((ICollection)collection).CopyTo(array, index), c =>
            {
                ICollection ic = c;
                AssertExtensions.Throws<ArgumentException>(null, () => ic.CopyTo(new SqlError[4, 3], 0));
                Assert.Throws<InvalidCastException>(() => ic.CopyTo(new string[10], 0));
            });
        }

        private static void ValidateCopyToThrows(
            Action<SqlErrorCollection, SqlError[], int> copyTo,
            Action<SqlErrorCollection> additionalValidation = null)
        {
            SqlErrorCollection c = CreateCollection();

            Assert.Throws<ArgumentNullException>(() => copyTo(c, null, 0));
            Assert.Throws<ArgumentNullException>(() => copyTo(c, null, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => copyTo(c, new SqlError[10], -1));
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => copyTo(c, new SqlError[10], 1000));

            additionalValidation?.Invoke(c);
        }

        [Fact]
        public void GetEnumerator_Success()
        {
            SqlErrorCollection c = CreateCollection();

            IEnumerator e = c.GetEnumerator();

            Assert.NotNull(e);
            Assert.NotSame(e, c.GetEnumerator());

            for (int i = 0; i < 2; i++)
            {
                Assert.Throws<InvalidOperationException>(() => e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(c[0], e.Current);

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                Assert.Throws<InvalidOperationException>(() => e.Current);

                e.Reset();
            }
        }

        private static SqlErrorCollection CreateCollection()
        {
            var builder = new SqlConnectionStringBuilder()
            {
                DataSource = badServer,
                ConnectTimeout = 1,
                Pooling = false
            };

            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (SqlException ex)
                {
                    Assert.NotNull(ex.Errors);
                    Assert.Equal(1, ex.Errors.Count);

                    return ex.Errors;
                }
            }

            throw new InvalidOperationException("SqlException.Errors should have been returned.");
        }
    }
}
