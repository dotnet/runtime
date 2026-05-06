// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.Serialization.Json
{
    public interface IXmlJsonWriterInitializer
    {
        void SetOutput(Stream stream, Encoding encoding, bool ownsStream);
    }
}
