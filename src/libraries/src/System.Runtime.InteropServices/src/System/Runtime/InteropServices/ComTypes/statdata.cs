// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
