// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that the types of the assembly that are not externally visible, and the members of its types that are not public, are not available to reflection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A type is externally visible if it is public and all the types that contain it are public. Reflection does not provide
    /// the members, the nested types, the custom attributes or the name of a type that is not externally visible.
    /// These types still work normally: instances can be created and cast, and <see cref="object.GetType" />,
    /// <see langword="typeof" /> and type equality are not affected. The types that the compiler generates inside
    /// a public type (closures, state machines) are not externally visible.
    /// </para>
    /// <para>
    /// Only the public members of a type that is externally visible are available to reflection: the members that are private, internal,
    /// protected, protected internal or private protected are not. This includes the constructors, the explicit interface implementations,
    /// the backing fields of properties and the accessors of a property or event that are not public (a property or event is available
    /// if one of its accessors is). Handles to a member that is not available (<see cref="System.Reflection.MethodBase.GetMethodFromHandle(RuntimeMethodHandle)" />) and
    /// delegates to it (<see cref="Delegate.Method" />) fail like the members of a type that is not available, and the members can't be
    /// found by name. The members still work for the program, and also for <see cref="UnsafeAccessorAttribute" />.
    /// </para>
    /// <para>
    /// Top-level types that derive from <see cref="Attribute" />, enums, delegates, generic types (except the ones nested in a type
    /// that is not available), and types that declare generic virtual methods are available to reflection, and so are
    /// the types that contain an enum, a delegate or a type that declares a generic virtual method as a nested type.
    /// The members of the enums and the delegates are available to reflection (the runtime needs them), whatever their accessibility. The attribute classes
    /// keep their constructors, fields and properties (attributes are instantiated through them) and their public members.
    /// </para>
    /// <para>
    /// The attribute does not affect diagnostics: stack traces, the debugger and the other diagnostic features show the names of the
    /// types and methods that are not available to reflection, like in an assembly without the attribute (<see cref="System.Diagnostics.StackFrame.GetMethod" /> and
    /// <see cref="Exception.TargetSite" /> are <see langword="null" /> for a method that is not available).
    /// </para>
    /// <para>
    /// Use this attribute when nothing needs to reflect over the implementation details of an assembly, so that a compiler that
    /// compiles an application ahead of time can remove the metadata of these types and members. The attribute has no effect on Mono.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class DisablePrivateReflectionAttribute : Attribute
    {
        public DisablePrivateReflectionAttribute() { }
    }
}
