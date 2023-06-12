// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("E4461914-4202-479F-8427-620E915F84B9")]
    internal partial interface IPointProvider
    {
        [PreserveSig]
        Point GetPoint();

        void SetPoint(Point point);
    }
}
