// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Type with metadata available that is equivalent to a TypeDef record in an ECMA 335 metadata stream.
    /// A class, an interface, or a value type.
    /// </summary>
    public abstract partial class MetadataType : DefType
    {
        public abstract override string Name { get; }

        public abstract override string Namespace { get; }

        /// <summary>
        /// Gets metadata that controls instance layout of this type.
        /// </summary>
        public abstract ClassLayoutMetadata GetClassLayout();

        /// <summary>
        /// If true, the type layout is dictated by the explicit layout rules provided.
        /// Corresponds to the definition of explicitlayout semantic defined in the ECMA-335 specification.
        /// </summary>
        public abstract bool IsExplicitLayout { get; }

        /// <summary>
        /// If true, the order of the fields needs to be preserved. Corresponds to the definition
        /// of sequentiallayout semantic defined in the ECMA-335 specification.
        /// </summary>
        public abstract bool IsSequentialLayout { get; }

        /// <summary>
        /// If true, the type initializer of this type has a relaxed semantic. Corresponds
        /// to the definition of beforefieldinit semantic defined in the ECMA-335 specification.
        /// </summary>
        public abstract bool IsBeforeFieldInit { get; }

        /// <summary>
        /// If true, this is the special &lt;Module&gt; type that contains the definitions
        /// of global fields and methods in the module.
        /// </summary>
        public virtual bool IsModuleType
        {
            get
            {
                return Module.GetGlobalModuleType() == this;
            }
        }

        /// <summary>
        /// Gets the module that defines this type.
        /// </summary>
        public abstract ModuleDesc Module { get; }

        /// <summary>
        /// Same as <see cref="TypeDesc.BaseType"/>, but the result is a MetadataType (avoids casting).
        /// </summary>
        public abstract MetadataType MetadataBaseType { get; }

        // Make sure children remember to override both MetadataBaseType and BaseType.
        public abstract override DefType BaseType { get; }

        /// <summary>
        /// If true, the type cannot be used as a base type of any other type.
        /// </summary>
        public abstract bool IsSealed { get; }

        /// <summary>
        /// Gets a value indicating whether the type is abstract and cannot be allocated.
        /// </summary>
        public abstract bool IsAbstract { get; }

        /// <summary>
        /// Returns true if the type has given custom attribute.
        /// </summary>
        public abstract bool HasCustomAttribute(string attributeNamespace, string attributeName);

        public abstract override DefType ContainingType { get; }

        /// <summary>
        /// Get all of the types nested in this type.
        /// </summary>
        public abstract IEnumerable<MetadataType> GetNestedTypes();

        /// <summary>
        /// Get a specific type nested in this type. Returns null if the type
        /// doesn't exist.
        /// </summary>
        public abstract MetadataType GetNestedType(string name);

        /// <summary>
        /// Gets a value indicating whether this is an inline array type
        /// </summary>
        public bool IsInlineArray
        {
            get
            {
                return (GetTypeFlags(TypeFlags.IsInlineArray | TypeFlags.AttributeCacheComputed) & TypeFlags.IsInlineArray) != 0;
            }
        }

        public abstract int GetInlineArrayLength();
    }

    public struct ClassLayoutMetadata
    {
        public int PackingSize;
        public int Size;
        public FieldAndOffset[] Offsets;
    }

    public struct FieldAndOffset
    {
        public static readonly LayoutInt InvalidOffset = new LayoutInt(int.MaxValue);

        public readonly FieldDesc Field;

        public readonly LayoutInt Offset;

        public FieldAndOffset(FieldDesc field, LayoutInt offset)
        {
            Field = field;
            Offset = offset;
        }
    }
}
