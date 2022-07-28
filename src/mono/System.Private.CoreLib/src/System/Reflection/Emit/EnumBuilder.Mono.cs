// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

//
// System.Reflection.Emit/EnumBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public sealed partial class EnumBuilder : TypeInfo
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private TypeBuilder _tb;

        private FieldBuilder _underlyingField;
        private Type _underlyingType;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2064:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        internal EnumBuilder(ModuleBuilder mb, string name, TypeAttributes visibility, Type underlyingType)
        {
            if ((visibility & ~TypeAttributes.VisibilityMask) != 0)
                throw new ArgumentException(SR.Argument_ShouldOnlySetVisibilityFlags, nameof(name));
            if ((visibility & TypeAttributes.VisibilityMask) >= TypeAttributes.NestedPublic && (visibility & TypeAttributes.VisibilityMask) <= TypeAttributes.NestedFamORAssem)
                throw new ArgumentException();
            _tb = new TypeBuilder(mb, name, (visibility | TypeAttributes.Sealed),
                typeof(Enum), null, PackingSize.Unspecified, 0, null);
            _underlyingType = underlyingType;
            _underlyingField = _tb.DefineField("value__", underlyingType,
                FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName);
            setup_enum_type(_tb);
        }

        internal TypeBuilder GetTypeBuilder()
        {
            return _tb;
        }

        internal override Type InternalResolve()
        {
            return _tb.InternalResolve();
        }

        internal override Type RuntimeResolve()
        {
            return _tb.RuntimeResolve();
        }

        public override Assembly Assembly
        {
            get
            {
                return _tb.Assembly;
            }
        }

        public override string? AssemblyQualifiedName
        {
            get
            {
                return _tb.AssemblyQualifiedName;
            }
        }

        public override Type? BaseType
        {
            get
            {
                return _tb.BaseType;
            }
        }

        public override Type? DeclaringType
        {
            get
            {
                return _tb.DeclaringType;
            }
        }

        public override string? FullName
        {
            get
            {
                return _tb.FullName;
            }
        }

        public override Guid GUID
        {
            get
            {
                return _tb.GUID;
            }
        }

        public override Module Module
        {
            get
            {
                return _tb.Module;
            }
        }

        public override string Name
        {
            get
            {
                return _tb.Name;
            }
        }

        public override string? Namespace
        {
            get
            {
                return _tb.Namespace;
            }
        }

        public override Type? ReflectedType
        {
            get
            {
                return _tb.ReflectedType;
            }
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                return _tb.TypeHandle;
            }
        }

        internal int TypeToken
        {
            get
            {
                return _tb.MetadataToken;
            }
        }

        public FieldBuilder UnderlyingField
        {
            get
            {
                return _underlyingField;
            }
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                return _underlyingType;
            }
        }

        [return: DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes.All)]
        public Type? CreateType()
        {
            return _tb.CreateType();
        }

        [return: DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes.All)]
        public TypeInfo? CreateTypeInfo()
        {
            return _tb.CreateTypeInfo();
        }

        public override Type GetEnumUnderlyingType()
        {
            return _underlyingType;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2110:ReflectionToDynamicallyAccessedMembers",
            Justification = "For instance members with MethodImplOptions.InternalCall, the linker preserves all fields of the declaring type. " +
            "The _tb field has DynamicallyAccessedMembersAttribute requirements, but the field access is safe because " +
            "Reflection.Emit is not subject to trimming.")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void setup_enum_type(Type t);

        public FieldBuilder DefineLiteral(string literalName, object? literalValue)
        {
            Type fieldType = this;
            FieldBuilder fieldBuilder = _tb.DefineField(literalName,
                fieldType, (FieldAttributes.Literal |
                (FieldAttributes.Static | FieldAttributes.Public)));
            fieldBuilder.SetConstant(literalValue);
            return fieldBuilder;
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return _tb.attrs;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(
            BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[]? modifiers)
        {
            return _tb.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return _tb.GetConstructors(bindingAttr);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return _tb.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                return _tb.GetCustomAttributes(inherit);
            else
                return _tb.GetCustomAttributes(attributeType, inherit);
        }

        public override Type? GetElementType()
        {
            return _tb.GetElementType();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        {
            return _tb.GetEvent(name, bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents()
        {
            return _tb.GetEvents();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return _tb.GetEvents(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            return _tb.GetField(name, bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return _tb.GetFields(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase)
        {
            return _tb.GetInterface(name, ignoreCase);
        }

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
        {
            return _tb.GetInterfaceMap(interfaceType);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
        {
            return _tb.GetInterfaces();
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            return _tb.GetMember(name, type, bindingAttr);
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return _tb.GetMembers(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(
            string name, BindingFlags bindingAttr, Binder? binder,
            CallingConventions callConvention, Type[]? types,
            ParameterModifier[]? modifiers)
        {
            if (types == null)
            {
                return _tb.GetMethod(name, bindingAttr);
            }

            return _tb.GetMethod(name, bindingAttr, binder,
                callConvention, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return _tb.GetMethods(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr)
        {
            return _tb.GetNestedType(name, bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return _tb.GetNestedTypes(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return _tb.GetProperties(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(
            string name, BindingFlags bindingAttr, Binder? binder,
            Type? returnType, Type[]? types,
            ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        protected override bool HasElementTypeImpl()
        {
            return _tb.HasElementType;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(
            string name, BindingFlags invokeAttr, Binder? binder,
            object? target, object?[]? args,
            ParameterModifier[]? modifiers, CultureInfo? culture,
            string[]? namedParameters)
        {
            return _tb.InvokeMember(name, invokeAttr, binder, target,
                args, modifiers, culture, namedParameters);
        }

        protected override bool IsArrayImpl()
        {
            return false;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return false;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsValueTypeImpl()
        {
            return true;
        }

        public override bool IsSZArray
        {
            get
            {
                return false;
            }
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return _tb.IsDefined(attributeType, inherit);
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType()
        {
            return new ArrayType(this, 0);
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank)
        {
            if (rank < 1)
                throw new IndexOutOfRangeException();
            return new ArrayType(this, rank);
        }

        public override Type MakeByRefType()
        {
            return new ByRefType(this);
        }

        public override Type MakePointerType()
        {
            return new PointerType(this);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            _tb.SetCustomAttribute(customBuilder);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        internal override bool IsUserType
        {
            get
            {
                return false;
            }
        }

        public override bool IsConstructedGenericType
        {
            get { return false; }
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        public override bool IsTypeDefinition => true;

        public override bool IsByRefLike => false;
    }
}
#endif
