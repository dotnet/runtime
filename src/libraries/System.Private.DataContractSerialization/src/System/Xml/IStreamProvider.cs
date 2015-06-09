// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Xml
{
    public interface IStreamProvider
    {
        Stream GetStream();
        void ReleaseStream(Stream stream);
    }
}
