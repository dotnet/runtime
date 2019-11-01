// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    internal class IdentityReference
    {
        public string UrnValue
        {
            get { return _urnValue; }
            set { _urnValue = value; }
        }

        public string UrnScheme
        {
            get { return _urnScheme; }
            set { _urnScheme = value; }
        }

        private string _urnValue;
        private string _urnScheme;

        public IdentityReference()
        {
            // Do nothing
        }
    }
}
