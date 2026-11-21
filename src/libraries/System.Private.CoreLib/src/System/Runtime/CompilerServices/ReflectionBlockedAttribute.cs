// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that no reflection metadata should be generated for the type. The author of the type
    /// promises that nothing in the application needs to reflect over it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute is a contract of the type author. It is honored by NativeAOT and has no effect on other runtimes.
    /// </para>
    /// <para>
    /// For a blocked type the compiler generates no type definition, no members, no custom attributes and no stack trace
    /// names. Its code and its method table are not affected: instances can be created and cast, interfaces and static
    /// abstract members work, <see cref="object.GetType"/> returns a <see cref="Type"/> and type equality works.
    /// Members and custom attributes of the type read as empty, and the members of the type can't be obtained
    /// from a <see cref="RuntimeMethodHandle"/> or <see cref="RuntimeFieldHandle"/>. Other members that need the metadata of the type
    /// throw <see cref="NotSupportedException"/>. <see cref="Reflection.MemberInfo.Name"/>, <see cref="Type.FullName"/> and
    /// <see cref="Type.ToString"/> return a placeholder.
    /// Types nested in a blocked type are blocked too.
    /// </para>
    /// <para>
    /// Blocking a type does not block types that use it as a generic argument, as an array element type, or that derive from it.
    /// Those keep their metadata.
    /// </para>
    /// <para>
    /// The attribute is not supported (and is reported as a compiler error) on generic types, enums and delegates.
    /// Generic methods whose signature mentions a blocked type are not supported either, and fail the compilation if the runtime needs them.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ReflectionBlockedAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBlockedAttribute"/> class.
        /// </summary>
        public ReflectionBlockedAttribute()
        {
        }
    }
}
