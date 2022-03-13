// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal class ConstructorInfoWrapper : ConstructorInfo
    {
        private readonly IMethodSymbol _ctor;
        private readonly MetadataLoadContextInternal _metadataLoadContext;

        public ConstructorInfoWrapper(IMethodSymbol ctor, MetadataLoadContextInternal metadataLoadContext)
        {
            _ctor = ctor;
            _metadataLoadContext = metadataLoadContext;
        }

        public override Type DeclaringType => _ctor.ContainingType.AsType(_metadataLoadContext);

        private MethodAttributes? _attributes;

        public override MethodAttributes Attributes => _attributes ??= _ctor.GetMethodAttributes();

        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

        public override string Name => _ctor.Name;

        public override Type ReflectedType => throw new NotImplementedException();

        public override bool IsGenericMethod => _ctor.IsGenericMethod;

        public override Type[] GetGenericArguments()
        {
            var typeArguments = new List<Type>();
            foreach (ITypeSymbol t in _ctor.TypeArguments)
            {
                typeArguments.Add(t.AsType(_metadataLoadContext));
            }
            return typeArguments.ToArray();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (AttributeData a in _ctor.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a, _metadataLoadContext));
            }
            return attributes;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            var parameters = new List<ParameterInfo>();
            foreach (IParameterSymbol p in _ctor.Parameters)
            {
                parameters.Add(new ParameterInfoWrapper(p, _metadataLoadContext));
            }
            return parameters.ToArray();
        }

        public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException();
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
