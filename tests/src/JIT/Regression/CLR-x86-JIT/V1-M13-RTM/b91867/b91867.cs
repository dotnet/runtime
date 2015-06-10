// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
class CC
{
    static int Main()
    {
        bool b = false;
        object local19 = b ? null : (object)new CC();
#pragma warning disable 1718
        String[] local21 = (b == b ? b : b) ? new string[1] : null;
#pragma warning restore 1718
        return 100;
    }
}
