// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    public class DownloadDataCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal DownloadDataCompletedEventArgs(byte[] result, System.Exception exception, bool cancelled, object userToken) : base(exception, cancelled, userToken) { }
        public byte[] Result { get { throw null; } }
    }
    public delegate void DownloadDataCompletedEventHandler(object sender, System.Net.DownloadDataCompletedEventArgs e);
    public class DownloadProgressChangedEventArgs : System.ComponentModel.ProgressChangedEventArgs
    {
        internal DownloadProgressChangedEventArgs(int progressPercentage, object userToken, long bytesReceived, long totalBytesToReceive) : base(progressPercentage, userToken) { }
        public long BytesReceived { get { throw null; } }
        public long TotalBytesToReceive { get { throw null; } }
    }
    public delegate void DownloadProgressChangedEventHandler(object sender, System.Net.DownloadProgressChangedEventArgs e);
    public class DownloadStringCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal DownloadStringCompletedEventArgs(string result, System.Exception exception, bool cancelled, object userToken) : base(exception, cancelled, userToken) { }
        public string Result { get { throw null; } }
    }
    public delegate void DownloadStringCompletedEventHandler(object sender, System.Net.DownloadStringCompletedEventArgs e);
    public delegate void OpenReadCompletedEventHandler(object sender, System.Net.OpenReadCompletedEventArgs e);
    public class OpenReadCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal OpenReadCompletedEventArgs(System.IO.Stream result, System.Exception exception, bool cancelled, object userToken) : base(exception, cancelled, userToken) { }
        public System.IO.Stream Result { get { throw null; } }
    }
    public delegate void OpenWriteCompletedEventHandler(object sender, System.Net.OpenWriteCompletedEventArgs e);
    public class OpenWriteCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal OpenWriteCompletedEventArgs(System.IO.Stream result, System.Exception exception, bool cancelled, object userToken) : base(exception, cancelled, userToken) { }
        public System.IO.Stream Result { get { throw null; } }
    }
    public class UploadDataCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal UploadDataCompletedEventArgs(byte[] result, System.Exception exception, bool cancelled, object userToken) : base(exception, cancelled, userToken) { }
        public byte[] Result { get { throw null; } }
    }
    public delegate void UploadDataCompletedEventHandler(object sender, System.Net.UploadDataCompletedEventArgs e);
    public class UploadFileCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal UploadFileCompletedEventArgs(byte[] result, System.Exception exception, bool cancelled, object userToken) : base(exception, cancelled, userToken) { }
        public byte[] Result { get { throw null; } }
    }
    public delegate void UploadFileCompletedEventHandler(object sender, System.Net.UploadFileCompletedEventArgs e);
    public class UploadProgressChangedEventArgs : System.ComponentModel.ProgressChangedEventArgs
    {
        internal UploadProgressChangedEventArgs(int progressPercentage, object userToken, long bytesSent, long totalBytesToSend, long bytesReceived, long totalBytesToReceive) : base(progressPercentage, userToken) { }
        public long BytesReceived { get { throw null; } }
        public long TotalBytesToReceive { get { throw null; } }
        public long BytesSent { get { throw null; } }
        public long TotalBytesToSend { get { throw null; } }
    }
    public delegate void UploadProgressChangedEventHandler(object sender, System.Net.UploadProgressChangedEventArgs e);
    public class UploadStringCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal UploadStringCompletedEventArgs(string result, System.Exception exception, bool cancelled, object userToken) : base(exception, cancelled, userToken) { }
        public string Result { get { throw null; } }
    }
    public delegate void UploadStringCompletedEventHandler(object sender, System.Net.UploadStringCompletedEventArgs e);
    public class UploadValuesCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs
    {
        internal UploadValuesCompletedEventArgs(byte[] result, System.Exception exception, bool cancelled, object userToken) : base(exception, cancelled, userToken) { }
        public byte[] Result { get { throw null; } }
    }
    public delegate void UploadValuesCompletedEventHandler(object sender, System.Net.UploadValuesCompletedEventArgs e);
    public class WebClient : System.ComponentModel.Component
    {
        public WebClient() { }
        [System.Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool AllowReadStreamBuffering { get { throw null; } set { } }
        [System.Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool AllowWriteStreamBuffering { get { throw null; } set { } }
        public string BaseAddress { get { throw null; } set { } }
        public System.Net.Cache.RequestCachePolicy CachePolicy { get { throw null; } set { } }
        public System.Net.ICredentials Credentials { get { throw null; } set { } }
        public System.Text.Encoding Encoding { get { throw null; } set { } }
        public System.Net.WebHeaderCollection Headers { get { throw null; } set { } }
        public bool IsBusy { get { throw null; } }
        public System.Net.IWebProxy Proxy { get { throw null; } set { } }
        public System.Collections.Specialized.NameValueCollection QueryString { get { throw null; } set { } }
        public System.Net.WebHeaderCollection ResponseHeaders { get { throw null; } }
        public bool UseDefaultCredentials { get { throw null; } set { } }
        public event System.Net.DownloadDataCompletedEventHandler DownloadDataCompleted { add { } remove { } }
        public event System.ComponentModel.AsyncCompletedEventHandler DownloadFileCompleted { add { } remove { } }
        public event System.Net.DownloadProgressChangedEventHandler DownloadProgressChanged { add { } remove { } }
        public event System.Net.DownloadStringCompletedEventHandler DownloadStringCompleted { add { } remove { } }
        public event System.Net.OpenReadCompletedEventHandler OpenReadCompleted { add { } remove { } }
        public event System.Net.OpenWriteCompletedEventHandler OpenWriteCompleted { add { } remove { } }
        public event System.Net.UploadDataCompletedEventHandler UploadDataCompleted { add { } remove { } }
        public event System.Net.UploadFileCompletedEventHandler UploadFileCompleted { add { } remove { } }
        public event System.Net.UploadProgressChangedEventHandler UploadProgressChanged { add { } remove { } }
        public event System.Net.UploadStringCompletedEventHandler UploadStringCompleted { add { } remove { } }
        public event System.Net.UploadValuesCompletedEventHandler UploadValuesCompleted { add { } remove { } }
        [System.Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public event System.Net.WriteStreamClosedEventHandler WriteStreamClosed { add { } remove { } }
        public void CancelAsync() { }
        public byte[] DownloadData(string address) { throw null; }
        public byte[] DownloadData(System.Uri address) { throw null; }
        public void DownloadDataAsync(System.Uri address) { }
        public void DownloadDataAsync(System.Uri address, object userToken) { }
        public System.Threading.Tasks.Task<byte[]> DownloadDataTaskAsync(string address) { throw null; }
        public System.Threading.Tasks.Task<byte[]> DownloadDataTaskAsync(System.Uri address) { throw null; }
        public void DownloadFile(string address, string fileName) { }
        public void DownloadFile(System.Uri address, string fileName) { }
        public void DownloadFileAsync(System.Uri address, string fileName) { }
        public void DownloadFileAsync(System.Uri address, string fileName, object userToken) { }
        public System.Threading.Tasks.Task DownloadFileTaskAsync(string address, string fileName) { throw null; }
        public System.Threading.Tasks.Task DownloadFileTaskAsync(System.Uri address, string fileName) { throw null; }
        public string DownloadString(string address) { throw null; }
        public string DownloadString(System.Uri address) { throw null; }
        public void DownloadStringAsync(System.Uri address) { }
        public void DownloadStringAsync(System.Uri address, object userToken) { }
        public System.Threading.Tasks.Task<string> DownloadStringTaskAsync(string address) { throw null; }
        public System.Threading.Tasks.Task<string> DownloadStringTaskAsync(System.Uri address) { throw null; }
        protected virtual System.Net.WebRequest GetWebRequest(System.Uri address) { throw null; }
        protected virtual System.Net.WebResponse GetWebResponse(System.Net.WebRequest request) { throw null; }
        protected virtual System.Net.WebResponse GetWebResponse(System.Net.WebRequest request, System.IAsyncResult result) { throw null; }
        protected virtual void OnDownloadDataCompleted(System.Net.DownloadDataCompletedEventArgs e) { }
        protected virtual void OnDownloadFileCompleted(System.ComponentModel.AsyncCompletedEventArgs e) { }
        protected virtual void OnDownloadProgressChanged(System.Net.DownloadProgressChangedEventArgs e) { }
        protected virtual void OnDownloadStringCompleted(System.Net.DownloadStringCompletedEventArgs e) { }
        protected virtual void OnOpenReadCompleted(System.Net.OpenReadCompletedEventArgs e) { }
        protected virtual void OnOpenWriteCompleted(System.Net.OpenWriteCompletedEventArgs e) { }
        protected virtual void OnUploadDataCompleted(System.Net.UploadDataCompletedEventArgs e) { }
        protected virtual void OnUploadFileCompleted(System.Net.UploadFileCompletedEventArgs e) { }
        protected virtual void OnUploadProgressChanged(System.Net.UploadProgressChangedEventArgs e) { }
        protected virtual void OnUploadStringCompleted(System.Net.UploadStringCompletedEventArgs e) { }
        protected virtual void OnUploadValuesCompleted(System.Net.UploadValuesCompletedEventArgs e) { }
        [System.Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected virtual void OnWriteStreamClosed(System.Net.WriteStreamClosedEventArgs e) { }
        public System.IO.Stream OpenRead(string address) { throw null; }
        public System.IO.Stream OpenRead(System.Uri address) { throw null; }
        public void OpenReadAsync(System.Uri address) { }
        public void OpenReadAsync(System.Uri address, object userToken) { }
        public System.Threading.Tasks.Task<System.IO.Stream> OpenReadTaskAsync(string address) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> OpenReadTaskAsync(System.Uri address) { throw null; }
        public System.IO.Stream OpenWrite(string address) { throw null; }
        public System.IO.Stream OpenWrite(System.Uri address) { throw null; }
        public System.IO.Stream OpenWrite(string address, string method) { throw null; }
        public System.IO.Stream OpenWrite(System.Uri address, string method) { throw null; }
        public void OpenWriteAsync(System.Uri address) { }
        public void OpenWriteAsync(System.Uri address, string method) { }
        public void OpenWriteAsync(System.Uri address, string method, object userToken) { }
        public System.Threading.Tasks.Task<System.IO.Stream> OpenWriteTaskAsync(string address) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> OpenWriteTaskAsync(System.Uri address) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> OpenWriteTaskAsync(string address, string method) { throw null; }
        public System.Threading.Tasks.Task<System.IO.Stream> OpenWriteTaskAsync(System.Uri address, string method) { throw null; }
        public byte[] UploadData(string address, byte[] data) { throw null; }
        public byte[] UploadData(System.Uri address, byte[] data) { throw null; }
        public byte[] UploadData(string address, string method, byte[] data) { throw null; }
        public byte[] UploadData(System.Uri address, string method, byte[] data) { throw null; }
        public void UploadDataAsync(System.Uri address, byte[] data) { }
        public void UploadDataAsync(System.Uri address, string method, byte[] data) { }
        public void UploadDataAsync(System.Uri address, string method, byte[] data, object userToken) { }
        public System.Threading.Tasks.Task<byte[]> UploadDataTaskAsync(string address, byte[] data) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadDataTaskAsync(System.Uri address, byte[] data) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadDataTaskAsync(string address, string method, byte[] data) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadDataTaskAsync(System.Uri address, string method, byte[] data) { throw null; }
        public byte[] UploadFile(string address, string fileName) { throw null; }
        public byte[] UploadFile(System.Uri address, string fileName) { throw null; }
        public byte[] UploadFile(string address, string method, string fileName) { throw null; }
        public byte[] UploadFile(System.Uri address, string method, string fileName) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadFileTaskAsync(string address, string fileName) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadFileTaskAsync(System.Uri address, string fileName) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadFileTaskAsync(string address, string method, string fileName) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadFileTaskAsync(System.Uri address, string method, string fileName) { throw null; }
        public void UploadFileAsync(System.Uri address, string fileName) { }
        public void UploadFileAsync(System.Uri address, string method, string fileName) { }
        public void UploadFileAsync(System.Uri address, string method, string fileName, object userToken) { }
        public string UploadString(string address, string data) { throw null; }
        public string UploadString(System.Uri address, string data) { throw null; }
        public string UploadString(string address, string method, string data) { throw null; }
        public string UploadString(System.Uri address, string method, string data) { throw null; }
        public void UploadStringAsync(System.Uri address, string data) { }
        public void UploadStringAsync(System.Uri address, string method, string data) { }
        public void UploadStringAsync(System.Uri address, string method, string data, object userToken) { }
        public System.Threading.Tasks.Task<string> UploadStringTaskAsync(string address, string data) { throw null; }
        public System.Threading.Tasks.Task<string> UploadStringTaskAsync(System.Uri address, string data) { throw null; }
        public System.Threading.Tasks.Task<string> UploadStringTaskAsync(string address, string method, string data) { throw null; }
        public System.Threading.Tasks.Task<string> UploadStringTaskAsync(System.Uri address, string method, string data) { throw null; }
        public byte[] UploadValues(string address, System.Collections.Specialized.NameValueCollection data) { throw null; }
        public byte[] UploadValues(System.Uri address, System.Collections.Specialized.NameValueCollection data) { throw null; }
        public byte[] UploadValues(string address, string method, System.Collections.Specialized.NameValueCollection data) { throw null; }
        public byte[] UploadValues(System.Uri address, string method, System.Collections.Specialized.NameValueCollection data) { throw null; }
        public void UploadValuesAsync(System.Uri address, System.Collections.Specialized.NameValueCollection data) { }
        public void UploadValuesAsync(System.Uri address, string method, System.Collections.Specialized.NameValueCollection data) { }
        public void UploadValuesAsync(System.Uri address, string method, System.Collections.Specialized.NameValueCollection data, object userToken) { }
        public System.Threading.Tasks.Task<byte[]> UploadValuesTaskAsync(string address, System.Collections.Specialized.NameValueCollection data) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadValuesTaskAsync(string address, string method, System.Collections.Specialized.NameValueCollection data) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadValuesTaskAsync(System.Uri address, System.Collections.Specialized.NameValueCollection data) { throw null; }
        public System.Threading.Tasks.Task<byte[]> UploadValuesTaskAsync(System.Uri address, string method, System.Collections.Specialized.NameValueCollection data) { throw null; }
    }
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class WriteStreamClosedEventArgs : System.EventArgs
    {
        [System.Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public WriteStreamClosedEventArgs() { }
        [System.Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public System.Exception Error { get { throw null; } }
    }
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public delegate void WriteStreamClosedEventHandler(object sender, System.Net.WriteStreamClosedEventArgs e);
}
