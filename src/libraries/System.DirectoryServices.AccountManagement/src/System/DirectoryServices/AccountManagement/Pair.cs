// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class Pair<J, K>
    {
        //
        // Constructor
        //
        internal Pair(J left, K right)
        {
            _left = left;
            _right = right;
        }

        //
        // Public properties
        //
        internal J Left
        {
            get { return _left; }
            set { _left = value; }
        }

        internal K Right
        {
            get { return _right; }
            set { _right = value; }
        }

        //
        // Private implementation
        //

        private J _left;
        private K _right;
    }
}
