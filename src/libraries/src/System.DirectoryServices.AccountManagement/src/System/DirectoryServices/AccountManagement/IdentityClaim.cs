/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    IdentityClaim.cs

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
            get {return this.urnValue;}
            set {this.urnValue = value;}
        }
        
        public string UrnScheme
        {
            set {this.urnScheme = value;}
            get {return this.urnScheme; }
        }
        
        
        string urnValue;
        string urnScheme;

        public IdentityClaim()
        {
            // Nothing to do here
        }
    }
}
