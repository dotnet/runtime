// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices;

[AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct,
                AllowMultiple=false, Inherited=false)]
public class MetadataUpdateOriginalTypeAttribute : Attribute
{
    /// <summary> This attribute is emitted by Roslyn when a type that is marked with (or derives
    /// from a type that is marked with) <see
    /// cref="System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute" /> is updated
    /// during a hot reload session.  The <see cref="OriginalType" /> points to the original version
    /// of the updated type.  The next update of the type will have the same <see
    /// cref="OriginalType" />. Frameworks that provide support for hot reload by implementing a
    /// <see cref="System.Reflection.Metadata.MetadataUpdateHandlerAttribute" /> may use this
    /// attribute to relate an updated type to its original version.  </summary>
    ///
    /// <param name="originalType">The original type that was updated</param>
    public MetadataUpdateOriginalTypeAttribute(Type originalType) => OriginalType = originalType;
    /// Gets the original version of the type that this attribtue is attached to.
    public Type OriginalType { get; }
}
