// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Context.Custom;
using System.Reflection.Context.Projection;
using System.Reflection.Context.Virtual;

namespace System.Reflection.Context
{
    internal sealed class IdentityReflectionContext : ReflectionContext
    {
        public override Assembly MapAssembly(Assembly assembly) { return assembly; }
        public override TypeInfo MapType(TypeInfo type) { return type; }
    }

    /// <summary>
    /// Represents a customizable reflection context.
    /// </summary>
    /// <remarks>
    /// For more information about this API, see <see href="https://github.com/dotnet/docs/raw/main/docs/fundamentals/runtime-libraries/system-reflection-context-customreflectioncontext.md">CustomReflectionContext</see>.
    /// </remarks>
    public abstract partial class CustomReflectionContext : ReflectionContext
    {
        private readonly ReflectionContextProjector _projector;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomReflectionContext"/> class.
        /// </summary>
        protected CustomReflectionContext() : this(new IdentityReflectionContext()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomReflectionContext"/> class with the specified reflection context as a base.
        /// </summary>
        /// <param name="source">The reflection context to use as a base.</param>
        protected CustomReflectionContext(ReflectionContext source)
        {
            ArgumentNullException.ThrowIfNull(source);

            SourceContext = source;
            _projector = new ReflectionContextProjector(this);
        }

        /// <summary>
        /// Gets the representation, in this reflection context, of an assembly that is represented by an object from another reflection context.
        /// </summary>
        /// <param name="assembly">The external representation of the assembly to represent in this context.</param>
        /// <returns>The representation of the assembly in this reflection context.</returns>
        public override Assembly MapAssembly(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            return _projector.ProjectAssemblyIfNeeded(assembly);
        }

        /// <summary>
        /// Gets the representation, in this reflection context, of a type represented by an object from another reflection context.
        /// </summary>
        /// <param name="type">The external representation of the type to represent in this context.</param>
        /// <returns>The representation of the type in this reflection context.</returns>
        public override TypeInfo MapType(TypeInfo type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return _projector.ProjectTypeIfNeeded(type);
        }

        /// <summary>
        /// When overridden in a derived class, provides a list of custom attributes for the specified member, as represented in this reflection context.
        /// </summary>
        /// <param name="member">The member whose custom attributes will be returned.</param>
        /// <param name="declaredAttributes">A collection of the member's attributes in its current context.</param>
        /// <returns>A collection that represents the custom attributes of the specified member in this reflection context.</returns>
        protected virtual IEnumerable<object> GetCustomAttributes(MemberInfo member, IEnumerable<object> declaredAttributes)
        {
            return declaredAttributes;
        }

        /// <summary>
        /// When overridden in a derived class, provides a list of custom attributes for the specified parameter, as represented in this reflection context.
        /// </summary>
        /// <param name="parameter">The parameter whose custom attributes will be returned.</param>
        /// <param name="declaredAttributes">A collection of the parameter's attributes in its current context.</param>
        /// <returns>A collection that represents the custom attributes of the specified parameter in this reflection context.</returns>
        protected virtual IEnumerable<object> GetCustomAttributes(ParameterInfo parameter, IEnumerable<object> declaredAttributes)
        {
            return declaredAttributes;
        }

        // The default implementation of GetProperties: just return an empty list.
        /// <summary>
        /// When overridden in a derived class, provides a collection of additional properties for the specified type, as represented in this reflection context.
        /// </summary>
        /// <param name="type">The type to add properties to.</param>
        /// <returns>A collection of additional properties for the specified type.</returns>
        /// <remarks>
        /// Override this method to specify which properties should be added to a given type. To create the properties, use the <see cref="CreateProperty(Type, string, Func{object, object?}?, Action{object, object?}?)"/> method.
        /// </remarks>
        protected virtual IEnumerable<PropertyInfo> AddProperties(Type type)
        {
            // return an empty enumeration
            yield break;
        }

        /// <summary>
        /// Creates an object that represents a property to be added to a type, to be used with the <see cref="AddProperties(Type)"/> method.
        /// </summary>
        /// <param name="propertyType">The type of the property to create.</param>
        /// <param name="name">The name of the property to create.</param>
        /// <param name="getter">An object that represents the property's <see langword="get"/> accessor.</param>
        /// <param name="setter">An object that represents the property's <see langword="set"/> accessor.</param>
        /// <returns>An object that represents the property.</returns>
        /// <remarks>
        /// Objects that are returned by this method are not complete <see cref="PropertyInfo"/> objects, and should be used only in the context of the <see cref="AddProperties(Type)"/> method.
        /// </remarks>
        protected PropertyInfo CreateProperty(
            Type propertyType,
            string name,
            Func<object, object?>? getter,
            Action<object, object?>? setter)
        {
            return new VirtualPropertyInfo(
                name,
                propertyType,
                getter,
                setter,
                null,
                null,
                null,
                this);
        }

        /// <summary>
        /// Creates an object that represents a property to be added to a type, to be used with the <see cref="AddProperties(Type)"/> method and using the specified custom attributes.
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
        /// Objects that are returned by this method are not complete <see cref="PropertyInfo"/> objects, and should be used only in the context of the <see cref="AddProperties(Type)"/> method.
        /// </remarks>
        protected PropertyInfo CreateProperty(
            Type propertyType,
            string name,
            Func<object, object?>? getter,
            Action<object, object?>? setter,
            IEnumerable<Attribute>? propertyCustomAttributes,
            IEnumerable<Attribute>? getterCustomAttributes,
            IEnumerable<Attribute>? setterCustomAttributes)
        {
            return new VirtualPropertyInfo(
                name,
                propertyType,
                getter,
                setter,
                propertyCustomAttributes,
                getterCustomAttributes,
                setterCustomAttributes,
                this);
        }

        internal IEnumerable<PropertyInfo> GetNewPropertiesForType(CustomType type)
        {
            // We don't support adding properties on these types.
            if (type.IsInterface || type.IsGenericParameter || type.HasElementType)
                yield break;

            // Passing in the underlying type.
            IEnumerable<PropertyInfo> newProperties = AddProperties(type.UnderlyingType);

            // Setting DeclaringType on the user provided virtual properties.
            foreach (PropertyInfo prop in newProperties)
            {
                if (prop == null)
                    throw new InvalidOperationException(SR.InvalidOperation_AddNullProperty);

                VirtualPropertyBase? vp = prop as VirtualPropertyBase;
                if (vp == null || vp.ReflectionContext != this)
                    throw new InvalidOperationException(SR.InvalidOperation_AddPropertyDifferentContext);

                if (vp.DeclaringType == null)
                    vp.SetDeclaringType(type);
                else if (!vp.DeclaringType.Equals(type))
                    throw new InvalidOperationException(SR.InvalidOperation_AddPropertyDifferentType);

                yield return prop;
            }
        }

        internal IEnumerable<object> GetCustomAttributesOnMember(MemberInfo member, IEnumerable<object> declaredAttributes, Type attributeFilterType)
        {
            IEnumerable<object> attributes = GetCustomAttributes(member, declaredAttributes);
            return AttributeUtils.FilterCustomAttributes(attributes, attributeFilterType);
        }

        internal IEnumerable<object> GetCustomAttributesOnParameter(ParameterInfo parameter, IEnumerable<object> declaredAttributes, Type attributeFilterType)
        {
            IEnumerable<object> attributes = GetCustomAttributes(parameter, declaredAttributes);
            return AttributeUtils.FilterCustomAttributes(attributes, attributeFilterType);
        }

        internal Projector Projector
        {
            get
            {
                return _projector;
            }
        }

        internal ReflectionContext SourceContext { get; }
    }
}
