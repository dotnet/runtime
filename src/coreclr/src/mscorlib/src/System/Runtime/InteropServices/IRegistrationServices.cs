// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: This interface provides services for registering and unregistering
**          a managed server for use by COM.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices {
    
    using System;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;

    [Flags()]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum AssemblyRegistrationFlags
    {
        None                    = 0x00000000,
        SetCodeBase             = 0x00000001,
    }

    [Guid("CCBD682C-73A5-4568-B8B0-C7007E11ABA2")]
[System.Runtime.InteropServices.ComVisible(true)]
    public interface IRegistrationServices
    {
        bool RegisterAssembly(Assembly assembly, AssemblyRegistrationFlags flags);

        bool UnregisterAssembly(Assembly assembly);

        Type[] GetRegistrableTypesInAssembly(Assembly assembly);

        String GetProgIdForType(Type type);

        void RegisterTypeForComClients(Type type, ref Guid g);

        Guid GetManagedCategoryGuid();

        bool TypeRequiresRegistration(Type type);

        bool TypeRepresentsComType(Type type);
    }
}
