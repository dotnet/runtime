using System;
internal static partial class Interop
{
    internal static partial class JavaScript
    {
        public class SharedArrayBuffer : CoreObject
        {
            public SharedArrayBuffer(int length) : base(Runtime.New<SharedArrayBuffer>(length))
            { }

            internal SharedArrayBuffer(IntPtr js_handle) : base(js_handle)
            { }

            public int ByteLength => (int)GetObjectProperty("byteLength");
            public SharedArrayBuffer Slice(int begin, int end) => (SharedArrayBuffer)Invoke("slice", begin, end);
        }
    }
}
