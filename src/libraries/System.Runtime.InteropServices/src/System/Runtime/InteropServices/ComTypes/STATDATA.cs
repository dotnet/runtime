// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.ComTypes
{
    public struct STATDATA
    {
        public FORMATETC formatetc;
        public ADVF advf;
        public IAdviseSink advSink;
        public int connection;
    }
}
