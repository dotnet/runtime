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
        protected virtual System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> AddProperties(System.Type type) { throw null; }
        /// <summary>
        /// Creates an object that represents a property to be added to a type, to be used with the <see cref="AddProperties(System.Type)"/> method.
        /// </summary>
        /// <param name="propertyType">The type of the property to create.</param>
        /// <param name="name">The name of the property to create.</param>
        /// <param name="getter">An object that represents the property's <see langword="get"/> accessor.</param>
        /// <param name="setter">An object that represents the property's <see langword="set"/> accessor.</param>
        /// <returns>An object that represents the property.</returns>
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
