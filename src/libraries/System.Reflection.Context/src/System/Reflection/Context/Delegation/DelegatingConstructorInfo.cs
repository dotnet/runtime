// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Reflection.Context.Delegation
{
    internal class DelegatingConstructorInfo : ConstructorInfo
    {
        public DelegatingConstructorInfo(ConstructorInfo constructor)
        {
            Debug.Assert(null != constructor);

            UnderlyingConstructor = constructor;
        }

        public override MethodAttributes Attributes
        {
            get { return UnderlyingConstructor.Attributes; }
        }

        public override CallingConventions CallingConvention
        {
            get { return UnderlyingConstructor.CallingConvention; }
        }

        public override bool ContainsGenericParameters
        {
            get { return UnderlyingConstructor.ContainsGenericParameters; }
        }

        public override Type? DeclaringType
        {
            get { return UnderlyingConstructor.DeclaringType; }
        }

        public override bool IsGenericMethod
        {
            get { return UnderlyingConstructor.IsGenericMethod; }
        }

        public override bool IsGenericMethodDefinition
        {
            get { return UnderlyingConstructor.IsGenericMethodDefinition; }
        }

        public override bool IsSecurityCritical
        {
            get { return UnderlyingConstructor.IsSecurityCritical; }
        }

        public override bool IsSecuritySafeCritical
        {
            get { return UnderlyingConstructor.IsSecuritySafeCritical; }
        }

        public override bool IsSecurityTransparent
        {
            get { return UnderlyingConstructor.IsSecurityTransparent; }
        }

        public override int MetadataToken
        {
            get { return UnderlyingConstructor.MetadataToken; }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { return UnderlyingConstructor.MethodHandle; }
        }

        public override Module Module
        {
            get { return UnderlyingConstructor.Module; }
        }

        public override string Name
        {
            get { return UnderlyingConstructor.Name; }
        }

        public override Type? ReflectedType
        {
            get { return UnderlyingConstructor.ReflectedType; }
        }

        public ConstructorInfo UnderlyingConstructor { get; }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return UnderlyingConstructor.GetCustomAttributes(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return UnderlyingConstructor.GetCustomAttributes(inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return UnderlyingConstructor.GetCustomAttributesData();
        }

        public override Type[] GetGenericArguments()
        {
            return UnderlyingConstructor.GetGenericArguments();
        }

        public override MethodBody? GetMethodBody()
        {
            return UnderlyingConstructor.GetMethodBody();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return UnderlyingConstructor.GetMethodImplementationFlags();
        }

        public override ParameterInfo[] GetParameters()
        {
            return UnderlyingConstructor.GetParameters();
        }

        public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            return UnderlyingConstructor.Invoke(invokeAttr, binder, parameters, culture);
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            return UnderlyingConstructor.Invoke(obj, invokeAttr, binder, parameters, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return UnderlyingConstructor.IsDefined(attributeType, inherit);
        }

        public override string? ToString()
        {
            return UnderlyingConstructor.ToString();
        }
    }
}
