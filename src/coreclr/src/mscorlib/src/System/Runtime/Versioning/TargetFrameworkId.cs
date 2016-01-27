// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Purpose: Describe the target framework of the application or AppDomain.
**
===========================================================*/
using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.Versioning
{
    // What type of .NET Framework was this application compiled against?
    [FriendAccessAllowed]
    internal enum TargetFrameworkId
    {
        NotYetChecked = 0,
        Unrecognized = 1, // Unknown type, such as a new SKU (like watches or cars)
        Unspecified = 2, // The TargetFrameworkAttribute was created in v4.0.  And apps compiled outside VS will not have this attribute.
        NetFramework = 3, // Desktop - Client or Server or ServerCore.
        Portable = 4, // Portable Library v1   Note: We do not expect people to build executables against portable libraries!
        NetCore = 5, // .NET Core = Windows 8 Immersive and Portable Library v2+
        Silverlight = 6, // Silverlight but not the Phone
        Phone = 7, // Windows Phone 7 or higher
    }
}
