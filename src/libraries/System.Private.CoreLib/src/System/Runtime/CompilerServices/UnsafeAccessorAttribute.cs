// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Specifies the kind of target to which an <see cref="UnsafeAccessorAttribute" /> is providing access.
    /// </summary>
    public enum UnsafeAccessorKind
    {
        /// <summary>
        /// Provide access to a constructor.
        /// </summary>
        Constructor,

        /// <summary>
        /// Provide access to a method.
        /// </summary>
        Method,

        /// <summary>
        /// Provide access to a static method.
        /// </summary>
        StaticMethod,

        /// <summary>
        /// Provide access to a field.
        /// </summary>
        Field,

        /// <summary>
        /// Provide access to a static field.
        /// </summary>
        StaticField
    };

    /// <summary>
    /// Provides access to an inaccessible member of a specific type.
    /// </summary>
    /// <remarks>
    /// This attribute may be applied to an <code>extern static</code> method.
    /// The implementation of the <code>extern static</code> method annotated with
    /// this attribute will be provided by the runtime based on the information in
    /// the attribute and the signature of the method that the attribute is applied to.
    /// The runtime will try to find the matching method or field and forward the call
    /// to it. If the matching method or field is not found, the body of the <code>extern</code>
    /// method will throw <see cref="MissingFieldException" /> or <see cref="MissingMethodException" />.
    ///
    /// For <see cref="UnsafeAccessorKind.Method"/>, <see cref="UnsafeAccessorKind.StaticMethod"/>,
    /// <see cref="UnsafeAccessorKind.Field"/>, and <see cref="UnsafeAccessorKind.StaticField"/>, the type of
    /// the first argument of the annotated <code>extern</code> method identifies the owning type.
    /// The value of the first argument is treated as <code>this</code> pointer for instance fields and methods.
    /// The first argument must be passed as <code>ref</code> for instance fields and methods on structs.
    /// The value of the first argument is not used by the implementation for <code>static</code> fields and methods.
    ///
    /// Return type is considered for the signature match. modreqs and modopts are initially not considered for
    /// the signature match. However, if an ambiguity exists ignoring modreqs and modopts, a precise match
    /// is attempted. If an ambiguity still exists <see cref="System.Reflection.AmbiguousMatchException" /> is thrown.
    ///
    /// By default, the attributed method's name dictates the name of the method/field. This can cause confusion
    /// in some cases since language abstractions, like C# local functions, generate mangled IL names. The
    /// solution to this is to use the <code>nameof</code> mechanism and define the <see cref="Name"/> property.
    ///
    /// <code>
    /// public void Method(Class c)
    /// {
    ///     PrivateMethod(c);
    ///
    ///     [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(PrivateMethod))]
    ///     extern static void PrivateMethod(Class c);
    /// }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class UnsafeAccessorAttribute : Attribute
    {
        // Block of text to include above when Generics support is added:
        //
        // The generic parameters of the <code>extern static</code> method are a concatenation of the type and
        // method generic arguments of the target method. For example,
        // <code>extern static void Method1&lt;T1, T2&gt;(Class1&lt;T1&gt; @this)</code>
        // can be used to call <code>Class1&lt;T1&gt;.Method1&lt;T2&gt;()</code>. The generic constraints of the
        // <code>extern static</code> method must match generic constraints of the target type, field or method.

        /// <summary>
        /// Instantiates an <see cref="UnsafeAccessorAttribute"/> providing access to a member of kind <see cref="UnsafeAccessorKind"/>.
        /// </summary>
        /// <param name="kind">The kind of the target to which access is provided.</param>
        public UnsafeAccessorAttribute(UnsafeAccessorKind kind)
            => Kind = kind;

        /// <summary>
        /// Gets the kind of member to which access is provided.
        /// </summary>
        public UnsafeAccessorKind Kind { get; }

        /// <summary>
        /// Gets or sets the name of the member to which access is provided.
        /// </summary>
        /// <remarks>
        /// The name defaults to the annotated method name if not specified.
        /// The name must be unset/<code>null</code> for <see cref="UnsafeAccessorKind.Constructor"/>.
        /// </remarks>
        public string? Name { get; set; }
    }
}
