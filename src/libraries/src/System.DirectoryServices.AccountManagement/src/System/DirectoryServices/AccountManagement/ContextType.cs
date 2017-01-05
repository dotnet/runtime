// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.DirectoryServices.AccountManagement
{
    public enum ContextType
    {
        Machine = 0,
        Domain = 1,
#if TESTHOOK        
        ApplicationDirectory = 2,
        Test            =   3
#else
        ApplicationDirectory = 2
#endif // TESTHOOK
    }
}

