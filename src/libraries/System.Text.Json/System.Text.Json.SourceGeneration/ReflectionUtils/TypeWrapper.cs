// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    internal class TypeWrapper : Type
    {
        private readonly ITypeSymbol _typeSymbol;
        private readonly MetadataLoadContext _metadataLoadContext;

        public TypeWrapper(ITypeSymbol namedTypeSymbol, MetadataLoadContext metadataLoadContext)
        {
            _typeSymbol = namedTypeSymbol;
            _metadataLoadContext = metadataLoadContext;
        }

        public override Assembly Assembly => new AssemblyWrapper(_typeSymbol.ContainingAssembly, _metadataLoadContext);

        public override string AssemblyQualifiedName => throw new NotImplementedException();

        public override Type BaseType => _typeSymbol.BaseType!.AsType(_metadataLoadContext);

        public override string FullName => Namespace == null ? Name : Namespace + "." + Name;

        public override Guid GUID => Guid.Empty;

        public override Module Module => throw new NotImplementedException();

        public override string Namespace => _typeSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining))!;

        public override Type UnderlyingSystemType => this;

        public override string Name => _typeSymbol.MetadataName;

        public override bool IsGenericType => NamedTypeSymbol?.IsGenericType == true;

        private INamedTypeSymbol NamedTypeSymbol => (_typeSymbol as INamedTypeSymbol)!;

        private IArrayTypeSymbol ArrayTypeSymbol => (_typeSymbol as IArrayTypeSymbol)!;

        public override bool IsGenericTypeDefinition => base.IsGenericTypeDefinition;

        public override Type[] GetGenericArguments()
        {
            var args = new List<Type>();
            foreach (ITypeSymbol item in NamedTypeSymbol.TypeArguments)
            {
                args.Add(item.AsType(_metadataLoadContext));
            }
            return args.ToArray();
        }

        public override Type GetGenericTypeDefinition()
        {
            return NamedTypeSymbol.ConstructedFrom.AsType(_metadataLoadContext);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (AttributeData a in _typeSymbol.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a, _metadataLoadContext));
            }
            return attributes;
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            var ctors = new List<ConstructorInfo>();
            foreach (IMethodSymbol c in NamedTypeSymbol.Constructors)
            {
                ctors.Add(new ConstructorInfoWrapper(c, _metadataLoadContext));
            }
            return ctors.ToArray();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override Type GetElementType()
        {
            return ArrayTypeSymbol?.ElementType.AsType(_metadataLoadContext)!;
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            var fields = new List<FieldInfo>();
            foreach (ISymbol item in _typeSymbol.GetMembers())
            {
                // Associated Symbol checks the field is not a backingfield.
                if (item is IFieldSymbol field && field.AssociatedSymbol == null)
                {
                    fields.Add(new FieldInfoWrapper(field, _metadataLoadContext));
                }
            }
            return fields.ToArray();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            var interfaces = new List<Type>();
            foreach (INamedTypeSymbol i in _typeSymbol.Interfaces)
            {
                interfaces.Add(i.AsType(_metadataLoadContext));
            }
            return interfaces.ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            var methods = new List<MethodInfo>();
            foreach (ISymbol m in _typeSymbol.GetMembers())
            {
                // TODO: Efficiency
                if (m is IMethodSymbol method && !NamedTypeSymbol.Constructors.Contains(method))
                {
                    methods.Add(method.AsMethodInfo(_metadataLoadContext));
                }
            }
            return methods.ToArray();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            var nestedTypes = new List<Type>();
            foreach (INamedTypeSymbol type in _typeSymbol.GetTypeMembers())
            {
                nestedTypes.Add(type.AsType(_metadataLoadContext));
            }
            return nestedTypes.ToArray();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            var properties = new List<PropertyInfo>();
            foreach (ISymbol item in _typeSymbol.GetMembers())
            {
                if (item is IPropertySymbol property)
                {
                    properties.Add(new PropertyWrapper(property, _metadataLoadContext));
                }
            }
            return properties.ToArray();
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            throw new NotImplementedException();
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override bool HasElementTypeImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsArrayImpl()
        {
            return ArrayTypeSymbol != null;
        }

        protected override bool IsByRefImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsCOMObjectImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsPointerImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsPrimitiveImpl()
        {
            throw new NotImplementedException();
        }

        public override bool IsAssignableFrom(Type c)
        {
            if (c is TypeWrapper tr)
            {
                return tr._typeSymbol.AllInterfaces.Contains(_typeSymbol) || (tr.NamedTypeSymbol != null && tr.NamedTypeSymbol.BaseTypes().Contains(_typeSymbol));
            }
            else if (_metadataLoadContext.Resolve(c) is TypeWrapper trr)
            {
                return trr._typeSymbol.AllInterfaces.Contains(_typeSymbol) || (trr.NamedTypeSymbol != null && trr.NamedTypeSymbol.BaseTypes().Contains(_typeSymbol));
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _typeSymbol.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (o is TypeWrapper tw)
            {
                return _typeSymbol.Equals(tw._typeSymbol, SymbolEqualityComparer.Default);
            }
            else if (o is Type t && _metadataLoadContext.Resolve(t) is TypeWrapper tww)
            {
                return _typeSymbol.Equals(tww._typeSymbol, SymbolEqualityComparer.Default);
            }

            return base.Equals(o);
        }

        public override bool Equals(Type o)
        {
            if (o is TypeWrapper tw)
            {
                return _typeSymbol.Equals(tw._typeSymbol, SymbolEqualityComparer.Default);
            }
            else if (_metadataLoadContext.Resolve(o) is TypeWrapper tww)
            {
                return _typeSymbol.Equals(tww._typeSymbol, SymbolEqualityComparer.Default);
            }
            return base.Equals(o);
        }
    }
}
