// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Martin Baulig (martin@ximian.com)
//
// (C) 2004 Novell, Inc.
//

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

#if MONO_FEATURE_SRE
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class RuntimeGenericTypeParameterBuilder : GenericTypeParameterBuilder
    {
#region Sync with MonoReflectionGenericParam in object-internals.h
        private RuntimeTypeBuilder tbuilder;
        private RuntimeMethodBuilder? mbuilder;
        private string name;
        private int index;
        private Type? base_type;
        private Type[]? iface_constraints;
        private CustomAttributeBuilder[]? cattrs;
        private GenericParameterAttributes attrs;
#endregion

        [DynamicDependency(nameof(attrs))]  // Automatically keeps all previous fields too due to StructLayout
        internal RuntimeGenericTypeParameterBuilder(RuntimeTypeBuilder tbuilder, RuntimeMethodBuilder? mbuilder, string name, int index)
        {
            this.tbuilder = tbuilder;
            this.mbuilder = mbuilder;
            this.name = name;
            this.index = index;
        }

        protected override void SetBaseTypeConstraintCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? baseTypeConstraint)
        {
            this.base_type = baseTypeConstraint ?? typeof(object);
        }

        protected override void SetInterfaceConstraintsCore(params Type[]? interfaceConstraints)
        {
            this.iface_constraints = interfaceConstraints;
        }

        protected override void SetGenericParameterAttributesCore(GenericParameterAttributes genericParameterAttributes)
        {
            this.attrs = genericParameterAttributes;
        }

        internal override Type InternalResolve()
        {
            if (mbuilder != null)
                return MethodBase.GetMethodFromHandle(mbuilder.MethodHandleInternal, mbuilder.TypeBuilder.InternalResolve().TypeHandle)!.GetGenericArguments()[index];
            return tbuilder.InternalResolve().GetGenericArguments()[index];
        }

        internal override Type RuntimeResolve()
        {
            if (mbuilder != null)
                return MethodBase.GetMethodFromHandle(mbuilder.MethodHandleInternal, mbuilder.TypeBuilder.RuntimeResolve().TypeHandle)!.GetGenericArguments()[index];
            return tbuilder.RuntimeResolve().GetGenericArguments()[index];
        }

        public override bool IsSubclassOf(Type c)
        {
            throw not_supported();
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return TypeAttributes.Public;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr,
                                       Binder? binder,
                                       CallingConventions callConvention,
                                       Type[] types,
                                       ParameterModifier[]? modifiers)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        public override EventInfo[] GetEvents()
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr,
                                 Binder? binder,
                                 CallingConventions callConvention,
                                 Type[]? types, ParameterModifier[]? modifiers)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw not_supported();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr,
                                 Binder? binder, Type? returnType,
                                 Type[]? types,
                                 ParameterModifier[]? modifiers)
        {
            throw not_supported();
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            throw not_supported();
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            if (typeInfo == null)
                return false;

            return IsAssignableFrom(typeInfo.AsType());
        }

        public override bool IsInstanceOfType(object? o)
        {
            throw not_supported();
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
            return base_type != null ? base_type.IsValueType : false;
        }

        public override bool IsSZArray
        {
            get
            {
                return false;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr,
                             Binder? binder, object? target, object?[]? args,
                             ParameterModifier[]? modifiers,
                             CultureInfo? culture, string[]? namedParameters)
        {
            throw not_supported();
        }

        public override Type GetElementType()
        {
            throw not_supported();
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                return this;
            }
        }

        public override Assembly Assembly
        {
            get { return tbuilder.Assembly; }
        }

        public override string? AssemblyQualifiedName
        {
            get { return null; }
        }

        public override Type? BaseType
        {
            get { return base_type; }
        }

        public override string? FullName
        {
            get { return null; }
        }

        public override Guid GUID
        {
            get { throw not_supported(); }
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw not_supported();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw not_supported();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw not_supported();
        }

        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
        {
            throw not_supported();
        }

        public override string Name
        {
            get { return name; }
        }

        public override string? Namespace
        {
            get { return null; }
        }

        public override Module Module
        {
            get { return tbuilder.Module; }
        }

        public override Type? DeclaringType
        {
            get { return mbuilder != null ? mbuilder.DeclaringType : tbuilder; }
        }

        public override Type? ReflectedType
        {
            get { return DeclaringType; }
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get { throw not_supported(); }
        }

        public override Type[] GetGenericArguments()
        {
            throw new InvalidOperationException();
        }

        public override Type GetGenericTypeDefinition()
        {
            throw new InvalidOperationException();
        }

        public override bool ContainsGenericParameters
        {
            get { return true; }
        }

        public override bool IsGenericParameter
        {
            get { return true; }
        }

        public override bool IsGenericType
        {
            get { return false; }
        }

        public override bool IsGenericTypeDefinition
        {
            get { return false; }
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                return attrs;
            }
        }

        public override int GenericParameterPosition
        {
            get { return index; }
        }

        public override Type[] GetGenericParameterConstraints()
        {
            throw new InvalidOperationException();
        }

        public override MethodBase? DeclaringMethod
        {
            get { return mbuilder; }
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            CustomAttributeBuilder customBuilder = new CustomAttributeBuilder(con, binaryAttribute);
            if (cattrs != null)
            {
                CustomAttributeBuilder[] new_array = new CustomAttributeBuilder[cattrs.Length + 1];
                cattrs.CopyTo(new_array, 0);
                new_array[cattrs.Length] = customBuilder;
                cattrs = new_array;
            }
            else
            {
                cattrs = new CustomAttributeBuilder[1];
                cattrs[0] = customBuilder;
            }
        }

        private static NotSupportedException not_supported()
        {
            return new NotSupportedException();
        }

        public override string ToString()
        {
            return name;
        }

        // FIXME:
        public override bool Equals(object? o)
        {
            return base.Equals(o);
        }

        // FIXME:
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]", this, 0)!;
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank)
        {
            string s = GetRankString(rank);
            return SymbolType.FormCompoundType(s, this, 0)!;
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType("&", this, 0)!;
        }

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] typeArguments)
        {
            throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericTypeDefinition, this));
        }

        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType("*", this, 0)!;
        }

        internal override bool IsUserType
        {
            get
            {
                return false;
            }
        }

        public override bool IsByRefLike => false;
    }
}
#endif
