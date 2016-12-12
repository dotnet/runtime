// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:


Abstract:

    Temporarily implementation of the IdentityClaim class, to let everything compile.

History:

    05-May-2004    MattRim     Created

--*/

using System;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    internal class IdentityClaim
    {
        public string UrnValue
        {
            get { return _urnValue; }
            set { _urnValue = value; }
        }

        public string UrnScheme
        {
            set { _urnScheme = value; }
            get { return _urnScheme; }
        }


        private string _urnValue;
        private string _urnScheme;

        public IdentityClaim()
        {
            // Nothing to do here
        }
    }
}
