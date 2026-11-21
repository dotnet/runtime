// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Represents a metadata blocking policy. A metadata blocking policy decides what types or members are never
    /// eligible to have their metadata generated into the executable.
    /// </summary>
    public abstract class MetadataBlockingPolicy
    {
        /// <summary>
        /// Returns true if type definition '<paramref name="type"/>' is reflection blocked.
        /// </summary>
        public abstract bool IsBlocked(MetadataType type);

        /// <summary>
        /// Returns true if method definition '<paramref name="method"/>' is reflection blocked.
        /// </summary>
        public abstract bool IsBlocked(MethodDesc method);

        /// <summary>
        /// Returns true if field definition '<paramref name="field"/>' is reflection blocked.
        /// </summary>
        public abstract bool IsBlocked(FieldDesc field);

        /// <summary>
        /// Returns true if type definition '<paramref name="type"/>' was blocked on the request of its author ([ReflectionBlocked]).
        /// Such a type is blocked (<see cref="IsBlocked(MetadataType)"/> returns true), but unlike types that are blocked for other reasons,
        /// generic instantiations and arrays that merely mention it are not blocked.
        /// </summary>
        public virtual bool IsStripped(MetadataType type) => false;

        /// <summary>
        /// Returns true if type definition '<paramref name="type"/>' is blocked for a reason other than <see cref="IsStripped(MetadataType)"/>.
        /// </summary>
        public virtual bool IsHardBlocked(MetadataType type) => IsBlocked(type);
    }

    public class FullyBlockedMetadataBlockingPolicy : MetadataBlockingPolicy
    {
        public override bool IsBlocked(MetadataType type)
        {
            Debug.Assert(type.IsTypeDefinition);
            return true;
        }

        public override bool IsBlocked(MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            return true;
        }

        public override bool IsBlocked(FieldDesc field)
        {
            Debug.Assert(field.IsTypicalFieldDefinition);
            return true;
        }
    }
}
