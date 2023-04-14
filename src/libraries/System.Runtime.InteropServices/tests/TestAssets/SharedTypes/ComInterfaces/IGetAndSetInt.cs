// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(_guid)]
    partial interface IGetAndSetInt
    {
        int GetInt();

        public void SetInt(int x);

        public const string _guid = "2c3f9903-b586-46b1-881b-adfce9af47b1";
    }
}
