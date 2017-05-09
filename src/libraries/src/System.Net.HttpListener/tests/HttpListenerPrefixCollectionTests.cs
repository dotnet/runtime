// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class HttpListenerPrefixCollectionTests
    {
        [Fact]
        public void Prefixes_Get_ReturnsEmpty()
        {
            var listener = new HttpListener();
            Assert.Empty(listener.Prefixes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void CopyTo_StringArray_ReturnsExpected(int offset)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");

            string[] array = new string[offset + 2];
            listener.Prefixes.CopyTo(array, offset);
            for (int i = 0; i < offset; i++)
            {
                Assert.Null(array[i]);
            }
            Assert.Equal("http://localhost:9200/", array[offset]);
            for (int i = offset + 1; i < array.Length; i++)
            {
                Assert.Null(array[i]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void CopyTo_Array_ReturnsExpected(int offset)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");

            object[] array = new object[offset + 2];
            listener.Prefixes.CopyTo(array, offset);
            for (int i = 0; i < offset; i++)
            {
                Assert.Null(array[i]);
            }
            Assert.Equal("http://localhost:9200/", array[offset]);
            for (int i = offset + 1; i < array.Length; i++)
            {
                Assert.Null(array[i]);
            }
        }

        [Fact]
        public void CopyTo_DisposedListener_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            HttpListenerPrefixCollection prefixes = listener.Prefixes;

            listener.Close();
            Assert.Throws<ObjectDisposedException>(() => prefixes.CopyTo((Array)new string[1], 0));
            Assert.Throws<ObjectDisposedException>(() => prefixes.CopyTo(new string[1], 0));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Core fixes a bug where HttpListenerPrefixCollection.CopyTo(null) throws an NRE.")]
        public void CopyTo_NullArray_ThrowsArgumentNullExceptionOnNetCore()
        {
            var listener = new HttpListener();
            Assert.Throws<ArgumentNullException>(() => listener.Prefixes.CopyTo((Array)null, 0));
            Assert.Throws<ArgumentNullException>(() => listener.Prefixes.CopyTo(null, 0));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, ".NET Core fixes a bug where HttpListenerPrefixCollection.CopyTo(null) throws an NRE.")]
        public void CopyTo_NullArray_ThrowsNullReferenceExceptionOnNetFx()
        {
            var listener = new HttpListener();
            Assert.Throws<NullReferenceException>(() => listener.Prefixes.CopyTo((Array)null, 0));
            Assert.Throws<NullReferenceException>(() => listener.Prefixes.CopyTo(null, 0));
        }

        [Fact]
        public void CopyTo_MultidimensionalArray_ThrowsIndexOutOfRangeException()
        {
            var listener = new HttpListener();

            // No exception thrown when empty.
            listener.Prefixes.CopyTo(new object[1, 1], 0);

            // Exception thrown when not empty.
            listener.Prefixes.Add("http://localhost:9200/");
            Assert.Throws<ArgumentException>(null, () => listener.Prefixes.CopyTo(new object[1, 1], 0));
        }

        [Fact]
        public void CopyTo_NonZeroLowerBoundArray_ThrowsIndexOutOfRangeException()
        {
            var listener = new HttpListener();
            
            // No exception thrown when empty.
            Array array = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { 1 });
            listener.Prefixes.CopyTo(array, 0);

            // Exception thrown when not empty.
            listener.Prefixes.Add("http://localhost:9200/");
            Assert.Throws<IndexOutOfRangeException>(() => listener.Prefixes.CopyTo(array, 0));
        }

        [Fact]
        public void CopyTo_InvalidArrayType_ThrowsInvalidCastException()
        {
            var listener = new HttpListener();

            // No exception thrown when empty.
            listener.Prefixes.CopyTo(new int[1], 0);

            // Exception thrown when not empty.
            listener.Prefixes.Add("http://localhost:9200/");
            Assert.Throws<InvalidCastException>(() => listener.Prefixes.CopyTo(new int[1], 0));
        }

        [Fact]
        public void CopyTo_ArrayTooSmall_ThrowsArgumentOutOfRangeException()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");
            Assert.Throws<ArgumentOutOfRangeException>("array", () => listener.Prefixes.CopyTo((Array)new string[0], 0));
            Assert.Throws<ArgumentOutOfRangeException>("array", () => listener.Prefixes.CopyTo(new string[0], 0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void CopyTo_InvalidOffset_ThrowsArgumentOutOfRangeException(int offset)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => listener.Prefixes.CopyTo((Array)new string[1], offset));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => listener.Prefixes.CopyTo(new string[1], offset));
        }

        [Fact]
        public void IsSynchronized_Get_ReturnsFalse()
        {
            var listener = new HttpListener();
            Assert.False(listener.Prefixes.IsSynchronized);
        }

        [Fact]
        public void IsReadOnly_Get_ReturnsFalse()
        {
            var listener = new HttpListener();
            Assert.False(listener.Prefixes.IsReadOnly);
        }

        [Theory]
        [InlineData("http://*/")]
        [InlineData("http://+/")]
        [InlineData("http://localhost/")]
        [InlineData("https://localhost/")]
        [InlineData("http://0.0.0.0/")]
        [InlineData("http://localhost:9200/")]
        [InlineData("https://localhost:9200/")]
        [InlineData("http://[fe80::70af:5aca:252a:3ca9]/")]
        public void Add_NotStarted_ReturnsExpected(string uriPrefix)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(uriPrefix);
            Assert.Equal(1, listener.Prefixes.Count);
            Assert.True(listener.Prefixes.Contains(uriPrefix));
        }

        [Fact]
        public void Add_AlreadyStarted_ReturnsExpected()
        {
            using (var factory = new HttpListenerFactory())
            {
                HttpListener listener = factory.GetListener();
                Assert.Single(listener.Prefixes);

                // Try to find a port that is not being used.
                for (int port = 1024; port <= IPEndPoint.MaxPort; port++)
                {
                    string uriPrefix = $"http://localhost:{port}/{factory.Path}/";
                    try
                    {
                        listener.Prefixes.Add(uriPrefix);
                        Assert.True(listener.Prefixes.Contains(uriPrefix));
                        Assert.Equal(2, listener.Prefixes.Count);

                        break;
                    }
                    catch (HttpListenerException)
                    {
                        // This port is already in use. Skip it and find a port that's not being used.
                    }
                }
            }
        }

        [Fact]
        public void Add_PrefixAlreadyRegisteredAndNotStarted_ThrowsHttpListenerException()
        {
            using (var factory = new HttpListenerFactory())
            {
                string uriPrefix = Assert.Single(factory.GetListener().Prefixes);

                var listener = new HttpListener();
                listener.Prefixes.Add(uriPrefix);

                Assert.Throws<HttpListenerException>(() => listener.Start());
            }
        }

        [Fact]
        public void Add_PrefixAlreadyRegisteredAndStarted_ThrowsHttpListenerException()
        {
            using (var factory = new HttpListenerFactory())
            {
                HttpListener listener = factory.GetListener();
                string uriPrefix = Assert.Single(listener.Prefixes);

                Assert.Throws<HttpListenerException>(() => listener.Prefixes.Add(uriPrefix));
                Assert.Throws<HttpListenerException>(() => listener.Prefixes.Add(uriPrefix + "/sub_path/"));
            }
        }

        public static IEnumerable<object[]> InvalidPrefix_TestData()
        {
            yield return new object[] { "http://microsoft.com/" };
            yield return new object[] { "http://[]/" };
            yield return new object[] { "http://[::1%2]/" };
            yield return new object[] { "http://[::]/" };
            yield return new object[] { "http://localhost:-1/" };
            yield return new object[] { "http://localhost:0/" };
            yield return new object[] { "http://localhost:65536/" };
            yield return new object[] { "http://localhost:trash/" };
            yield return new object[] { "http://localhost/invalid%path/" };
            yield return new object[] { "http://./" };
            yield return new object[] { "http://\\/" };
        }

        [ActiveIssue(19526)]
        [Theory]
        [MemberData(nameof(InvalidPrefix_TestData))]
        public void Add_InvalidPrefixNotStarted_ThrowsHttpListenerExceptionOnStart(string uriPrefix)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(uriPrefix);

            Assert.Equal(1, listener.Prefixes.Count);
            Assert.True(listener.Prefixes.Contains(uriPrefix));
            Assert.Throws<HttpListenerException>(() => listener.Start());
        }

        [ActiveIssue(19526)]
        [Theory]
        [MemberData(nameof(InvalidPrefix_TestData))]
        public void Add_InvalidPrefixAlreadyStarted_ThrowsHttpListenerExceptionOnAdd(string uriPrefix)
        {
            using (var factory = new HttpListenerFactory())
            {
                HttpListener listener = factory.GetListener();
                    Assert.Single(listener.Prefixes);

                Assert.Throws<HttpListenerException>(() => listener.Prefixes.Add(uriPrefix));
            }
        }

        [Theory]
        [ActiveIssue(18128, TestPlatforms.AnyUnix)] // Fails by design on Windows but is allowed by the managed implementation
        [InlineData("http://192./")]
        public void Add_InvalidPrefix_ThrowsHttpListenerException_Windows(string uriPrefix)
        {
            Add_InvalidPrefixNotStarted_ThrowsHttpListenerExceptionOnStart(uriPrefix);
            Add_InvalidPrefixAlreadyStarted_ThrowsHttpListenerExceptionOnAdd(uriPrefix);
        }

        [Theory]
        [InlineData("")]
        [InlineData("http")]
        [InlineData("https")]
        [InlineData("https://")]
        [InlineData("http://")]
        [InlineData("ftp://connection")]
        [InlineData("http://[")]
        [InlineData("http://[[]/")]
        [InlineData("http://localhost:9200")]
        [InlineData("http://localhost/path")]
        [InlineData("http://::/")]
        [InlineData("http://::/")]
        public void Add_InvalidPrefix_ThrowsArgumentException(string uriPrefix)
        {
            var listener = new HttpListener();
            Assert.Throws<ArgumentException>("uriPrefix", () => listener.Prefixes.Add(uriPrefix));

            // If the prefix was invalid, it shouldn't be added to the list.
            Assert.Empty(listener.Prefixes);
        }

        [Fact]
        public void Add_NullPrefix_ThrowsArgumentNullException()
        {
            var listener = new HttpListener();
            Assert.Throws<ArgumentNullException>("uriPrefix", () => listener.Prefixes.Add(null));
        }

        [Fact]
        public void Add_LongHost_ThrowsArgumentOutOfRangeException()
        {
            var listener = new HttpListener();
            string longPrefix = "http://" + new string('a', 256) + "/";
            Assert.Throws<ArgumentOutOfRangeException>("hostName", () => listener.Prefixes.Add(longPrefix));

            // Ouch: even though adding the prefix threw an exception, the prefix was still added.
            Assert.Equal(1, listener.Prefixes.Count);
            Assert.True(listener.Prefixes.Contains(longPrefix));

            Assert.Throws<HttpListenerException>(() => listener.Start());
        }

        [Fact]
        public void Add_DisposedListener_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            HttpListenerPrefixCollection prefixes = listener.Prefixes;

            listener.Close();
            Assert.Throws<ObjectDisposedException>(() => prefixes.Add("http://localhost:9200/"));
            Assert.Throws<ObjectDisposedException>(() => prefixes.Add("http://localhost:9200/"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("http://localhost:80/")]
        [InlineData("http://localhost:9200")]
        [InlineData("https://localhost:9200/")]
        [InlineData("HTTP://LOCALHOST:9200/")]
        public void Contains_NoSuchPrefix_ReturnsFalse(string uriPrefix)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");
            Assert.False(listener.Prefixes.Contains(uriPrefix));
        }

        [Fact]
        public void Contains_NullPrefix_ThrowsArgumentNullException()
        {
            var listener = new HttpListener();
            Assert.Throws<ArgumentNullException>("key", () => listener.Prefixes.Contains(null));
        }

        [Fact]
        public void Remove_PrefixExistsNotStarted_ReturnsTrue()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");

            Assert.True(listener.Prefixes.Remove("http://localhost:9200/"));
            Assert.False(listener.Prefixes.Contains("http://localhost:9200/"));
            Assert.Equal(0, listener.Prefixes.Count);
        }

        [Fact]
        public async Task Remove_PrefixExistsStarted_ReturnsTrue()
        {
            using (var factory = new HttpListenerFactory())
            {
                HttpListener listener = factory.GetListener();
                string uriPrefix = Assert.Single(listener.Prefixes);

                Assert.True(listener.Prefixes.Remove(uriPrefix));
                Assert.False(listener.Prefixes.Contains(uriPrefix));
                Assert.Equal(0, listener.Prefixes.Count);

                // Trying to connect to the HttpListener should now fail.
                using (var client = new HttpClient())
                {
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStringAsync(factory.ListeningUrl));
                }
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("http://localhost:80/")]
        [InlineData("http://localhost:9200")]
        [InlineData("https://localhost:9200/")]
        [InlineData("HTTP://LOCALHOST:9200/")]
        public void Remove_NoSuchPrefix_ReturnsFalse(string uriPrefix)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");
            Assert.False(listener.Prefixes.Remove(uriPrefix));
        }

        [Fact]
        public void Remove_DisposedListener_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            HttpListenerPrefixCollection prefixes = listener.Prefixes;

            listener.Close();
            Assert.Throws<ObjectDisposedException>(() => prefixes.Remove("http://localhost:9200/"));
        }

        [Fact]
        public void Remove_NullPrefix_ThrowsArgumentNullException()
        {
            var listener = new HttpListener();
            Assert.Throws<ArgumentNullException>("uriPrefix", () => listener.Prefixes.Remove(null));
        }

        [Fact]
        public void Clear_NonEmpty_Success()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");

            listener.Prefixes.Clear();
            Assert.False(listener.Prefixes.Contains("http://localhost:9200/"));
            Assert.Equal(0, listener.Prefixes.Count);
        }

        [Fact]
        public void Clear_DisposedListener_ThrowsObjectDisposedException()
        {
            var listener = new HttpListener();
            HttpListenerPrefixCollection prefixes = listener.Prefixes;

            listener.Close();
            Assert.Throws<ObjectDisposedException>(() => prefixes.Clear());
        }

        [Fact]
        public void GetEnumeratorGeneric_ResetMultipleTimes_ReturnsExpected()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");
            listener.Prefixes.Add("http://localhost:5601/");
            string[] prefixes = listener.Prefixes.ToArray();

            IEnumerator<string> enumerator = listener.Prefixes.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                while (enumerator.MoveNext())
                {
                    Assert.Equal(prefixes[counter], enumerator.Current);
                    counter++;
                }

                Assert.Equal(listener.Prefixes.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public void GetEnumeratorNonGeneric_ResetMultipleTimes_ReturnsExpected()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:9200/");
            listener.Prefixes.Add("http://localhost:5601/");
            string[] prefixes = listener.Prefixes.ToArray();

            IEnumerator enumerator = ((IEnumerable)listener.Prefixes).GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                while (enumerator.MoveNext())
                {
                    Assert.Equal(prefixes[counter], enumerator.Current);
                    counter++;
                }

                Assert.Equal(listener.Prefixes.Count, counter);
                enumerator.Reset();
            }
        }
    }
}
