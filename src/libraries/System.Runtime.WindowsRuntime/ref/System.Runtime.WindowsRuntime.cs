// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    [global::System.CLSCompliantAttribute(false)]
    [global::System.Security.SecurityCriticalAttribute]
    public static partial class WindowsRuntimeSystemExtensions
    {
        public static global::Windows.Foundation.IAsyncAction AsAsyncAction(this global::System.Threading.Tasks.Task source) { return default(global::Windows.Foundation.IAsyncAction); }
        public static global::Windows.Foundation.IAsyncOperation<TResult> AsAsyncOperation<TResult>(this global::System.Threading.Tasks.Task<TResult> source) { return default(global::Windows.Foundation.IAsyncOperation<TResult>); }
        public static global::System.Threading.Tasks.Task AsTask(this global::Windows.Foundation.IAsyncAction source) { return default(global::System.Threading.Tasks.Task); }
        public static global::System.Threading.Tasks.Task AsTask(this global::Windows.Foundation.IAsyncAction source, global::System.Threading.CancellationToken cancellationToken) { return default(global::System.Threading.Tasks.Task); }
        public static global::System.Threading.Tasks.Task AsTask<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source) { return default(global::System.Threading.Tasks.Task); }
        public static global::System.Threading.Tasks.Task AsTask<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source, global::System.IProgress<TProgress> progress) { return default(global::System.Threading.Tasks.Task); }
        public static global::System.Threading.Tasks.Task AsTask<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source, global::System.Threading.CancellationToken cancellationToken) { return default(global::System.Threading.Tasks.Task); }
        public static global::System.Threading.Tasks.Task AsTask<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source, global::System.Threading.CancellationToken cancellationToken, global::System.IProgress<TProgress> progress) { return default(global::System.Threading.Tasks.Task); }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult>(this global::Windows.Foundation.IAsyncOperation<TResult> source) { return default(global::System.Threading.Tasks.Task<TResult>); }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult>(this global::Windows.Foundation.IAsyncOperation<TResult> source, global::System.Threading.CancellationToken cancellationToken) { return default(global::System.Threading.Tasks.Task<TResult>); }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source) { return default(global::System.Threading.Tasks.Task<TResult>); }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source, global::System.IProgress<TProgress> progress) { return default(global::System.Threading.Tasks.Task<TResult>); }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source, global::System.Threading.CancellationToken cancellationToken) { return default(global::System.Threading.Tasks.Task<TResult>); }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source, global::System.Threading.CancellationToken cancellationToken, global::System.IProgress<TProgress> progress) { return default(global::System.Threading.Tasks.Task<TResult>); }
        [global::System.ComponentModel.EditorBrowsableAttribute((global::System.ComponentModel.EditorBrowsableState)(1))]
        public static global::System.Runtime.CompilerServices.TaskAwaiter GetAwaiter(this global::Windows.Foundation.IAsyncAction source) { return default(global::System.Runtime.CompilerServices.TaskAwaiter); }
        [global::System.ComponentModel.EditorBrowsableAttribute((global::System.ComponentModel.EditorBrowsableState)(1))]
        public static global::System.Runtime.CompilerServices.TaskAwaiter GetAwaiter<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source) { return default(global::System.Runtime.CompilerServices.TaskAwaiter); }
        [global::System.ComponentModel.EditorBrowsableAttribute((global::System.ComponentModel.EditorBrowsableState)(1))]
        public static global::System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter<TResult>(this global::Windows.Foundation.IAsyncOperation<TResult> source) { return default(global::System.Runtime.CompilerServices.TaskAwaiter<TResult>); }
        [global::System.ComponentModel.EditorBrowsableAttribute((global::System.ComponentModel.EditorBrowsableState)(1))]
        public static global::System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source) { return default(global::System.Runtime.CompilerServices.TaskAwaiter<TResult>); }
    }
}
namespace System.IO
{
    [global::System.Security.SecurityCriticalAttribute]
    public static partial class WindowsRuntimeStorageExtensions
    {
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.Threading.Tasks.Task<global::System.IO.Stream> OpenStreamForReadAsync(this global::Windows.Storage.IStorageFile windowsRuntimeFile) { return default(global::System.Threading.Tasks.Task<global::System.IO.Stream>); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.Threading.Tasks.Task<global::System.IO.Stream> OpenStreamForReadAsync(this global::Windows.Storage.IStorageFolder rootDirectory, string relativePath) { return default(global::System.Threading.Tasks.Task<global::System.IO.Stream>); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.Threading.Tasks.Task<global::System.IO.Stream> OpenStreamForWriteAsync(this global::Windows.Storage.IStorageFile windowsRuntimeFile) { return default(global::System.Threading.Tasks.Task<global::System.IO.Stream>); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.Threading.Tasks.Task<global::System.IO.Stream> OpenStreamForWriteAsync(this global::Windows.Storage.IStorageFolder rootDirectory, string relativePath, global::Windows.Storage.CreationCollisionOption creationCollisionOption) { return default(global::System.Threading.Tasks.Task<global::System.IO.Stream>); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    public static partial class WindowsRuntimeStreamExtensions
    {
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IInputStream AsInputStream(this global::System.IO.Stream stream) { return default(global::Windows.Storage.Streams.IInputStream); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IOutputStream AsOutputStream(this global::System.IO.Stream stream) { return default(global::Windows.Storage.Streams.IOutputStream); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IRandomAccessStream AsRandomAccessStream(this global::System.IO.Stream stream) { return default(global::Windows.Storage.Streams.IRandomAccessStream); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStream(this global::Windows.Storage.Streams.IRandomAccessStream windowsRuntimeStream) { return default(global::System.IO.Stream); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStream(this global::Windows.Storage.Streams.IRandomAccessStream windowsRuntimeStream, int bufferSize) { return default(global::System.IO.Stream); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStreamForRead(this global::Windows.Storage.Streams.IInputStream windowsRuntimeStream) { return default(global::System.IO.Stream); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStreamForRead(this global::Windows.Storage.Streams.IInputStream windowsRuntimeStream, int bufferSize) { return default(global::System.IO.Stream); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStreamForWrite(this global::Windows.Storage.Streams.IOutputStream windowsRuntimeStream) { return default(global::System.IO.Stream); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStreamForWrite(this global::Windows.Storage.Streams.IOutputStream windowsRuntimeStream, int bufferSize) { return default(global::System.IO.Stream); }
    }
}
namespace System.Runtime.InteropServices.WindowsRuntime
{
    [global::System.CLSCompliantAttribute(false)]
    [global::System.Security.SecurityCriticalAttribute]
    public static partial class AsyncInfo
    {
        public static global::Windows.Foundation.IAsyncAction Run(global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task> taskProvider) { return default(global::Windows.Foundation.IAsyncAction); }
        public static global::Windows.Foundation.IAsyncActionWithProgress<TProgress> Run<TProgress>(global::System.Func<global::System.Threading.CancellationToken, global::System.IProgress<TProgress>, global::System.Threading.Tasks.Task> taskProvider) { return default(global::Windows.Foundation.IAsyncActionWithProgress<TProgress>); }
        public static global::Windows.Foundation.IAsyncOperation<TResult> Run<TResult>(global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task<TResult>> taskProvider) { return default(global::Windows.Foundation.IAsyncOperation<TResult>); }
        public static global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> Run<TResult, TProgress>(global::System.Func<global::System.Threading.CancellationToken, global::System.IProgress<TProgress>, global::System.Threading.Tasks.Task<TResult>> taskProvider) { return default(global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress>); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    public sealed partial class WindowsRuntimeBuffer
    {
        internal WindowsRuntimeBuffer() { }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer Create(byte[] data, int offset, int length, int capacity) { return default(global::Windows.Storage.Streams.IBuffer); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer Create(int capacity) { return default(global::Windows.Storage.Streams.IBuffer); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    public static partial class WindowsRuntimeBufferExtensions
    {
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer AsBuffer(this byte[] source) { return default(global::Windows.Storage.Streams.IBuffer); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer AsBuffer(this byte[] source, int offset, int length) { return default(global::Windows.Storage.Streams.IBuffer); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer AsBuffer(this byte[] source, int offset, int length, int capacity) { return default(global::Windows.Storage.Streams.IBuffer); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStream(this global::Windows.Storage.Streams.IBuffer source) { return default(global::System.IO.Stream); }
        [global::System.CLSCompliantAttribute(false)]
        public static void CopyTo(this byte[] source, int sourceIndex, global::Windows.Storage.Streams.IBuffer destination, uint destinationIndex, int count) { }
        [global::System.CLSCompliantAttribute(false)]
        public static void CopyTo(this byte[] source, global::Windows.Storage.Streams.IBuffer destination) { }
        [global::System.CLSCompliantAttribute(false)]
        public static void CopyTo(this global::Windows.Storage.Streams.IBuffer source, byte[] destination) { }
        [global::System.CLSCompliantAttribute(false)]
        public static void CopyTo(this global::Windows.Storage.Streams.IBuffer source, uint sourceIndex, byte[] destination, int destinationIndex, int count) { }
        [global::System.CLSCompliantAttribute(false)]
        public static void CopyTo(this global::Windows.Storage.Streams.IBuffer source, uint sourceIndex, global::Windows.Storage.Streams.IBuffer destination, uint destinationIndex, uint count) { }
        [global::System.CLSCompliantAttribute(false)]
        public static void CopyTo(this global::Windows.Storage.Streams.IBuffer source, global::Windows.Storage.Streams.IBuffer destination) { }
        [global::System.CLSCompliantAttribute(false)]
        public static byte GetByte(this global::Windows.Storage.Streams.IBuffer source, uint byteOffset) { return default(byte); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer GetWindowsRuntimeBuffer(this global::System.IO.MemoryStream underlyingStream) { return default(global::Windows.Storage.Streams.IBuffer); }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer GetWindowsRuntimeBuffer(this global::System.IO.MemoryStream underlyingStream, int positionInStream, int length) { return default(global::Windows.Storage.Streams.IBuffer); }
        [global::System.CLSCompliantAttribute(false)]
        public static bool IsSameData(this global::Windows.Storage.Streams.IBuffer buffer, global::Windows.Storage.Streams.IBuffer otherBuffer) { return default(bool); }
        [global::System.CLSCompliantAttribute(false)]
        public static byte[] ToArray(this global::Windows.Storage.Streams.IBuffer source) { return default(byte[]); }
        [global::System.CLSCompliantAttribute(false)]
        public static byte[] ToArray(this global::Windows.Storage.Streams.IBuffer source, uint sourceIndex, int count) { return default(byte[]); }
    }
}
namespace Windows.Foundation
{
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Point
    {
        public Point(double x, double y) { throw new global::System.NotImplementedException(); }
        public double X { get { return default(double); } set { } }
        public double Y { get { return default(double); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object o) { return default(bool); }
        public bool Equals(global::Windows.Foundation.Point value) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.Foundation.Point point1, global::Windows.Foundation.Point point2) { return default(bool); }
        public static bool operator !=(global::Windows.Foundation.Point point1, global::Windows.Foundation.Point point2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
        public string ToString(global::System.IFormatProvider provider) { return default(string); }
    }
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Rect
    {
        public Rect(double x, double y, double width, double height) { throw new global::System.NotImplementedException(); }
        public Rect(global::Windows.Foundation.Point point1, global::Windows.Foundation.Point point2) { throw new global::System.NotImplementedException(); }
        public Rect(global::Windows.Foundation.Point location, global::Windows.Foundation.Size size) { throw new global::System.NotImplementedException(); }
        public double Bottom { get { return default(double); } }
        public static global::Windows.Foundation.Rect Empty { get { return default(global::Windows.Foundation.Rect); } }
        public double Height { get { return default(double); } set { } }
        public bool IsEmpty { get { return default(bool); } }
        public double Left { get { return default(double); } }
        public double Right { get { return default(double); } }
        public double Top { get { return default(double); } }
        public double Width { get { return default(double); } set { } }
        public double X { get { return default(double); } set { } }
        public double Y { get { return default(double); } set { } }
        public bool Contains(global::Windows.Foundation.Point point) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object o) { return default(bool); }
        public bool Equals(global::Windows.Foundation.Rect value) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public void Intersect(global::Windows.Foundation.Rect rect) { }
        public static bool operator ==(global::Windows.Foundation.Rect rect1, global::Windows.Foundation.Rect rect2) { return default(bool); }
        public static bool operator !=(global::Windows.Foundation.Rect rect1, global::Windows.Foundation.Rect rect2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
        public string ToString(global::System.IFormatProvider provider) { return default(string); }
        public void Union(global::Windows.Foundation.Point point) { }
        public void Union(global::Windows.Foundation.Rect rect) { }
    }
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Size
    {
        public Size(double width, double height) { throw new global::System.NotImplementedException(); }
        public static global::Windows.Foundation.Size Empty { get { return default(global::Windows.Foundation.Size); } }
        public double Height { get { return default(double); } set { } }
        public bool IsEmpty { get { return default(bool); } }
        public double Width { get { return default(double); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object o) { return default(bool); }
        public bool Equals(global::Windows.Foundation.Size value) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.Foundation.Size size1, global::Windows.Foundation.Size size2) { return default(bool); }
        public static bool operator !=(global::Windows.Foundation.Size size1, global::Windows.Foundation.Size size2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
    }
}
namespace Windows.UI
{
    [global::System.Security.SecurityCriticalAttribute]
    [global::System.Runtime.InteropServices.StructLayoutAttribute(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Color
    {
        public byte A { get { return default(byte); } set { } }
        public byte B { get { return default(byte); } set { } }
        public byte G { get { return default(byte); } set { } }
        public byte R { get { return default(byte); } set { } }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override bool Equals(object o) { return default(bool); }
        public bool Equals(global::Windows.UI.Color color) { return default(bool); }
        public static global::Windows.UI.Color FromArgb(byte a, byte r, byte g, byte b) { return default(global::Windows.UI.Color); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(global::Windows.UI.Color color1, global::Windows.UI.Color color2) { return default(bool); }
        public static bool operator !=(global::Windows.UI.Color color1, global::Windows.UI.Color color2) { return default(bool); }
        [global::System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
        public string ToString(global::System.IFormatProvider provider) { return default(string); }
    }
}
