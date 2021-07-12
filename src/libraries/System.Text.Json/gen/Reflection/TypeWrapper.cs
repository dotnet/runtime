﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal class TypeWrapper : Type
    {
        private readonly ITypeSymbol _typeSymbol;

        private readonly MetadataLoadContextInternal _metadataLoadContext;

        private INamedTypeSymbol? _namedTypeSymbol;

        private IArrayTypeSymbol? _arrayTypeSymbol;

        private Type _elementType;

        public TypeWrapper(ITypeSymbol namedTypeSymbol, MetadataLoadContextInternal metadataLoadContext)
        {
            _typeSymbol = namedTypeSymbol;
            _metadataLoadContext = metadataLoadContext;
            _namedTypeSymbol = _typeSymbol as INamedTypeSymbol;
            _arrayTypeSymbol = _typeSymbol as IArrayTypeSymbol;
        }

        public override Assembly Assembly => new AssemblyWrapper(_typeSymbol.ContainingAssembly, _metadataLoadContext);

        private string? _assemblyQualifiedName;

        public override string AssemblyQualifiedName
        {
            get
            {
                if (_assemblyQualifiedName == null)
                {
                    StringBuilder sb = new();

                    AssemblyIdentity identity = _typeSymbol.ContainingAssembly.Identity;

                    sb.Append(FullName);

                    sb.Append(", ");
                    sb.Append(identity.Name);

                    sb.Append(", Version=");
                    sb.Append(identity.Version);

                    if (string.IsNullOrWhiteSpace(identity.CultureName))
                    {
                        sb.Append(", Culture=neutral");
                    }

                    sb.Append(", PublicKeyToken=");
                    ImmutableArray<byte> publicKeyToken = identity.PublicKeyToken;
                    if (publicKeyToken.Length > 0)
                    {
                        foreach (byte b in publicKeyToken)
                        {
                            sb.Append(b.ToString("x2"));
                        }
                    }
                    else
                    {
                        sb.Append("null");
                    }

                    _assemblyQualifiedName = sb.ToString();
                }

                return _assemblyQualifiedName;
            }
        }

        public override Type BaseType => _typeSymbol.BaseType!.AsType(_metadataLoadContext);

        private string? _fullName;

        public override string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    StringBuilder sb = new();

                    if (this.IsNullableValueType(out Type? underlyingType))
                    {
                        sb.Append("System.Nullable`1[[");
                        sb.Append(underlyingType.AssemblyQualifiedName);
                        sb.Append("]]");
                    }
                    else
                    {
                        sb.Append(Name);

                        for (ISymbol currentSymbol = _typeSymbol.ContainingSymbol; currentSymbol != null && currentSymbol.Kind != SymbolKind.Namespace; currentSymbol = currentSymbol.ContainingSymbol)
                        {
                            sb.Insert(0, $"{currentSymbol.Name}+");
                        }

                        if (!string.IsNullOrWhiteSpace(Namespace))
                        {
                            sb.Insert(0, $"{Namespace}.");
                        }
                    }

                    _fullName = sb.ToString();
                }

                return _fullName;
            }
        }

        public override Guid GUID => Guid.Empty;

        public override Module Module => throw new NotImplementedException();

        public override string Namespace =>
            IsArray ?
            GetElementType().Namespace :
            _typeSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining))!;

        public override Type UnderlyingSystemType => this;

        public override string Name
        {
            get
            {
                if (_arrayTypeSymbol == null)
                {
                    return _typeSymbol.MetadataName;
                }

                Type elementType = GetElementType();
                return elementType.Name + "[]";
            }
        }

        private Type _enumType;

        public override bool IsEnum
        {
            get
            {
                _enumType ??= _metadataLoadContext.Resolve(typeof(Enum));
                return IsSubclassOf(_enumType);
            }
        }

        public override bool IsGenericType => _namedTypeSymbol?.IsGenericType == true;

        public override bool ContainsGenericParameters => _namedTypeSymbol?.IsUnboundGenericType == true;

        public override bool IsGenericTypeDefinition => base.IsGenericTypeDefinition;

        public INamespaceSymbol GetNamespaceSymbol => _typeSymbol.ContainingNamespace;

        public override Type[] GetGenericArguments()
        {
            var args = new List<Type>();
            foreach (ITypeSymbol item in _namedTypeSymbol.TypeArguments)
            {
                args.Add(item.AsType(_metadataLoadContext));
            }
            return args.ToArray();
        }

        public override Type GetGenericTypeDefinition()
        {
            return _namedTypeSymbol.ConstructedFrom.AsType(_metadataLoadContext);
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
            foreach (IMethodSymbol c in _namedTypeSymbol.Constructors)
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
            _elementType ??= _arrayTypeSymbol?.ElementType.AsType(_metadataLoadContext)!;
            return _elementType;
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
            List<FieldInfo> fields = new();

            foreach (ISymbol item in _typeSymbol.GetMembers())
            {
                if (item is IFieldSymbol fieldSymbol)
                {
                    // Skip if:
                    if (
                        // this is a backing field
                        fieldSymbol.AssociatedSymbol != null ||
                        // we want a static field and this is not static
                        (BindingFlags.Static & bindingAttr) != 0 && !fieldSymbol.IsStatic ||
                        // we want an instance field and this is static or a constant
                        (BindingFlags.Instance & bindingAttr) != 0 && (fieldSymbol.IsStatic || fieldSymbol.IsConst))
                    {
                        continue;
                    }

                    if ((BindingFlags.Public & bindingAttr) != 0 && item.DeclaredAccessibility == Accessibility.Public ||
                        (BindingFlags.NonPublic & bindingAttr) != 0)
                    {
                        fields.Add(new FieldInfoWrapper(fieldSymbol, _metadataLoadContext));
                    }
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
            var members = new List<MemberInfo>();
            foreach (ISymbol m in _typeSymbol.GetMembers())
            {
                members.Add(new MemberInfoWrapper(m, _metadataLoadContext));
            }
            return members.ToArray();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            var methods = new List<MethodInfo>();
            foreach (ISymbol m in _typeSymbol.GetMembers())
            {
                // TODO: Efficiency
                if (m is IMethodSymbol method && !_namedTypeSymbol.Constructors.Contains(method))
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
            List<PropertyInfo> properties = new();

            foreach (ISymbol item in _typeSymbol.GetMembers())
            {
                if (item is IPropertySymbol propertySymbol)
                {
                    // Skip if:
                    if (
                        // we want a static property and this is not static
                        (BindingFlags.Static & bindingAttr) != 0 && !propertySymbol.IsStatic ||
                        // we want an instance property and this is static
                        (BindingFlags.Instance & bindingAttr) != 0 && propertySymbol.IsStatic)
                    {
                        continue;
                    }

                    if ((BindingFlags.Public & bindingAttr) != 0 && item.DeclaredAccessibility == Accessibility.Public ||
                        (BindingFlags.NonPublic & bindingAttr) != 0)
                    {
                        properties.Add(new PropertyInfoWrapper(propertySymbol, _metadataLoadContext));
                    }
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

        private TypeAttributes? _typeAttributes;

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            if (!_typeAttributes.HasValue)
            {
                _typeAttributes = default(TypeAttributes);

                if (_typeSymbol.IsAbstract)
                {
                    _typeAttributes |= TypeAttributes.Abstract;
                }

                if (_typeSymbol.TypeKind == TypeKind.Interface)
                {
                    _typeAttributes |= TypeAttributes.Interface;
                }

                if (_typeSymbol.ContainingType != null && _typeSymbol.DeclaredAccessibility == Accessibility.Private)
                {
                    _typeAttributes |= TypeAttributes.NestedPrivate;
                }
            }

            return _typeAttributes.Value;
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            foreach (ConstructorInfo constructor in GetConstructors(bindingAttr))
            {
                ParameterInfo[] parameters = constructor.GetParameters();

                if (parameters.Length == types.Length)
                {
                    bool mismatched = false;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType != types[i])
                        {
                            mismatched = true;
                            break;
                        }
                    }

                    if (!mismatched)
                    {
                        return constructor;
                    }
                }
            }

            return null;
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            // TODO: peformance; caching; honor bindingAttr
            foreach (PropertyInfo propertyInfo in GetProperties(bindingAttr))
            {
                if (propertyInfo.Name == name)
                {
                    return propertyInfo;
                }
            }

            return null!;
        }

        protected override bool HasElementTypeImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsArrayImpl()
        {
            return _arrayTypeSymbol != null;
        }

        private Type _valueType;

        protected override bool IsValueTypeImpl()
        {
            _valueType ??= _metadataLoadContext.Resolve(typeof(ValueType));
            return IsSubclassOf(_valueType);
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
                return tr._typeSymbol.AllInterfaces.Contains(_typeSymbol, SymbolEqualityComparer.Default) ||
                    (tr._namedTypeSymbol != null && tr._namedTypeSymbol.BaseTypes().Contains(_typeSymbol, SymbolEqualityComparer.Default));
            }
            else if (_metadataLoadContext.Resolve(c) is TypeWrapper trr)
            {
                return trr._typeSymbol.AllInterfaces.Contains(_typeSymbol, SymbolEqualityComparer.Default) ||
                    (trr._namedTypeSymbol != null && trr._namedTypeSymbol.BaseTypes().Contains(_typeSymbol, SymbolEqualityComparer.Default));
            }
            return false;
        }

#pragma warning disable RS1024 // Compare symbols correctly
        public override int GetHashCode() => _typeSymbol.GetHashCode();
#pragma warning restore RS1024 // Compare symbols correctly

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
