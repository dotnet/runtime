// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Text.Json.Serialization.Tests
{
    public sealed class Utf8MemoryStream : MemoryStream
    {
        public Utf8MemoryStream() : base()
        {
        }

        public Utf8MemoryStream(string text) : base(Encoding.UTF8.GetBytes(text))
        {
        }

        public string AsString() => Encoding.UTF8.GetString(ToArray());
    }
}
