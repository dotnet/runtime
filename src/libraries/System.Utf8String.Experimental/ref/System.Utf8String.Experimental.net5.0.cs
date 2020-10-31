// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public static partial class Utf8Extensions
    {
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, System.Index startIndex) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, int start) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, System.Range range) { throw null; }
    }
}
namespace System.Net.Http
{
    public sealed partial class Utf8StringContent : System.Net.Http.HttpContent
    {
        protected override System.IO.Stream CreateContentReadStream(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void SerializeToStream(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
