// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection.Context
{
    /// <summary>
    /// Represents a customizable reflection context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CustomReflectionContext"/> provides a way for you to add or remove custom attributes from reflection objects, or add dummy properties to those objects, without re-implementing the complete reflection model. The default <see cref="CustomReflectionContext"/> simply wraps reflection objects without making any changes, but by subclassing and overriding the relevant methods, you can add, remove, or change the attributes that apply to any reflected parameter or member, or add new properties to a reflected type.
    /// </para>
    /// <para>
    /// For example, suppose that your code follows the convention of applying a particular attribute to factory methods, but you are now required to work with third-party code that lacks attributes. You can use <see cref="CustomReflectionContext"/> to specify a rule for identifying the objects that should have attributes and to supply the objects with those attributes when they are viewed from your code.
    /// </para>
    /// <para>
    /// To use <see cref="CustomReflectionContext"/> effectively, the code that uses the reflected objects must support the notion of specifying a reflection context, instead of assuming that all reflected objects are associated with the runtime reflection context. Many reflection methods in .NET provide a <see cref="System.Reflection.ReflectionContext"/> parameter for this purpose.
    /// </para>
    /// <para>
    /// To modify the attributes that are applied to a reflected parameter or member, override the <see cref="GetCustomAttributes(System.Reflection.ParameterInfo, System.Collections.Generic.IEnumerable{object})"/> or <see cref="GetCustomAttributes(System.Reflection.MemberInfo, System.Collections.Generic.IEnumerable{object})"/> method. These methods take the reflected object and the list of attributes under its current reflection context, and return the list of attributes it should have under the custom reflection context.
    /// </para>
    /// <note type="warning">
    /// <see cref="CustomReflectionContext"/> methods should not access the list of attributes of a reflected object or method directly by calling the <see cref="System.Reflection.MemberInfo.GetCustomAttributes(bool)"/> method on the provided <see cref="System.Reflection.MemberInfo"/> or <see cref="System.Reflection.ParameterInfo"/> instance, but should instead use the <c>declaredAttributes</c> list, which is passed as a parameter to the <see cref="GetCustomAttributes(System.Reflection.MemberInfo, System.Collections.Generic.IEnumerable{object})"/> method overloads.
    /// </note>
    /// <para>
    /// To add properties to a reflected type, override the <see cref="AddProperties(System.Type)"/> method. The method accepts a parameter that specifies the reflected type, and returns a list of additional properties. You should use the <see cref="CreateProperty(System.Type, string, System.Func{object, object?}?, System.Action{object, object?}?)"/> method to create property objects to return. You can specify delegates when creating the property that will serve as the property accessor, and you can omit one of the accessors to create a read-only or write-only property. Note that such dummy properties have no metadata or Common Intermediate Language (CIL) backing.
    /// </para>
    /// <note type="warning">
    /// Be cautious about equality among reflected objects when you work with reflection contexts, because objects may represent the same reflected object in multiple contexts. You can use the <see cref="MapType(System.Reflection.TypeInfo)"/> method to obtain a particular reflection context's version of a reflected object.
    /// </note>
    /// <note type="warning">
    /// A <see cref="CustomReflectionContext"/> object alters the attributes returned by a particular reflection object, such as those obtained by the <see cref="System.Reflection.MemberInfo.GetCustomAttributes(bool)"/> method. It does not alter the custom attribute data returned by the <see cref="System.Reflection.MemberInfo.GetCustomAttributesData()"/> method, and these two lists will not match when you use a custom reflection context.
    /// </note>
    /// For more information, see https://github.com/dotnet/docs/raw/main/docs/fundamentals/runtime-libraries/system-reflection-context-customreflectioncontext.md.
    /// </remarks>
    public abstract partial class CustomReflectionContext : System.Reflection.ReflectionContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomReflectionContext"/> class.
        /// </summary>
        protected CustomReflectionContext() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomReflectionContext"/> class with the specified reflection context as a base.
        /// </summary>
        /// <param name="source">The reflection context to use as a base.</param>
        protected CustomReflectionContext(System.Reflection.ReflectionContext source) { }
        /// <summary>
        /// When overridden in a derived class, provides a collection of additional properties for the specified type, as represented in this reflection context.
        /// </summary>
        /// <param name="type">The type to add properties to.</param>
        /// <returns>A collection of additional properties for the specified type.</returns>
        /// <remarks>
        /// Override this method to specify which properties should be added to a given type. To create the properties, use the <see cref="CreateProperty(System.Type, string, System.Func{object, object?}?, System.Action{object, object?}?)"/> method.
        /// </remarks>
        protected virtual System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> AddProperties(System.Type type) { throw null; }
        /// <summary>
        /// Creates an object that represents a property to be added to a type, to be used with the <see cref="AddProperties(System.Type)"/> method.
        /// </summary>
        /// <param name="propertyType">The type of the property to create.</param>
        /// <param name="name">The name of the property to create.</param>
        /// <param name="getter">An object that represents the property's <see langword="get"/> accessor.</param>
        /// <param name="setter">An object that represents the property's <see langword="set"/> accessor.</param>
        /// <returns>An object that represents the property.</returns>
        /// <remarks>
        /// Objects that are returned by this method are not complete <see cref="System.Reflection.PropertyInfo"/> objects, and should be used only in the context of the <see cref="AddProperties(System.Type)"/> method.
        /// </remarks>
        protected System.Reflection.PropertyInfo CreateProperty(System.Type propertyType, string name, System.Func<object, object?>? getter, System.Action<object, object?>? setter) { throw null; }
        /// <summary>
        /// Creates an object that represents a property to be added to a type, to be used with the <see cref="AddProperties(System.Type)"/> method and using the specified custom attributes.
        /// </summary>
        /// <param name="propertyType">The type of the property to create.</param>
        /// <param name="name">The name of the property to create.</param>
        /// <param name="getter">An object that represents the property's <see langword="get"/> accessor.</param>
        /// <param name="setter">An object that represents the property's <see langword="set"/> accessor.</param>
        /// <param name="propertyCustomAttributes">A collection of custom attributes to apply to the property.</param>
        /// <param name="getterCustomAttributes">A collection of custom attributes to apply to the property's <see langword="get"/> accessor.</param>
        /// <param name="setterCustomAttributes">A collection of custom attributes to apply to the property's <see langword="set"/> accessor.</param>
        /// <returns>An object that represents the property.</returns>
        /// <remarks>
        /// Objects that are returned by this method are not complete <see cref="System.Reflection.PropertyInfo"/> objects, and should be used only in the context of the <see cref="AddProperties(System.Type)"/> method.
        /// </remarks>
        protected System.Reflection.PropertyInfo CreateProperty(System.Type propertyType, string name, System.Func<object, object?>? getter, System.Action<object, object?>? setter, System.Collections.Generic.IEnumerable<System.Attribute>? propertyCustomAttributes, System.Collections.Generic.IEnumerable<System.Attribute>? getterCustomAttributes, System.Collections.Generic.IEnumerable<System.Attribute>? setterCustomAttributes) { throw null; }
        /// <summary>
        /// When overridden in a derived class, provides a list of custom attributes for the specified member, as represented in this reflection context.
        /// </summary>
        /// <param name="member">The member whose custom attributes will be returned.</param>
        /// <param name="declaredAttributes">A collection of the member's attributes in its current context.</param>
        /// <returns>A collection that represents the custom attributes of the specified member in this reflection context.</returns>
        protected virtual System.Collections.Generic.IEnumerable<object> GetCustomAttributes(System.Reflection.MemberInfo member, System.Collections.Generic.IEnumerable<object> declaredAttributes) { throw null; }
        /// <summary>
        /// When overridden in a derived class, provides a list of custom attributes for the specified parameter, as represented in this reflection context.
        /// </summary>
        /// <param name="parameter">The parameter whose custom attributes will be returned.</param>
        /// <param name="declaredAttributes">A collection of the parameter's attributes in its current context.</param>
        /// <returns>A collection that represents the custom attributes of the specified parameter in this reflection context.</returns>
        protected virtual System.Collections.Generic.IEnumerable<object> GetCustomAttributes(System.Reflection.ParameterInfo parameter, System.Collections.Generic.IEnumerable<object> declaredAttributes) { throw null; }
        /// <summary>
        /// Gets the representation, in this reflection context, of an assembly that is represented by an object from another reflection context.
        /// </summary>
        /// <param name="assembly">The external representation of the assembly to represent in this context.</param>
        /// <returns>The representation of the assembly in this reflection context.</returns>
        public override System.Reflection.Assembly MapAssembly(System.Reflection.Assembly assembly) { throw null; }
        /// <summary>
        /// Gets the representation, in this reflection context, of a type represented by an object from another reflection context.
        /// </summary>
        /// <param name="type">The external representation of the type to represent in this context.</param>
        /// <returns>The representation of the type in this reflection context.</returns>
        public override System.Reflection.TypeInfo MapType(System.Reflection.TypeInfo type) { throw null; }
    }
}
