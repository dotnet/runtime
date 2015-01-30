// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        [System.Security.SecurityCritical]  // auto-generated_required
        bool RegisterAssembly(Assembly assembly, AssemblyRegistrationFlags flags);

        [System.Security.SecurityCritical]  // auto-generated_required
        bool UnregisterAssembly(Assembly assembly);

        [System.Security.SecurityCritical]  // auto-generated_required
        Type[] GetRegistrableTypesInAssembly(Assembly assembly);

        [System.Security.SecurityCritical]  // auto-generated_required
        String GetProgIdForType(Type type);

        [System.Security.SecurityCritical]  // auto-generated_required
        void RegisterTypeForComClients(Type type, ref Guid g);

        Guid GetManagedCategoryGuid();

        [System.Security.SecurityCritical]  // auto-generated_required
        bool TypeRequiresRegistration(Type type);

        bool TypeRepresentsComType(Type type);
    }
}
