// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using OLEDB.Test.ModuleCore;

namespace Webdata.Test.XmlDriver
{
    /// <summary>
    /// CXmlDriverException.
    /// </summary>
    public class CXmlDriverException : CTestException
    {
        internal CXmlDriverException(string msg) : base(msg) { }
    }
}
