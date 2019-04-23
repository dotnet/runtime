// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    public enum HttpRequestHeader
    {
        CacheControl = 0,
        Connection = 1,
        Date = 2,
        KeepAlive = 3,
        Pragma = 4,
        Trailer = 5,
        TransferEncoding = 6,
        Upgrade = 7,
        Via = 8,
        Warning = 9,
        Allow = 10,
        ContentLength = 11,
        ContentType = 12,
        ContentEncoding = 13,
        ContentLanguage = 14,
        ContentLocation = 15,
        ContentMd5 = 16,
        ContentRange = 17,
        Expires = 18,
        LastModified = 19,
        Accept = 20,
        AcceptCharset = 21,
        AcceptEncoding = 22,
        AcceptLanguage = 23,
        Authorization = 24,
        Cookie = 25,
        Expect = 26,
        From = 27,
        Host = 28,
        IfMatch = 29,
        IfModifiedSince = 30,
        IfNoneMatch = 31,
        IfRange = 32,
        IfUnmodifiedSince = 33,
        MaxForwards = 34,
        ProxyAuthorization = 35,
        Referer = 36,
        Range = 37,
        Te = 38,
        Translate = 39,
        UserAgent = 40,
    }
    public enum HttpResponseHeader
    {
        CacheControl = 0,
        Connection = 1,
        Date = 2,
        KeepAlive = 3,
        Pragma = 4,
        Trailer = 5,
        TransferEncoding = 6,
        Upgrade = 7,
        Via = 8,
        Warning = 9,
        Allow = 10,
        ContentLength = 11,
        ContentType = 12,
        ContentEncoding = 13,
        ContentLanguage = 14,
        ContentLocation = 15,
        ContentMd5 = 16,
        ContentRange = 17,
        Expires = 18,
        LastModified = 19,
        AcceptRanges = 20,
        Age = 21,
        ETag = 22,
        Location = 23,
        ProxyAuthenticate = 24,
        RetryAfter = 25,
        Server = 26,
        SetCookie = 27,
        Vary = 28,
        WwwAuthenticate = 29,
    }
    public partial class WebHeaderCollection : System.Collections.Specialized.NameValueCollection, System.Collections.IEnumerable, System.Runtime.Serialization.ISerializable
    {
        public WebHeaderCollection() { }
        protected WebHeaderCollection(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override string[] AllKeys { get { throw null; } }
        public override int Count { get { throw null; } }
        public string this[System.Net.HttpRequestHeader header] { get { throw null; } set { } }
        public string this[System.Net.HttpResponseHeader header] { get { throw null; } set { } }
        public new string this[string name] { get { throw null; } set { } }
        public override System.Collections.Specialized.NameObjectCollectionBase.KeysCollection Keys { get { throw null; } }
        public void Add(System.Net.HttpRequestHeader header, string value) { }
        public void Add(System.Net.HttpResponseHeader header, string value) { }
        public void Add(string header) { }
        public override void Add(string name, string value) { }
        protected void AddWithoutValidate(string headerName, string headerValue) { }
        public override void Clear() { }
        public override string Get(int index) { throw null; }
        public override string Get(string name) { throw null; }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override string GetKey(int index) { throw null; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override string[] GetValues(int index) { throw null; }
        public override string[] GetValues(string header) { throw null; }
        public static bool IsRestricted(string headerName) { throw null; }
        public static bool IsRestricted(string headerName, bool response) { throw null; }
        public override void OnDeserialization(object sender) { }
        public void Remove(System.Net.HttpRequestHeader header) { }
        public void Remove(System.Net.HttpResponseHeader header) { }
        public override void Remove(string name) { }
        public void Set(System.Net.HttpRequestHeader header, string value) { }
        public void Set(System.Net.HttpResponseHeader header, string value) { }
        public override void Set(string name, string value) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public byte[] ToByteArray() { throw null; }
        public override string ToString() { throw null; }
    }
}
