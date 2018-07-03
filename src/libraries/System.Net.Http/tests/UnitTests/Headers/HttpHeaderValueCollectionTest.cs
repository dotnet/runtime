// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpHeaderValueCollectionTest
    {
        // Note: These are not real known headers, so they won't be returned if we call HeaderDescriptor.Get().
        private static readonly HeaderDescriptor knownStringHeader = (new KnownHeader("known-string-header", HttpHeaderType.General, new MockHeaderParser(typeof(string)))).Descriptor;
        private static readonly HeaderDescriptor knownUriHeader = (new KnownHeader("known-uri-header", HttpHeaderType.General, new MockHeaderParser(typeof(Uri)))).Descriptor;

        private static readonly Uri specialValue = new Uri("http://special/");
        private static readonly Uri invalidValue = new Uri("http://invalid/");
        private static readonly TransferCodingHeaderValue specialChunked = new TransferCodingHeaderValue("chunked");

        // Note that this type just forwards calls to HttpHeaders. So this test method focuses on making sure 
        // the correct calls to HttpHeaders are made. This test suite will not test HttpHeaders functionality.

        [Fact]
        public void IsReadOnly_CallProperty_AlwaysFalse()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<string> collection = new HttpHeaderValueCollection<string>(knownStringHeader, headers);

            Assert.False(collection.IsReadOnly);
        }

        [Fact]
        public void Count_AddSingleValueThenQueryCount_ReturnsValueCountWithSpecialValues()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<string> collection = new HttpHeaderValueCollection<string>(knownStringHeader, headers,
                "special");

            Assert.Equal(0, collection.Count);

            headers.Add(knownStringHeader, "value2");
            Assert.Equal(1, collection.Count);

            headers.Clear();
            headers.Add(knownStringHeader, "special");
            Assert.Equal(1, collection.Count);
            headers.Add(knownStringHeader, "special");
            headers.Add(knownStringHeader, "special");
            Assert.Equal(3, collection.Count);
        }

        [Fact]
        public void Count_AddMultipleValuesThenQueryCount_ReturnsValueCountWithSpecialValues()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<string> collection = new HttpHeaderValueCollection<string>(knownStringHeader, headers,
                "special");

            Assert.Equal(0, collection.Count);

            collection.Add("value1");
            headers.Add(knownStringHeader, "special");
            Assert.Equal(2, collection.Count);

            headers.Add(knownStringHeader, "special");
            headers.Add(knownStringHeader, "value2");
            headers.Add(knownStringHeader, "special");
            Assert.Equal(5, collection.Count);
        }

        [Fact]
        public void Add_CallWithNullValue_Throw()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            Assert.Throws<ArgumentNullException>(() => { collection.Add(null); });
        }

        [Fact]
        public void Add_AddValues_AllValuesAdded()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        public void Add_UseSpecialValue_Success()
        {
            HttpRequestHeaders headers = new HttpRequestHeaders();
            Assert.Null(headers.TransferEncodingChunked);
            Assert.Equal(0, headers.TransferEncoding.Count);
            Assert.Equal(string.Empty, headers.TransferEncoding.ToString());

            headers.TransferEncoding.Add(specialChunked);

            Assert.True((bool)headers.TransferEncodingChunked);
            Assert.Equal(1, headers.TransferEncoding.Count);
            Assert.Equal(specialChunked, headers.TransferEncoding.First());
            Assert.Equal(specialChunked.ToString(), headers.TransferEncoding.ToString());
        }

        [Fact]
        public void Add_UseSpecialValueWithSpecialAlreadyPresent_AddsDuplicate()
        {
            HttpRequestHeaders headers = new HttpRequestHeaders();
            headers.TransferEncodingChunked = true;

            Assert.True((bool)headers.TransferEncodingChunked);
            Assert.Equal(1, headers.TransferEncoding.Count);
            Assert.Equal(specialChunked.ToString(), headers.TransferEncoding.ToString());

            headers.TransferEncoding.Add(specialChunked);

            Assert.True((bool)headers.TransferEncodingChunked);
            Assert.Equal(2, headers.TransferEncoding.Count);
            Assert.Equal("chunked, chunked", headers.TransferEncoding.ToString());

            // removes first instance of
            headers.TransferEncodingChunked = false;

            Assert.True((bool)headers.TransferEncodingChunked);
            Assert.Equal(1, headers.TransferEncoding.Count);
            Assert.Equal(specialChunked.ToString(), headers.TransferEncoding.ToString());

            // does not add duplicate
            headers.TransferEncodingChunked = true;

            Assert.True((bool)headers.TransferEncodingChunked);
            Assert.Equal(1, headers.TransferEncoding.Count);
            Assert.Equal(specialChunked.ToString(), headers.TransferEncoding.ToString());
        }

        [Fact]
        public void ParseAdd_CallWithNullValue_NothingAdded()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.ParseAdd(null);
            Assert.False(collection.IsSpecialValueSet);
            Assert.Equal(0, collection.Count);
            Assert.Equal(string.Empty, collection.ToString());
        }

        [Fact]
        public void ParseAdd_AddValues_AllValuesAdded()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.ParseAdd("http://www.example.org/1/");
            collection.ParseAdd("http://www.example.org/2/");
            collection.ParseAdd("http://www.example.org/3/");

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        public void ParseAdd_UseSpecialValue_Added()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.ParseAdd(specialValue.AbsoluteUri);

            Assert.True(collection.IsSpecialValueSet);
            Assert.Equal(specialValue.ToString(), collection.ToString());
        }

        [Fact]
        public void ParseAdd_AddBadValue_Throws()
        {
            HttpResponseHeaders headers = new HttpResponseHeaders();
            string input = "Basic, D\rigest qop=\"auth\",algorithm=MD5-sess";
            
            Assert.Throws<FormatException>(() => { headers.WwwAuthenticate.ParseAdd(input); });
        }

        [Fact]
        public void TryParseAdd_CallWithNullValue_NothingAdded()
        {
            HttpResponseHeaders headers = new HttpResponseHeaders();

            Assert.True(headers.WwwAuthenticate.TryParseAdd(null));

            Assert.False(headers.WwwAuthenticate.IsSpecialValueSet);
            Assert.Equal(0, headers.WwwAuthenticate.Count);
            Assert.Equal(string.Empty, headers.WwwAuthenticate.ToString());
        }

        [Fact]
        public void TryParseAdd_AddValues_AllAdded()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            Assert.True(collection.TryParseAdd("http://www.example.org/1/"));
            Assert.True(collection.TryParseAdd("http://www.example.org/2/"));
            Assert.True(collection.TryParseAdd("http://www.example.org/3/"));

            Assert.Equal(3, collection.Count);
        }

        [Fact]
        public void TryParseAdd_UseSpecialValue_Added()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            Assert.True(collection.TryParseAdd(specialValue.AbsoluteUri));

            Assert.True(collection.IsSpecialValueSet);
            Assert.Equal(specialValue.ToString(), collection.ToString());
        }

        [Fact]
        public void TryParseAdd_AddBadValue_False()
        {
            HttpResponseHeaders headers = new HttpResponseHeaders();
            string input = "Basic, D\rigest qop=\"auth\",algorithm=MD5-sess";
            Assert.False(headers.WwwAuthenticate.TryParseAdd(input));
            Assert.Equal(string.Empty, headers.WwwAuthenticate.ToString());
            Assert.Equal(string.Empty, headers.ToString());
        }

        [Fact]
        public void TryParseAdd_AddBadAfterGoodValue_False()
        {
            HttpResponseHeaders headers = new HttpResponseHeaders();
            headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Negotiate"));
            string input = "Basic, D\rigest qop=\"auth\",algorithm=MD5-sess";
            Assert.False(headers.WwwAuthenticate.TryParseAdd(input));
            Assert.Equal("Negotiate", headers.WwwAuthenticate.ToString());
            Assert.Equal("WWW-Authenticate: Negotiate\r\n", headers.ToString());
        }

        [Fact]
        public void Clear_AddValuesThenClear_NoElementsInCollection()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));

            Assert.Equal(3, collection.Count);

            collection.Clear();

            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void Clear_AddValuesAndSpecialValueThenClear_EverythingCleared()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.SetSpecialValue();
            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));

            Assert.Equal(4, collection.Count);
            Assert.True(collection.IsSpecialValueSet, "Special value not set.");

            collection.Clear();

            Assert.Equal(0, collection.Count);
            Assert.False(collection.IsSpecialValueSet, "Special value was removed by Clear().");
        }

        [Fact]
        public void Contains_CallWithNullValue_Throw()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            Assert.Throws<ArgumentNullException>(() => { collection.Contains(null); });
        }

        [Fact]
        public void Contains_AddValuesThenCallContains_ReturnsTrueForExistingItemsFalseOtherwise()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));

            Assert.True(collection.Contains(new Uri("http://www.example.org/2/")), "Expected true for existing item.");
            Assert.False(collection.Contains(new Uri("http://www.example.org/4/")),
                "Expected false for non-existing item.");
        }

        [Fact]
        public void Contains_UseSpecialValueWhenEmpty_False()
        {
            HttpRequestHeaders headers = new HttpRequestHeaders();

            Assert.False(headers.TransferEncoding.Contains(specialChunked));
        }

        [Fact]
        public void Contains_UseSpecialValueWithProperty_True()
        {
            HttpRequestHeaders headers = new HttpRequestHeaders();

            headers.TransferEncodingChunked = true;
            Assert.True(headers.TransferEncoding.Contains(specialChunked));

            headers.TransferEncodingChunked = false;
            Assert.False(headers.TransferEncoding.Contains(specialChunked));
        }

        [Fact]
        public void Contains_UseSpecialValueWhenSpecilValueIsPresent_True()
        {
            HttpRequestHeaders headers = new HttpRequestHeaders();

            headers.TransferEncoding.Add(specialChunked);
            Assert.True(headers.TransferEncoding.Contains(specialChunked));

            headers.TransferEncoding.Remove(specialChunked);
            Assert.False(headers.TransferEncoding.Contains(specialChunked));
        }

        [Fact]
        public void CopyTo_CallWithStartIndexPlusElementCountGreaterArrayLength_Throw()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));

            Uri[] array = new Uri[2];
            
            // startIndex + Count = 1 + 2 > array.Length
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => { collection.CopyTo(array, 1); });
        }

        [Fact]
        public void CopyTo_EmptyToEmpty_Success()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            Uri[] array = new Uri[0];
            collection.CopyTo(array, 0);
        }

        [Fact]
        public void CopyTo_NoValues_DoesNotChangeArray()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            Uri[] array = new Uri[4];
            collection.CopyTo(array, 0);

            for (int i = 0; i < array.Length; i++)
            {
                Assert.Null(array[i]);
            }
        }

        [Fact]
        public void CopyTo_AddSingleValue_ContainsSingleValue()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.Add(new Uri("http://www.example.org/"));

            Uri[] array = new Uri[1];
            collection.CopyTo(array, 0);
            Assert.Equal(new Uri("http://www.example.org/"), array[0]);

            // Now only set the special value: nothing should be added to the array.
            headers.Clear();
            headers.Add(knownUriHeader, specialValue.ToString());
            array[0] = null;
            collection.CopyTo(array, 0);
            Assert.Equal(specialValue, array[0]);
        }

        [Fact]
        public void CopyTo_AddMultipleValues_ContainsAllValuesInTheRightOrder()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));

            Uri[] array = new Uri[5];
            collection.CopyTo(array, 1);

            Assert.Null(array[0]);
            Assert.Equal(new Uri("http://www.example.org/1/"), array[1]);
            Assert.Equal(new Uri("http://www.example.org/2/"), array[2]);
            Assert.Equal(new Uri("http://www.example.org/3/"), array[3]);
            Assert.Null(array[4]);
        }

        [Fact]
        public void CopyTo_AddValuesAndSpecialValue_AllValuesCopied()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.SetSpecialValue();
            collection.Add(new Uri("http://www.example.org/3/"));

            Uri[] array = new Uri[5];
            collection.CopyTo(array, 1);

            Assert.Null(array[0]);
            Assert.Equal(new Uri("http://www.example.org/1/"), array[1]);
            Assert.Equal(new Uri("http://www.example.org/2/"), array[2]);
            Assert.Equal(specialValue, array[3]);
            Assert.Equal(new Uri("http://www.example.org/3/"), array[4]);

            Assert.True(collection.IsSpecialValueSet, "Special value not set.");
        }

        [Fact]
        public void CopyTo_OnlySpecialValue_Copied()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.SetSpecialValue();
            headers.Add(knownUriHeader, specialValue.ToString());
            headers.Add(knownUriHeader, specialValue.ToString());
            headers.Add(knownUriHeader, specialValue.ToString());

            Uri[] array = new Uri[4];
            array[0] = null;
            collection.CopyTo(array, 0);

            Assert.Equal(specialValue, array[0]);
            Assert.Equal(specialValue, array[1]);
            Assert.Equal(specialValue, array[2]);
            Assert.Equal(specialValue, array[3]);

            Assert.True(collection.IsSpecialValueSet, "Special value not set.");
        }

        [Fact]
        public void CopyTo_OnlySpecialValueEmptyDestination_Copied()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.SetSpecialValue();
            headers.Add(knownUriHeader, specialValue.ToString());

            Uri[] array = new Uri[2];
            collection.CopyTo(array, 0);

            Assert.Equal(specialValue, array[0]);
            Assert.Equal(specialValue, array[1]);

            Assert.True(collection.IsSpecialValueSet, "Special value not set.");
        }

        [Fact]
        public void CopyTo_ArrayTooSmall_Throw()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<string> collection = new HttpHeaderValueCollection<string>(knownStringHeader, headers,
                "special");

            string[] array = new string[1];
            array[0] = null;

            collection.CopyTo(array, 0); // no exception
            Assert.Null(array[0]);

            Assert.Throws<ArgumentNullException>(() => { collection.CopyTo(null, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { collection.CopyTo(array, -1); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { collection.CopyTo(array, 2); });

            headers.Add(knownStringHeader, "special");
            array = new string[0];
            AssertExtensions.Throws<ArgumentException>(null, () => { collection.CopyTo(array, 0); });

            headers.Add(knownStringHeader, "special");
            headers.Add(knownStringHeader, "special");
            array = new string[1];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => { collection.CopyTo(array, 0); });

            headers.Add(knownStringHeader, "value1");
            array = new string[0];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => { collection.CopyTo(array, 0); });

            headers.Add(knownStringHeader, "value2");
            array = new string[1];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => { collection.CopyTo(array, 0); });

            array = new string[2];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => { collection.CopyTo(array, 1); });
        }

        [Fact]
        public void Remove_CallWithNullValue_Throw()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            Assert.Throws<ArgumentNullException>(() => { collection.Remove(null); });
        }

        [Fact]
        public void Remove_AddValuesThenCallRemove_ReturnsTrueWhenRemovingExistingValuesFalseOtherwise()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));

            Assert.True(collection.Remove(new Uri("http://www.example.org/2/")), "Expected true for existing item.");
            Assert.False(collection.Remove(new Uri("http://www.example.org/4/")),
                "Expected false for non-existing item.");
        }

        [Fact]
        public void Remove_UseSpecialValue_FalseWhenEmpty()
        {
            HttpRequestHeaders headers = new HttpRequestHeaders();
            Assert.Null(headers.TransferEncodingChunked);
            Assert.Equal(0, headers.TransferEncoding.Count);

            Assert.False(headers.TransferEncoding.Remove(specialChunked));

            Assert.Null(headers.TransferEncodingChunked);
            Assert.Equal(0, headers.TransferEncoding.Count);
        }

        [Fact]
        public void Remove_UseSpecialValueWhenSetWithProperty_True()
        {
            HttpRequestHeaders headers = new HttpRequestHeaders();
            headers.TransferEncodingChunked = true;
            Assert.True((bool)headers.TransferEncodingChunked);
            Assert.Equal(1, headers.TransferEncoding.Count);
            Assert.True(headers.TransferEncoding.Contains(specialChunked));

            Assert.True(headers.TransferEncoding.Remove(specialChunked));

            Assert.False((bool)headers.TransferEncodingChunked);
            Assert.Equal(0, headers.TransferEncoding.Count);
            Assert.False(headers.TransferEncoding.Contains(specialChunked));
        }

        [Fact]
        public void Remove_UseSpecialValueWhenAdded_True()
        {
            HttpRequestHeaders headers = new HttpRequestHeaders();
            headers.TransferEncoding.Add(specialChunked);
            Assert.True((bool)headers.TransferEncodingChunked);
            Assert.Equal(1, headers.TransferEncoding.Count);
            Assert.True(headers.TransferEncoding.Contains(specialChunked));

            Assert.True(headers.TransferEncoding.Remove(specialChunked));

            Assert.Null(headers.TransferEncodingChunked);
            Assert.Equal(0, headers.TransferEncoding.Count);
            Assert.False(headers.TransferEncoding.Contains(specialChunked));
        }

        [Fact]
        public void GetEnumerator_AddSingleValueAndGetEnumerator_AllValuesReturned()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.Add(new Uri("http://www.example.org/"));

            bool started = false;
            foreach (var item in collection)
            {
                Assert.False(started, "We have more than one element returned by the enumerator.");
                Assert.Equal(new Uri("http://www.example.org/"), item);
            }
        }

        [Fact]
        public void GetEnumerator_AddValuesAndGetEnumerator_AllValuesReturned()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));

            int i = 1;
            foreach (var item in collection)
            {
                Assert.Equal(new Uri("http://www.example.org/" + i + "/"), item);
                i++;
            }
        }

        [Fact]
        public void GetEnumerator_NoValues_EmptyEnumerator()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            IEnumerator<Uri> enumerator = collection.GetEnumerator();

            Assert.False(enumerator.MoveNext(), "No items expected in enumerator.");
        }

        [Fact]
        public void GetEnumerator_AddValuesAndGetEnumeratorFromInterface_AllValuesReturned()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));

            System.Collections.IEnumerable enumerable = collection;

            int i = 1;
            foreach (var item in enumerable)
            {
                Assert.Equal(new Uri("http://www.example.org/" + i + "/"), item);
                i++;
            }
        }

        [Fact]
        public void GetEnumerator_AddValuesAndSpecialValueAndGetEnumeratorFromInterface_AllValuesReturned()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.Add(new Uri("http://www.example.org/3/"));
            collection.SetSpecialValue();

            System.Collections.IEnumerable enumerable = collection;

            // The "special value" should be ignored and not part of the resulting collection.
            int i = 1;
            bool specialFound = false;
            foreach (var item in enumerable)
            {
                if (item.Equals(specialValue))
                {
                    specialFound = true;
                }
                else
                {
                    Assert.Equal(new Uri("http://www.example.org/" + i + "/"), item);
                    i++;
                }
            }

            Assert.True(specialFound);

            Assert.True(collection.IsSpecialValueSet, "Special value not set.");
        }

        [Fact]
        public void GetEnumerator_AddValuesAndSpecialValue_AllValuesReturned()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.SetSpecialValue();
            collection.Add(new Uri("http://www.example.org/3/"));

            System.Collections.IEnumerable enumerable = collection;

            // The special value we added above, must be part of the collection returned by GetEnumerator().
            int i = 1;
            bool specialFound = false;
            foreach (var item in enumerable)
            {
                if (item.Equals(specialValue))
                {
                    specialFound = true;
                }
                else
                {
                    Assert.Equal(new Uri("http://www.example.org/" + i + "/"), item);
                    i++;
                }
            }

            Assert.True(specialFound);
            Assert.True(collection.IsSpecialValueSet, "Special value not set.");
        }

        [Fact]
        public void IsSpecialValueSet_NoSpecialValueUsed_ReturnsFalse()
        {
            // Create a new collection _without_ specifying a special value.
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                null, null);

            Assert.False(collection.IsSpecialValueSet,
                "Special value is set even though collection doesn't define a special value.");
        }

        [Fact]
        public void RemoveSpecialValue_AddRemoveSpecialValue_SpecialValueGetsRemoved()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.SetSpecialValue();
            Assert.True(collection.IsSpecialValueSet, "Special value not set.");
            Assert.Equal(1, headers.GetValues(knownUriHeader).Count());

            collection.RemoveSpecialValue();
            Assert.False(collection.IsSpecialValueSet, "Special value is set.");

            // Since the only header value was the "special value", removing it will remove the whole header
            // from the collection.
            Assert.False(headers.Contains(knownUriHeader));
        }

        [Fact]
        public void RemoveSpecialValue_AddValueAndSpecialValueThenRemoveSpecialValue_SpecialValueGetsRemoved()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.Add(new Uri("http://www.example.org/"));
            collection.SetSpecialValue();
            Assert.True(collection.IsSpecialValueSet, "Special value not set.");
            Assert.Equal(2, headers.GetValues(knownUriHeader).Count());

            collection.RemoveSpecialValue();
            Assert.False(collection.IsSpecialValueSet, "Special value is set.");
            Assert.Equal(1, headers.GetValues(knownUriHeader).Count());
        }

        [Fact]
        public void RemoveSpecialValue_AddTwoValuesAndSpecialValueThenRemoveSpecialValue_SpecialValueGetsRemoved()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue);

            collection.Add(new Uri("http://www.example.org/1/"));
            collection.Add(new Uri("http://www.example.org/2/"));
            collection.SetSpecialValue();
            Assert.True(collection.IsSpecialValueSet, "Special value not set.");
            Assert.Equal(3, headers.GetValues(knownUriHeader).Count());

            // The difference between this test and the previous one is that HttpHeaders in this case will use
            // a List<T> to store the two remaining values, whereas in the previous case it will just store
            // the remaining value (no list).
            collection.RemoveSpecialValue();
            Assert.False(collection.IsSpecialValueSet, "Special value is set.");
            Assert.Equal(2, headers.GetValues(knownUriHeader).Count());
        }

        [Fact]
        public void Ctor_ProvideValidator_ValidatorIsUsedWhenAddingValues()
        {
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                MockValidator);

            // Adding an arbitrary Uri should not throw.
            collection.Add(new Uri("http://some/"));

            // When we add 'invalidValue' our MockValidator will throw.
            Assert.Throws<MockException>(() => { collection.Add(invalidValue); });
        }

        [Fact]
        public void Ctor_ProvideValidator_ValidatorIsUsedWhenRemovingValues()
        {
            // Use different ctor overload than in previous test to make sure all ctor overloads work correctly.
            MockHeaders headers = new MockHeaders();
            HttpHeaderValueCollection<Uri> collection = new HttpHeaderValueCollection<Uri>(knownUriHeader, headers,
                specialValue, MockValidator);

            // When we remove 'invalidValue' our MockValidator will throw.
            Assert.Throws<MockException>(() => { collection.Remove(invalidValue); });
        }

        [Fact]
        public void ToString_SpecialValues_Success()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            request.Headers.TransferEncodingChunked = true;
            string result = request.Headers.TransferEncoding.ToString();
            Assert.Equal("chunked", result);

            request.Headers.ExpectContinue = true;
            result = request.Headers.Expect.ToString();
            Assert.Equal("100-continue", result);

            request.Headers.ConnectionClose = true;
            result = request.Headers.Connection.ToString();
            Assert.Equal("close", result);
        }

        [Fact]
        public void ToString_SpecialValueAndExtra_Success()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            request.Headers.Add(HttpKnownHeaderNames.TransferEncoding, "bla1");
            request.Headers.TransferEncodingChunked = true;
            request.Headers.Add(HttpKnownHeaderNames.TransferEncoding, "bla2");
            string result = request.Headers.TransferEncoding.ToString();
            Assert.Equal("bla1, chunked, bla2", result);
        }

        [Fact]
        public void ToString_SingleValue_Success()
        {
            using (var response = new HttpResponseMessage())
            {
                string input = "Basic";
                response.Headers.Add(HttpKnownHeaderNames.WWWAuthenticate, input);
                string result = response.Headers.WwwAuthenticate.ToString();
                Assert.Equal(input, result);
            }
        }

        [Fact]
        public void ToString_MultipleValue_Success()
        {
            using (var response = new HttpResponseMessage())
            {
                string input = "Basic, NTLM, Negotiate, Custom";
                response.Headers.Add(HttpKnownHeaderNames.WWWAuthenticate, input);
                string result = response.Headers.WwwAuthenticate.ToString();
                Assert.Equal(input, result);
            }
        }

        [Fact]
        public void ToString_EmptyValue_Success()
        {
            using (var response = new HttpResponseMessage())
            {
                string result = response.Headers.WwwAuthenticate.ToString();
                Assert.Equal(string.Empty, result);
            }
        }

        #region Helper methods

        private static void MockValidator(HttpHeaderValueCollection<Uri> collection, Uri value)
        {
            if (value == invalidValue)
            {
                throw new MockException();
            }
        }

        public class MockException : Exception
        {
            public MockException() { }
            public MockException(string message) : base(message) { }
            public MockException(string message, Exception inner) : base(message, inner) { }
        }

        private class MockHeaders : HttpHeaders
        {
            public MockHeaders()
            {
            }
        }

        private class MockHeaderParser : HttpHeaderParser
        {
            private static MockComparer comparer = new MockComparer();
            private Type valueType;

            public override IEqualityComparer Comparer
            {
                get { return comparer; }
            }

            public MockHeaderParser(Type valueType)
                : base(true)
            {
                Assert.Contains(valueType, new[] { typeof(string), typeof(Uri) });
                this.valueType = valueType;
            }

            public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
            {
                parsedValue = null;
                if (value == null)
                {
                    return true;
                }

                index = value.Length;

                // Just return the raw string (as string or Uri depending on the value type)
                parsedValue = (valueType == typeof(Uri) ? (object)new Uri(value) : value);
                return true;
            }
        }
        private class MockComparer : IEqualityComparer
        {
            public int EqualsCount { get; private set; }
            public int GetHashCodeCount { get; private set; }

            #region IEqualityComparer Members

            public new bool Equals(object x, object y)
            {
                EqualsCount++;
                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                GetHashCodeCount++;
                return obj.GetHashCode();
            }
            #endregion
        }
        #endregion
    }
}
