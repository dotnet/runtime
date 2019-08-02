using System;
using System.Collections.Generic;
using System.Net.Http.HPack;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Net.Http.Unit.Tests.HPack
{
    public class DynamicTableTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void DynamicTable_WrapsRingBuffer_Success(int targetInsertIndex)
        {
            FieldInfo insertIndexField = typeof(DynamicTable).GetField("_insertIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var table = new DynamicTable(maxSize: 256);
            var insertedHeaders = new Stack<byte[]>();

            // Insert into dynamic table until its insert index into its ring buffer loops back to 0.
            do
            {
                InsertOne();
            }
            while ((int)insertIndexField.GetValue(table) != 0);

            // Finally loop until the insert index reaches the target.
            while ((int)insertIndexField.GetValue(table) != targetInsertIndex)
            {
                InsertOne();
            }

            void InsertOne()
            {
                byte[] data = Encoding.ASCII.GetBytes($"header-{insertedHeaders.Count}");

                insertedHeaders.Push(data);
                table.Insert(data, data);
            }

            // Now check to see that we can retrieve the remaining headers.
            // Some headers will have been evacuated from the table during this process, so we don't exhaust the entire insertedHeaders stack.
            Assert.True(table.Count > 0);
            Assert.True(table.Count < insertedHeaders.Count);

            for (int i = 0; i < table.Count; ++i)
            {
                HeaderField dynamicField = table[i];
                byte[] expectedData = insertedHeaders.Pop();

                Assert.True(expectedData.AsSpan().SequenceEqual(dynamicField.Name));
                Assert.True(expectedData.AsSpan().SequenceEqual(dynamicField.Value));
            }
        }
    }
}
