// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Context.Delegation
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given method
    internal class DelegatingMethodInfo : MethodInfo
    {
        public DelegatingMethodInfo(MethodInfo method)
        {
            Debug.Assert(null != method);

            UnderlyingMethod = method;
        }

        public override MethodAttributes Attributes
        {
            get { return UnderlyingMethod.Attributes; }
        }

        public override CallingConventions CallingConvention
        {
            get { return UnderlyingMethod.CallingConvention; }
        }

        public override bool ContainsGenericParameters
        {
            get { return UnderlyingMethod.ContainsGenericParameters; }
        }

        public override Type? DeclaringType
        {
            get { return UnderlyingMethod.DeclaringType; }
        }

        public override bool IsGenericMethod
        {
            get { return UnderlyingMethod.IsGenericMethod; }
        }

        public override bool IsGenericMethodDefinition
        {
            get { return UnderlyingMethod.IsGenericMethodDefinition; }
        }

        public override bool IsSecurityCritical
        {
            get { return UnderlyingMethod.IsSecurityCritical; }
        }

        public override bool IsSecuritySafeCritical
        {
            get { return UnderlyingMethod.IsSecuritySafeCritical; }
        }

        public override bool IsSecurityTransparent
        {
            get { return UnderlyingMethod.IsSecurityTransparent; }
        }

        public override int MetadataToken
        {
            get { return UnderlyingMethod.MetadataToken; }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { return UnderlyingMethod.MethodHandle; }
        }

        public override Module Module
        {
            get { return UnderlyingMethod.Module; }
        }

        public override string Name
        {
            get { return UnderlyingMethod.Name; }
        }

        public override Type? ReflectedType
        {
            get { return UnderlyingMethod.ReflectedType; }
        }

        public override ParameterInfo ReturnParameter
        {
            get { return UnderlyingMethod.ReturnParameter; }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { return UnderlyingMethod.ReturnTypeCustomAttributes; }
        }

        public override Type ReturnType
        {
            get { return UnderlyingMethod.ReturnType; }
        }

        public MethodInfo UnderlyingMethod { get; }

        public override MethodInfo GetBaseDefinition()
        {
            return UnderlyingMethod.GetBaseDefinition();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return UnderlyingMethod.GetCustomAttributes(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return UnderlyingMethod.GetCustomAttributes(inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return UnderlyingMethod.GetCustomAttributesData();
        }

        public override Type[] GetGenericArguments()
        {
            return UnderlyingMethod.GetGenericArguments();
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            return UnderlyingMethod.GetGenericMethodDefinition();
        }

        public override MethodBody? GetMethodBody()
        {
            return UnderlyingMethod.GetMethodBody();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return UnderlyingMethod.GetMethodImplementationFlags();
        }

        public override ParameterInfo[] GetParameters()
        {
            return UnderlyingMethod.GetParameters();
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            return UnderlyingMethod.Invoke(obj, invokeAttr, binder, parameters, culture);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return UnderlyingMethod.IsDefined(attributeType, inherit);
        }

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            return UnderlyingMethod.MakeGenericMethod(typeArguments);
        }

        public override Delegate CreateDelegate(Type delegateType)
        {
            return UnderlyingMethod.CreateDelegate(delegateType);
        }

        public override Delegate CreateDelegate(Type delegateType, object? target)
        {
            return UnderlyingMethod.CreateDelegate(delegateType, target);
        }

        public override string? ToString()
        {
            return UnderlyingMethod.ToString();
        }
    }
}
