// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.Versioning;

namespace System.Xml
{
    internal enum XmlStandalone
    {
        // Do not change the constants - XmlBinaryWriter depends in it
        Omit = 0,
        Yes = 1,
        No = 2,
    }
}
