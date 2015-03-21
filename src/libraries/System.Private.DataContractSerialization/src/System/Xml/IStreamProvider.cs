//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace System.Xml
{
    using System.IO;

    public interface IStreamProvider
    {
        Stream GetStream();
        void ReleaseStream(Stream stream);
    }
}
