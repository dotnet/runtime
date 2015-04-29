// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace System.Net.Http
{
    internal enum HttpParseResult
    {
        Parsed,
        NotParsed,
        InvalidFormat,
    }
}
