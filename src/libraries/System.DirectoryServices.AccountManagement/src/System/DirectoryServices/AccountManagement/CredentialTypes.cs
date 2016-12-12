// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    MatchType.cs

Abstract:

    Implements the MatchType enum.
    
History:

    05-May-2004    MattRim     Created

--*/

using System;

namespace System.DirectoryServices.AccountManagement
{
    [Flags]
    internal enum CredentialTypes
    {
        Password = 1,
        Certificate = 2
    }
}
