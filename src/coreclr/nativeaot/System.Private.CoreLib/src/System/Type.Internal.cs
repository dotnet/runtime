// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

using Internal.Runtime.Augments;

namespace System
{
    //
    // This file contains methods on Type that are internal to the framework.
    //
    // Before adding new entries to this, ask yourself: is it ever referenced by System.Private.CoreLib?
    // If not, don't put it here. Put it on RuntimeTypeInfo instead.
    //
    public abstract partial class Type
    {
        internal EETypePtr GetEEType()
        {
            RuntimeTypeHandle typeHandle = RuntimeAugments.Callbacks.GetTypeHandleIfAvailable(this);
            Debug.Assert(!typeHandle.IsNull);
            return typeHandle.ToEETypePtr();
        }

        internal bool TryGetEEType(out EETypePtr eeType)
        {
            RuntimeTypeHandle typeHandle = RuntimeAugments.Callbacks.GetTypeHandleIfAvailable(this);
            if (typeHandle.IsNull)
            {
                eeType = default(EETypePtr);
                return false;
            }
            eeType = typeHandle.ToEETypePtr();
            return true;
        }

        /// <summary>
        /// Return Type.Name if sufficient metadata is available to do so - otherwise return null.
        /// </summary>
        public string? InternalNameIfAvailable
        {
            get
            {
                Type? ignore = null;
                return InternalGetNameIfAvailable(ref ignore);
            }
        }

        /// <summary>
        /// Return Type.Name if sufficient metadata is available to do so - otherwise return null and set "rootCauseForFailure" to an object to pass to MissingMetadataException.
        /// </summary>
        internal virtual string? InternalGetNameIfAvailable(ref Type? rootCauseForFailure) => Name;

        /// <summary>
        /// Return Type.Name if sufficient metadata is available to do so - otherwise return a default (non-null) string.
        /// </summary>
        internal string NameOrDefault
        {
            get
            {
                return InternalNameIfAvailable ?? DefaultTypeNameWhenMissingMetadata;
            }
        }

        /// <summary>
        /// Return Type.FullName if sufficient metadata is available to do so - otherwise return a default (non-null) string.
        /// </summary>
        internal string FullNameOrDefault
        {
            get
            {
                // First, see if Type.Name is available. If Type.Name is available, then we can be reasonably confident that it is safe to call Type.FullName.
                // We'll still wrap the call in a try-catch as a failsafe.
                if (InternalNameIfAvailable == null)
                    return DefaultTypeNameWhenMissingMetadata;

                try
                {
                    return FullName;
                }
                catch (MissingMetadataException)
                {
                    return DefaultTypeNameWhenMissingMetadata;
                }
            }
        }

        //
        // This is a port of the desktop CLR's RuntimeType.FormatTypeName() routine. This routine is used by various Reflection ToString() methods
        // to display the name of a type. Do not use for any other purpose as it inherits some pretty quirky desktop behavior.
        //
        // The Project N version takes a raw metadata handle rather than a completed type so that it remains robust in the face of missing metadata.
        //
        internal string FormatTypeNameForReflection()
        {
            try
            {
                // Though we wrap this in a try-catch as a failsafe, this code must still strive to avoid triggering MissingMetadata exceptions
                // (non-error exceptions are very annoying when debugging.)

                // Legacy: this doesn't make sense, why use only Name for nested types but otherwise
                // ToString() which contains namespace.
                Type rootElementType = this;
                while (rootElementType.HasElementType)
                    rootElementType = rootElementType.GetElementType()!;
                if (rootElementType.IsNested)
                {
                    return InternalNameIfAvailable ?? DefaultTypeNameWhenMissingMetadata;
                }

                // Legacy: why removing "System"? Is it just because C# has keywords for these types?
                // If so why don't we change it to lower case to match the C# keyword casing?
                string typeName = ToString();
                if (typeName.StartsWith("System."))
                {
                    if (rootElementType.IsPrimitive || rootElementType == typeof(void))
                    {
                        typeName = typeName.Substring("System.".Length);
                    }
                }
                return typeName;
            }
            catch (Exception)
            {
                return DefaultTypeNameWhenMissingMetadata;
            }
        }

        internal const string DefaultTypeNameWhenMissingMetadata = "UnknownType";
    }
}
