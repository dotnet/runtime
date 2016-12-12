/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    IdentityReference.cs

Abstract:

    Temporarily implementation of the IdentityReference class, to let everything compile.

History:

    05-May-2004    MattRim     Created

--*/

using System;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    internal class IdentityReference
    {

        public string UrnValue
        {
            get {return this.urnValue;}
            set {this.urnValue = value;}
        }
        
        public string UrnScheme
        {
            get {return this.urnScheme;}
            set {this.urnScheme = value;}
        }

        string urnValue;
        string urnScheme;
        
        public IdentityReference()
        {
            // Do nothing
        }
    }

}
