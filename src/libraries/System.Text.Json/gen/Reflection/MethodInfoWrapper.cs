// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal class MethodInfoWrapper : MethodInfo
    {
        private readonly IMethodSymbol _method;
        private readonly MetadataLoadContextInternal _metadataLoadContext;

        public MethodInfoWrapper(IMethodSymbol method, MetadataLoadContextInternal metadataLoadContext)
        {
            _method = method;
            _metadataLoadContext = metadataLoadContext;
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

        private MethodAttributes? _attributes;

        public override MethodAttributes Attributes => _attributes ??= _method.GetMethodAttributes();

        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

        public override Type DeclaringType => _method.ContainingType.AsType(_metadataLoadContext);

        public override Type ReturnType => _method.ReturnType.AsType(_metadataLoadContext);

        public override string Name => _method.Name;

        public override bool IsGenericMethod => _method.IsGenericMethod;

        public bool IsInitOnly => _method.IsInitOnly;

        public override Type ReflectedType => throw new NotImplementedException();

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (AttributeData a in _method.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a, _metadataLoadContext));
            }
            return attributes;
        }

        public override MethodInfo GetBaseDefinition()
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override Type[] GetGenericArguments()
        {
            var typeArguments = new List<Type>();
            foreach (ITypeSymbol t in _method.TypeArguments)
            {
                typeArguments.Add(t.AsType(_metadataLoadContext));
            }
            return typeArguments.ToArray();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            var parameters = new List<ParameterInfo>();
            foreach (IParameterSymbol p in _method.Parameters)
            {
                parameters.Add(new ParameterInfoWrapper(p, _metadataLoadContext));
            }
            return parameters.ToArray();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
