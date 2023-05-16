// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal sealed class TypeWrapper : Type
    {
        private readonly ITypeSymbol _typeSymbol;

        private readonly MetadataLoadContextInternal _metadataLoadContext;

        private readonly INamedTypeSymbol? _namedTypeSymbol;

        private readonly IArrayTypeSymbol? _arrayTypeSymbol;

        private Type? _elementType;

        public TypeWrapper(ITypeSymbol namedTypeSymbol, MetadataLoadContextInternal metadataLoadContext)
        {
            _typeSymbol = namedTypeSymbol;
            _metadataLoadContext = metadataLoadContext;
            _namedTypeSymbol = _typeSymbol as INamedTypeSymbol;
            _arrayTypeSymbol = _typeSymbol as IArrayTypeSymbol;
        }

        public ITypeSymbol Symbol => _typeSymbol;

        public override Assembly Assembly => new AssemblyWrapper(_typeSymbol.ContainingAssembly, _metadataLoadContext);

        private string? _assemblyQualifiedName;

        public override string? AssemblyQualifiedName
        {
            get
            {
                if (_assemblyQualifiedName == null && !IsGenericParameter)
                {
                    StringBuilder sb = new();

                    AssemblyIdentity identity;

                    if (_arrayTypeSymbol == null)
                    {
                        identity = _typeSymbol.ContainingAssembly.Identity;
                        sb.Append(FullName);
                    }
                    else
                    {
                        TypeWrapper currentType = this;
                        int nestCount = 1;

                        while (true)
                        {
                            currentType = (TypeWrapper)currentType.GetElementType();

                            if (!currentType.IsArray)
                            {
                                break;
                            }

                            nestCount++;
                        }

                        identity = currentType._typeSymbol.ContainingAssembly.Identity;
                        sb.Append(currentType.FullName);

                        for (int i = 0; i < nestCount; i++)
                        {
                            sb.Append("[]");
                        }
                    }

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

        public override Type? BaseType => _typeSymbol.BaseType.AsType(_metadataLoadContext);

        public override Type? DeclaringType => _typeSymbol.ContainingType?.ConstructedFrom.AsType(_metadataLoadContext);

        private string? _fullName;

        public override string? FullName
        {
            get
            {
                if (_fullName == null && !IsGenericParameter)
                {
                    StringBuilder sb = new();

                    if (this.IsNullableValueType(out Type? underlyingType))
                    {
                        sb.Append("System.Nullable`1[[");
                        sb.Append(underlyingType.AssemblyQualifiedName);
                        sb.Append("]]");
                    }
                    else if (IsArray)
                    {
                        int rank = GetArrayRank();
                        sb.Append(GetElementType().FullName + FormatArrayTypeNameSuffix(rank));
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(Namespace) && Namespace != JsonConstants.GlobalNamespaceValue)
                        {
                            sb.Append(Namespace);
                            sb.Append('.');
                        }

                        AppendContainingTypes(sb, _typeSymbol);

                        sb.Append(Name);

                        if (IsGenericType && !ContainsGenericParameters)
                        {
                            sb.Append('[');

                            bool first = true;
                            foreach (Type genericArg in GetGenericArguments())
                            {
                                if (!first)
                                {
                                    sb.Append(',');
                                }
                                else
                                {
                                    first = false;
                                }

                                sb.Append('[');
                                sb.Append(genericArg.AssemblyQualifiedName);
                                sb.Append(']');
                            }

                            sb.Append(']');
                        }
                    }

                    _fullName = sb.ToString();
                }

                return _fullName;

                static void AppendContainingTypes(StringBuilder sb, ITypeSymbol typeSymbol)
                {
                    if (typeSymbol.ContainingType != null)
                    {
                        AppendContainingTypes(sb, typeSymbol.ContainingType);
                        sb.Append(typeSymbol.ContainingType.MetadataName);
                        sb.Append('+');
                    }
                }
            }
        }

        public override Guid GUID => Guid.Empty;

        public override Module Module => throw new NotImplementedException();

        public override string? Namespace =>
            IsArray ?
            GetElementType().Namespace :
            _typeSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining));

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
                int rank = GetArrayRank();
                return elementType.Name + FormatArrayTypeNameSuffix(rank);
            }
        }

        internal static string FormatArrayTypeNameSuffix(int rank) => rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";

        public string SimpleName => _typeSymbol.Name;

        private Type? _enumType;

        public override bool IsEnum
        {
            get
            {
                _enumType ??= _metadataLoadContext.Resolve(typeof(Enum));
                Debug.Assert(_enumType != null);
                return IsSubclassOf(_enumType);
            }
        }

        [MemberNotNullWhen(true, nameof(_namedTypeSymbol))]
        public override bool IsGenericType => _namedTypeSymbol?.IsGenericType == true;

        public override bool ContainsGenericParameters
        {
            get
            {
                if (IsGenericParameter)
                {
                    return true;
                }

                for (INamedTypeSymbol? currentSymbol = _namedTypeSymbol; currentSymbol != null; currentSymbol = currentSymbol.ContainingType)
                {
                    if (currentSymbol.TypeArguments.Any(arg => arg.TypeKind == TypeKind.TypeParameter))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override bool IsGenericTypeDefinition => IsGenericType && SymbolEqualityComparer.Default.Equals(_namedTypeSymbol, _namedTypeSymbol.ConstructedFrom);

        public override bool IsGenericParameter => _typeSymbol.TypeKind == TypeKind.TypeParameter;

        public INamespaceSymbol GetNamespaceSymbol => _typeSymbol.ContainingNamespace;

        public override Type[] GetGenericArguments()
        {
            if (!IsGenericType)
            {
                return EmptyTypes;
            }

            var args = new List<Type>();
            AddTypeArguments(args, _namedTypeSymbol, _metadataLoadContext);
            return args.ToArray();

            static void AddTypeArguments(List<Type> args, INamedTypeSymbol typeSymbol, MetadataLoadContextInternal metadataLoadContext)
            {
                if (typeSymbol.ContainingType != null)
                {
                    AddTypeArguments(args, typeSymbol.ContainingType, metadataLoadContext);
                }
                foreach (ITypeSymbol item in typeSymbol.TypeArguments)
                {
                    args.Add(item.AsType(metadataLoadContext));
                }
            }
        }

        public override Type GetGenericTypeDefinition()
        {
            if (!IsGenericType)
            {
                throw new InvalidOperationException();
            }

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
            if (_namedTypeSymbol == null)
            {
                return Array.Empty<ConstructorInfo>();
            }

            List<ConstructorInfo> ctors = new();

            foreach (IMethodSymbol c in _namedTypeSymbol.Constructors)
            {
                if (c.IsImplicitlyDeclared && IsValueType)
                {
                    continue;
                }

                if (((BindingFlags.Public & bindingAttr) != 0 && c.DeclaredAccessibility == Accessibility.Public) ||
                    ((BindingFlags.NonPublic & bindingAttr) != 0 && c.DeclaredAccessibility != Accessibility.Public))
                {
                    ctors.Add(new ConstructorInfoWrapper(c, _metadataLoadContext));
                }
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

        public override Type MakeArrayType()
        {
            return _metadataLoadContext.Compilation.CreateArrayTypeSymbol(_typeSymbol).AsType(_metadataLoadContext);
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
                        (BindingFlags.Instance & bindingAttr) != 0 && (fieldSymbol.IsStatic || fieldSymbol.IsConst) ||
                        // symbol represents an explicitly named tuple element
                        fieldSymbol.IsExplicitlyNamedTupleElement)
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
            foreach (INamedTypeSymbol i in _typeSymbol.AllInterfaces)
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
            if (_namedTypeSymbol is null)
            {
                return Array.Empty<MethodInfo>();
            }

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

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
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

                bool isNested = _typeSymbol.ContainingType != null;

                switch (_typeSymbol.DeclaredAccessibility)
                {
                    case Accessibility.NotApplicable:
                    case Accessibility.Private:
                        _typeAttributes |= isNested ? TypeAttributes.NestedPrivate : TypeAttributes.NotPublic;
                        break;
                    case Accessibility.ProtectedAndInternal:
                        _typeAttributes |= isNested ? TypeAttributes.NestedFamANDAssem : TypeAttributes.NotPublic;
                        break;
                    case Accessibility.Protected:
                        _typeAttributes |= isNested ? TypeAttributes.NestedFamily : TypeAttributes.NotPublic;
                        break;
                    case Accessibility.Internal:
                        _typeAttributes |= isNested ? TypeAttributes.NestedAssembly : TypeAttributes.NotPublic;
                        break;
                    case Accessibility.ProtectedOrInternal:
                        _typeAttributes |= isNested ? TypeAttributes.NestedFamORAssem : TypeAttributes.NotPublic;
                        break;
                    case Accessibility.Public:
                        _typeAttributes |= isNested ? TypeAttributes.NestedPublic : TypeAttributes.Public;
                        break;
                }
            }

            return _typeAttributes.Value;
        }

        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            foreach (ConstructorInfo constructor in GetConstructors(bindingAttr))
            {
                ParameterInfo[] parameters = constructor.GetParameters();

                if (parameters.Length == (types?.Length ?? 0))
                {
                    bool mismatched = false;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType != types![i])
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

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            // TODO: performance; caching; honor bindingAttr
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

        private Type? _valueType;

        protected override bool IsValueTypeImpl()
        {
            _valueType ??= _metadataLoadContext.Resolve(typeof(ValueType));
            Debug.Assert(_valueType != null);
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

        public override bool IsAssignableFrom(Type? c)
        {
            TypeWrapper? tr = c switch
            {
                null => null,
                TypeWrapper tw => tw,
                _ => _metadataLoadContext.Resolve(c) as TypeWrapper,
            };

            return tr is not null &&
                (tr._typeSymbol.AllInterfaces.Contains(_typeSymbol, SymbolEqualityComparer.Default) ||
                (tr._namedTypeSymbol != null && tr._namedTypeSymbol.BaseTypes().Contains(_typeSymbol, SymbolEqualityComparer.Default)));
        }

#pragma warning disable RS1024 // Compare symbols correctly
        public override int GetHashCode() => _typeSymbol.GetHashCode();
#pragma warning restore RS1024 // Compare symbols correctly

        public override int GetArrayRank()
        {
            if (_arrayTypeSymbol == null)
            {
                throw new ArgumentException("Must be an array type.");
            }

            return _arrayTypeSymbol.Rank;
        }

        public override bool Equals(object? o)
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

        public override bool Equals(Type? o)
        {
            if (o != null)
            {
                if (o is TypeWrapper tw)
                {
                    return _typeSymbol.Equals(tw._typeSymbol, SymbolEqualityComparer.Default);
                }
                else if (_metadataLoadContext.Resolve(o) is TypeWrapper tww)
                {
                    return _typeSymbol.Equals(tww._typeSymbol, SymbolEqualityComparer.Default);
                }
            }

            return base.Equals(o);
        }

        public Location? Location => _typeSymbol.Locations.Length > 0 ? _typeSymbol.Locations[0] : null;
    }
}
