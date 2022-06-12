// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Runtime.TypeInfos;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.General
{
    internal static class ToStringUtils
    {
        //
        // This is a port of the desktop CLR's RuntimeType.FormatTypeName() routine. This routine is used by various Reflection ToString() methods
        // to display the name of a type. Do not use for any other purpose as it inherits some pretty quirky desktop behavior.
        //
        // The Project N version takes a raw metadata handle rather than a completed type so that it remains robust in the face of missing metadata.
        //
        public static string FormatTypeName(this QTypeDefRefOrSpec qualifiedTypeHandle, TypeContext typeContext)
        {
            try
            {
                // Though we wrap this in a try-catch as a failsafe, this code must still strive to avoid triggering MissingMetadata exceptions
                // (non-error exceptions are very annoying when debugging.)

                Exception? exception = null;
                RuntimeTypeInfo runtimeType = qualifiedTypeHandle.TryResolve(typeContext, ref exception);
                if (runtimeType == null)
                    return Type.DefaultTypeNameWhenMissingMetadata;

                // Because this runtimeType came from a successful TryResolve() call, it is safe to querying the TypeInfo's of the type and its component parts.
                // If we're wrong, we do have the safety net of a try-catch.
                return runtimeType.FormatTypeNameForReflection();
            }
            catch (Exception)
            {
                return Type.DefaultTypeNameWhenMissingMetadata;
            }
        }
    }
}
