// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization.Json
{
    internal interface IXmlJsonWriterInitializer
    {
        void SetOutput(Stream stream, Encoding encoding, bool ownsStream);
    }
}
