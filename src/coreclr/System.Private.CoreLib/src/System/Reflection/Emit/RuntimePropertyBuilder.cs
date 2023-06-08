// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
**
** Propertybuilder is for client to define properties for a class
**
**
===========================================================*/

using System.Runtime.CompilerServices;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection.Emit
{
    //
    // A PropertyBuilder is always associated with a TypeBuilder.  The TypeBuilder.DefineProperty
    // method will return a new PropertyBuilder to a client.
    //
    internal sealed class RuntimePropertyBuilder : PropertyBuilder
    {
        // Constructs a PropertyBuilder.
        //
        internal RuntimePropertyBuilder(
            RuntimeModuleBuilder mod, // the module containing this PropertyBuilder
            string name, // property name
            PropertyAttributes attr, // property attribute such as DefaultProperty, Bindable, DisplayBind, etc
            Type returnType, // return type of the property.
            int prToken, // the metadata token for this property
            RuntimeTypeBuilder containingType) // the containing type
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            if (name[0] == '\0')
                throw new ArgumentException(SR.Argument_IllegalName, nameof(name));

            m_name = name;
            m_moduleBuilder = mod;
            m_attributes = attr;
            m_returnType = returnType;
            m_tkProperty = prToken;
            m_containingType = containingType;
        }

        /// <summary>
        /// Set the default value of the Property
        /// </summary>
        protected override void SetConstantCore(object? defaultValue)
        {
            m_containingType.ThrowIfCreated();

            RuntimeTypeBuilder.SetConstantValue(
                m_moduleBuilder,
                m_tkProperty,
                m_returnType,
                defaultValue);
        }


        // Return the Token for this property within the TypeBuilder that the
        // property is defined within.
        internal int PropertyToken => m_tkProperty;

        public override Module Module => m_containingType.Module;

        private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);

            m_containingType.ThrowIfCreated();
            RuntimeModuleBuilder module = m_moduleBuilder;
            RuntimeTypeBuilder.DefineMethodSemantics(
                new QCallModule(ref module),
                m_tkProperty,
                semantics,
                mdBuilder.MetadataToken);
        }

        protected override void SetGetMethodCore(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Getter);
            m_getMethod = mdBuilder;
        }

        protected override void SetSetMethodCore(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Setter);
            m_setMethod = mdBuilder;
        }

        protected override void AddOtherMethodCore(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
        }

        // Use this function if client decides to form the custom attribute blob themselves
        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            m_containingType.ThrowIfCreated();
            RuntimeTypeBuilder.DefineCustomAttribute(
                m_moduleBuilder,
                m_tkProperty,
                m_moduleBuilder.GetMethodMetadataToken(con),
                binaryAttribute);
        }

        // Not supported functions in dynamic module.
        public override object GetValue(object? obj, object?[]? index)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override object GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override void SetValue(object? obj, object? value, object?[]? index)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            if (nonPublic || m_getMethod == null)
                return m_getMethod;
            // now check to see if m_getMethod is public
            if ((m_getMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                return m_getMethod;
            return null;
        }

        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            if (nonPublic || m_setMethod == null)
                return m_setMethod;
            // now check to see if m_setMethod is public
            if ((m_setMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                return m_setMethod;
            return null;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override Type PropertyType => m_returnType;

        public override PropertyAttributes Attributes => m_attributes;

        public override bool CanRead
        {
            get { if (m_getMethod != null) return true; else return false; }
        }

        public override bool CanWrite
        {
            get { if (m_setMethod != null) return true; else return false; }
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override string Name => m_name;

        public override Type? DeclaringType => m_containingType;

        public override Type? ReflectedType => m_containingType;

        // These are package private so that TypeBuilder can access them.
        private readonly string m_name; // The name of the property
        private readonly int m_tkProperty; // The token of this property
        private readonly RuntimeModuleBuilder m_moduleBuilder;
        private readonly PropertyAttributes m_attributes; // property's attribute flags
        private readonly Type m_returnType; // property's return type
        private MethodInfo? m_getMethod;
        private MethodInfo? m_setMethod;
        private readonly RuntimeTypeBuilder m_containingType;
    }
}
