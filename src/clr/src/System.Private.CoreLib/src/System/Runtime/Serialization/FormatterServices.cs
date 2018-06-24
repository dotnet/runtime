// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Provides some static methods to aid with the implementation
**          of a Formatter for Serialization.
**
**
============================================================*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.IO;
using System.Text;
using System.Globalization;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    // This class duplicates a class on CoreFX. We are keeping it here -- just this one method --
    // as it was widely invoked by reflection to workaround it being missing in .NET Core 1.0
    internal static class FormatterServices
    {
        // Gets a new instance of the object.  The entire object is initalized to 0 and no 
        // constructors have been run. **THIS MEANS THAT THE OBJECT MAY NOT BE IN A STATE
        // CONSISTENT WITH ITS INTERNAL REQUIREMENTS** This method should only be used for
        // deserialization when the user intends to immediately populate all fields.  This method
        // will not create an unitialized string because it is non-sensical to create an empty
        // instance of an immutable type.
        //
        public static object GetUninitializedObject(Type type)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!(type is RuntimeType))
            {
                throw new SerializationException(SR.Format(SR.Serialization_InvalidType, type.ToString()));
            }

            return nativeGetUninitializedObject((RuntimeType)type);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object nativeGetUninitializedObject(RuntimeType type);
    }
}





