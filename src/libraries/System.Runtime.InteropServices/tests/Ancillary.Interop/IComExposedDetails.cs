// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    public unsafe interface IComExposedDetails
    {
        ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count);

        internal static IComExposedDetails? GetFromAttribute(RuntimeTypeHandle handle)
        {
            var type = Type.GetTypeFromHandle(handle);
            if (type is null)
            {
                return null;
            }
            return (IComExposedDetails?)type.GetCustomAttribute(typeof(ComExposedClassAttribute<>));
        }
    }
}
