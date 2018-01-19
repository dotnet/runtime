// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    [global::System.CLSCompliantAttribute(false)]
    public static partial class WindowsRuntimeSystemExtensions
    {
        public static global::Windows.Foundation.IAsyncAction AsAsyncAction(this global::System.Threading.Tasks.Task source) { throw null; }
        public static global::Windows.Foundation.IAsyncOperation<TResult> AsAsyncOperation<TResult>(this global::System.Threading.Tasks.Task<TResult> source) { throw null; }
        public static global::System.Threading.Tasks.Task AsTask(this global::Windows.Foundation.IAsyncAction source) { throw null; }
        public static global::System.Threading.Tasks.Task AsTask(this global::Windows.Foundation.IAsyncAction source, global::System.Threading.CancellationToken cancellationToken) { throw null; }
        public static global::System.Threading.Tasks.Task AsTask<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source) { throw null; }
        public static global::System.Threading.Tasks.Task AsTask<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source, global::System.IProgress<TProgress> progress) { throw null; }
        public static global::System.Threading.Tasks.Task AsTask<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source, global::System.Threading.CancellationToken cancellationToken) { throw null; }
        public static global::System.Threading.Tasks.Task AsTask<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source, global::System.Threading.CancellationToken cancellationToken, global::System.IProgress<TProgress> progress) { throw null; }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult>(this global::Windows.Foundation.IAsyncOperation<TResult> source) { throw null; }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult>(this global::Windows.Foundation.IAsyncOperation<TResult> source, global::System.Threading.CancellationToken cancellationToken) { throw null; }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source) { throw null; }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source, global::System.IProgress<TProgress> progress) { throw null; }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source, global::System.Threading.CancellationToken cancellationToken) { throw null; }
        public static global::System.Threading.Tasks.Task<TResult> AsTask<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source, global::System.Threading.CancellationToken cancellationToken, global::System.IProgress<TProgress> progress) { throw null; }
        [global::System.ComponentModel.EditorBrowsableAttribute((global::System.ComponentModel.EditorBrowsableState)(1))]
        public static global::System.Runtime.CompilerServices.TaskAwaiter GetAwaiter(this global::Windows.Foundation.IAsyncAction source) { throw null; }
        [global::System.ComponentModel.EditorBrowsableAttribute((global::System.ComponentModel.EditorBrowsableState)(1))]
        public static global::System.Runtime.CompilerServices.TaskAwaiter GetAwaiter<TProgress>(this global::Windows.Foundation.IAsyncActionWithProgress<TProgress> source) { throw null; }
        [global::System.ComponentModel.EditorBrowsableAttribute((global::System.ComponentModel.EditorBrowsableState)(1))]
        public static global::System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter<TResult>(this global::Windows.Foundation.IAsyncOperation<TResult> source) { throw null; }
        [global::System.ComponentModel.EditorBrowsableAttribute((global::System.ComponentModel.EditorBrowsableState)(1))]
        public static global::System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> source) { throw null; }
    }
}
namespace System.IO
{
    public static partial class WindowsRuntimeStorageExtensions
    {
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.Threading.Tasks.Task<global::System.IO.Stream> OpenStreamForReadAsync(this global::Windows.Storage.IStorageFile windowsRuntimeFile) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.Threading.Tasks.Task<global::System.IO.Stream> OpenStreamForReadAsync(this global::Windows.Storage.IStorageFolder rootDirectory, string relativePath) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.Threading.Tasks.Task<global::System.IO.Stream> OpenStreamForWriteAsync(this global::Windows.Storage.IStorageFile windowsRuntimeFile) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.Threading.Tasks.Task<global::System.IO.Stream> OpenStreamForWriteAsync(this global::Windows.Storage.IStorageFolder rootDirectory, string relativePath, global::Windows.Storage.CreationCollisionOption creationCollisionOption) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Microsoft.Win32.SafeHandles.SafeFileHandle CreateSafeFileHandle(
            this global::Windows.Storage.IStorageFile windowsRuntimeFile,
            global::System.IO.FileAccess access = global::System.IO.FileAccess.ReadWrite,
            global::System.IO.FileShare share = global::System.IO.FileShare.Read,
            global::System.IO.FileOptions options = global::System.IO.FileOptions.None) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Microsoft.Win32.SafeHandles.SafeFileHandle CreateSafeFileHandle(
            this global::Windows.Storage.IStorageFolder rootDirectory,
            string relativePath,
            global::System.IO.FileMode mode) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Microsoft.Win32.SafeHandles.SafeFileHandle CreateSafeFileHandle(
            this global::Windows.Storage.IStorageFolder rootDirectory,
            string relativePath,
            global::System.IO.FileMode mode,
            global::System.IO.FileAccess access,
            global::System.IO.FileShare share = global::System.IO.FileShare.Read,
            global::System.IO.FileOptions options = global::System.IO.FileOptions.None) { throw null; }
    }
    public static partial class WindowsRuntimeStreamExtensions
    {
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IInputStream AsInputStream(this global::System.IO.Stream stream) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IOutputStream AsOutputStream(this global::System.IO.Stream stream) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IRandomAccessStream AsRandomAccessStream(this global::System.IO.Stream stream) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStream(this global::Windows.Storage.Streams.IRandomAccessStream windowsRuntimeStream) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStream(this global::Windows.Storage.Streams.IRandomAccessStream windowsRuntimeStream, int bufferSize) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStreamForRead(this global::Windows.Storage.Streams.IInputStream windowsRuntimeStream) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStreamForRead(this global::Windows.Storage.Streams.IInputStream windowsRuntimeStream, int bufferSize) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStreamForWrite(this global::Windows.Storage.Streams.IOutputStream windowsRuntimeStream) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStreamForWrite(this global::Windows.Storage.Streams.IOutputStream windowsRuntimeStream, int bufferSize) { throw null; }
    }
}
namespace System.Runtime.InteropServices.WindowsRuntime
{
    [global::System.CLSCompliantAttribute(false)]
    public static partial class AsyncInfo
    {
        public static global::Windows.Foundation.IAsyncAction Run(global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task> taskProvider) { throw null; }
        public static global::Windows.Foundation.IAsyncActionWithProgress<TProgress> Run<TProgress>(global::System.Func<global::System.Threading.CancellationToken, global::System.IProgress<TProgress>, global::System.Threading.Tasks.Task> taskProvider) { throw null; }
        public static global::Windows.Foundation.IAsyncOperation<TResult> Run<TResult>(global::System.Func<global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task<TResult>> taskProvider) { throw null; }
        public static global::Windows.Foundation.IAsyncOperationWithProgress<TResult, TProgress> Run<TResult, TProgress>(global::System.Func<global::System.Threading.CancellationToken, global::System.IProgress<TProgress>, global::System.Threading.Tasks.Task<TResult>> taskProvider) { throw null; }
    }
    public sealed partial class WindowsRuntimeBuffer
    {
        internal WindowsRuntimeBuffer() { }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer Create(byte[] data, int offset, int length, int capacity) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer Create(int capacity) { throw null; }
    }
    public static partial class WindowsRuntimeBufferExtensions
    {
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer AsBuffer(this byte[] source) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer AsBuffer(this byte[] source, int offset, int length) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer AsBuffer(this byte[] source, int offset, int length, int capacity) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::System.IO.Stream AsStream(this global::Windows.Storage.Streams.IBuffer source) { throw null; }
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
        public static byte GetByte(this global::Windows.Storage.Streams.IBuffer source, uint byteOffset) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer GetWindowsRuntimeBuffer(this global::System.IO.MemoryStream underlyingStream) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static global::Windows.Storage.Streams.IBuffer GetWindowsRuntimeBuffer(this global::System.IO.MemoryStream underlyingStream, int positionInStream, int length) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static bool IsSameData(this global::Windows.Storage.Streams.IBuffer buffer, global::Windows.Storage.Streams.IBuffer otherBuffer) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static byte[] ToArray(this global::Windows.Storage.Streams.IBuffer source) { throw null; }
        [global::System.CLSCompliantAttribute(false)]
        public static byte[] ToArray(this global::Windows.Storage.Streams.IBuffer source, uint sourceIndex, int count) { throw null; }
    }
}
namespace Windows.Foundation
{
    public partial struct Point
    {
        private int _dummy;
        public Point(double x, double y) { throw null; }
        public double X { get { throw null; } set { } }
        public double Y { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public bool Equals(global::Windows.Foundation.Point value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.Foundation.Point point1, global::Windows.Foundation.Point point2) { throw null; }
        public static bool operator !=(global::Windows.Foundation.Point point1, global::Windows.Foundation.Point point2) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(global::System.IFormatProvider provider) { throw null; }
    }
    public partial struct Rect
    {
        private int _dummy;
        public Rect(double x, double y, double width, double height) { throw null; }
        public Rect(global::Windows.Foundation.Point point1, global::Windows.Foundation.Point point2) { throw null; }
        public Rect(global::Windows.Foundation.Point location, global::Windows.Foundation.Size size) { throw null; }
        public double Bottom { get { throw null; } }
        public static global::Windows.Foundation.Rect Empty { get { throw null; } }
        public double Height { get { throw null; } set { } }
        public bool IsEmpty { get { throw null; } }
        public double Left { get { throw null; } }
        public double Right { get { throw null; } }
        public double Top { get { throw null; } }
        public double Width { get { throw null; } set { } }
        public double X { get { throw null; } set { } }
        public double Y { get { throw null; } set { } }
        public bool Contains(global::Windows.Foundation.Point point) { throw null; }
        public override bool Equals(object o) { throw null; }
        public bool Equals(global::Windows.Foundation.Rect value) { throw null; }
        public override int GetHashCode() { throw null; }
        public void Intersect(global::Windows.Foundation.Rect rect) { }
        public static bool operator ==(global::Windows.Foundation.Rect rect1, global::Windows.Foundation.Rect rect2) { throw null; }
        public static bool operator !=(global::Windows.Foundation.Rect rect1, global::Windows.Foundation.Rect rect2) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(global::System.IFormatProvider provider) { throw null; }
        public void Union(global::Windows.Foundation.Point point) { }
        public void Union(global::Windows.Foundation.Rect rect) { }
    }
    public partial struct Size
    {
        private int _dummy;
        public Size(double width, double height) { throw null; }
        public static global::Windows.Foundation.Size Empty { get { throw null; } }
        public double Height { get { throw null; } set { } }
        public bool IsEmpty { get { throw null; } }
        public double Width { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public bool Equals(global::Windows.Foundation.Size value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.Foundation.Size size1, global::Windows.Foundation.Size size2) { throw null; }
        public static bool operator !=(global::Windows.Foundation.Size size1, global::Windows.Foundation.Size size2) { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace Windows.UI
{
    public partial struct Color
    {
        private int _dummy;
        public byte A { get { throw null; } set { } }
        public byte B { get { throw null; } set { } }
        public byte G { get { throw null; } set { } }
        public byte R { get { throw null; } set { } }
        public override bool Equals(object o) { throw null; }
        public bool Equals(global::Windows.UI.Color color) { throw null; }
        public static global::Windows.UI.Color FromArgb(byte a, byte r, byte g, byte b) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(global::Windows.UI.Color color1, global::Windows.UI.Color color2) { throw null; }
        public static bool operator !=(global::Windows.UI.Color color1, global::Windows.UI.Color color2) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(global::System.IFormatProvider provider) { throw null; }
    }
}
