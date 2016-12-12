/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    Pair.cs

Abstract:

    Implements the Pair class.

History:

    10-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    class Pair<J,K>
    {
        //
        // Constructor
        //
        internal Pair(J left, K right)
        {
            this.left = left;
            this.right = right;
        }

        //
        // Public properties
        //
        internal J Left
        {
            get { return this.left; }
            set { this.left = value; }
        }

        internal K Right
        {
            get { return this.right; }
            set { this.right = value; }
        }


        //
        // Private implementation
        //

        J left;
        K right;
    }
}
