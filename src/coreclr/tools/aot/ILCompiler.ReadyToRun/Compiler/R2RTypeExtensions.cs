// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public static class R2RTypeExtensions
    {
        /// <summary>
        /// Return true when the type in question is marked with the NonVersionable attribute. Primitive types are implicitly NonVersionable
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True when the type is marked with the non-versionable custom attribute and meets the criteria
        /// for a non-versionable type, false otherwise.</returns>
        public static bool IsNonVersionable(this MetadataType type)
        {
            bool result = type.HasCustomAttribute("System.Runtime.Versioning", "NonVersionableAttribute");

            if (!type.IsValueType)
            {
                if (type.BaseType != type.Context.GetWellKnownType(WellKnownType.Object)) // Only types that derive directly from Object can be non-versionable
                    result = false;

                // Only reference types defined in System.Private.CoreLib are eligible for the non-versionable processing
                // This allows for extremely careful vetting of said types
                if (type.Module != type.Context.SystemModule)
                    result = false;
            }
            else if (type.IsPrimitive)
            {
                // The primitive types are all NonVersionable
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Return true when the method is marked as non-versionable. Non-versionable methods
        /// may be freely inlined into ReadyToRun images even when they don't reside in the
        /// same version bubble as the module being compiled.
        /// </summary>
        /// <param name="method">Method to check</param>
        /// <returns>True when the method is marked as non-versionable, false otherwise.</returns>
        public static bool IsNonVersionable(this MethodDesc method)
        {
            return method.HasCustomAttribute("System.Runtime.Versioning", "NonVersionableAttribute");
        }
    }
}
