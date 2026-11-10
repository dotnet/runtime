// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="TextWriter"/>.</summary>
internal static class TextWriterPolyfills
{
    extension(TextWriter writer)
    {
        public void Write(ReadOnlySpan<char> value) =>
            writer.Write(value.ToString());
    }
}
