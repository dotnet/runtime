// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Xml
{
    public interface IStreamProvider
    {
        Stream GetStream();
        void ReleaseStream(Stream stream);
    }
}
