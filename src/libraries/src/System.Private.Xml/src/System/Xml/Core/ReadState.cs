// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    // Specifies the state of the XmlReader.
    public enum ReadState
    {
        // The Read method has not been called yet.
        Initial = 0,

        // Reading is in progress.
        Interactive = 1,

        // An error occurred that prevents the XmlReader from continuing.
        Error = 2,

        // The end of the stream has been reached successfully.
        EndOfFile = 3,

        // The Close method has been called and the XmlReader is closed.
        Closed = 4
    }
}
