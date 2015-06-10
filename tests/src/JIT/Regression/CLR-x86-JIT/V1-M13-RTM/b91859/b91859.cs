// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
class AA
{
    static int Main()
    {
        bool b = false;
        b = (b ? (object)b : (object)new AA()) ==
            (b ? new AA() : (b ? new AA() : null));
        return 100;
    }
}
