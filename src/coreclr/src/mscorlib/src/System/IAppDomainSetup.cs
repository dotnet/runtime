// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  IAppDomainSetup
** 
** 
**
**
** Purpose: Properties exposed to COM
**
** 
===========================================================*/
namespace System {

    using System.Runtime.InteropServices;

    [GuidAttribute("27FFF232-A7A8-40dd-8D4A-734AD59FCD41")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IAppDomainSetup
    {
        String ApplicationBase {
            get;
            set;
        }

        String ApplicationName
        {
            get;
            set;
        }

        String CachePath
        {
            get;
            set;
        }

        String ConfigurationFile {
            get;
            set;
        }

        String DynamicBase
        {
            get;
            set;
        }

        String LicenseFile
        {
            get;
            set;
        }

        String PrivateBinPath
        {
            get;
            set;
        }

        String PrivateBinPathProbe
        {
            get;
            set;
        }

        String ShadowCopyDirectories
        {
            get;
            set;
        }

        String ShadowCopyFiles
        {
            get;
            set;
        }

    }
}
