// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// System.Reflection.Emit.TypeBuilderInstantiation
//
// Sean MacIsaac (macisaac@ximian.com)
// Paolo Molaro (lupus@ximian.com)
// Patrik Torstensson (patrik.torstensson@labs2.com)
//
// (C) 2001 Ximian, Inc.
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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    /*
     * TypeBuilderInstantiation represents an instantiation of a generic TypeBuilder.
     */
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class TypeBuilderInstantiation :
        TypeInfo
    {
        #region Keep in sync with object-internals.h MonoReflectionGenericClass
#pragma warning disable 649
        internal Type generic_type;
        private Type[] type_arguments;
#pragma warning restore 649
        #endregion

        private Dictionary<FieldInfo, FieldInfo>? fields;
        private Dictionary<ConstructorInfo, ConstructorInfo>? ctors;
        private Dictionary<MethodInfo, MethodInfo>? methods;

        internal TypeBuilderInstantiation()
        {
            // this should not be used
            throw new InvalidOperationException();
        }

        internal TypeBuilderInstantiation(Type tb, Type[] args)
        {
            this.generic_type = tb;
            this.type_arguments = args;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2006:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        internal override Type InternalResolve()
        {
            Type gtd = generic_type.InternalResolve();
            Type[] args = new Type[type_arguments.Length];
            for (int i = 0; i < type_arguments.Length; ++i)
                args[i] = type_arguments[i].InternalResolve();
            return gtd.MakeGenericType(args);
        }

        // Called from the runtime to return the corresponding finished Type object
        internal override Type RuntimeResolve()
        {
            if (generic_type is TypeBuilder tb && !tb.IsCreated())
                throw new NotImplementedException();
            for (int i = 0; i < type_arguments.Length; ++i)
            {
                Type t = type_arguments[i];
                if (t is TypeBuilder ttb && !ttb.IsCreated())
                    throw new NotImplementedException();
            }
            return InternalResolve();
        }

        internal bool IsCreated
        {
            get
            {
                return generic_type is TypeBuilder tb ? tb.is_created : true;
            }
        }

        private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private Type? GetParentType()
        {
            return InflateType(generic_type.BaseType);
        }

        internal Type? InflateType(Type? type)
        {
            return InflateType(type, type_arguments, null);
        }

        internal Type? InflateType(Type type, Type[] method_args)
        {
            return InflateType(type, type_arguments, method_args);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2006:UnrecognizedReflectionPattern",
            Justification = "Reflection emitted types have all of their members")]
        internal static Type? InflateType(Type? type, Type[]? type_args, Type[]? method_args)
        {
            if (type == null)
                return null;
            if (!type.IsGenericParameter && !type.ContainsGenericParameters)
                return type;
            if (type.IsGenericParameter)
            {
                if (type.DeclaringMethod == null)
                    return type_args == null ? type : type_args[type.GenericParameterPosition];
                return method_args == null ? type : method_args[type.GenericParameterPosition];
            }
            if (type.IsPointer)
                return InflateType(type.GetElementType(), type_args, method_args)!.MakePointerType();
            if (type.IsByRef)
                return InflateType(type.GetElementType(), type_args, method_args)!.MakeByRefType();
            if (type.IsArray)
            {
                if (type.GetArrayRank() > 1)
                    return InflateType(type.GetElementType(), type_args, method_args)!.MakeArrayType(type.GetArrayRank());

                if (type.ToString().EndsWith("[*]", StringComparison.Ordinal)) /*FIXME, the reflection API doesn't offer a way around this*/
                    return InflateType(type.GetElementType(), type_args, method_args)!.MakeArrayType(1);
                return InflateType(type.GetElementType(), type_args, method_args)!.MakeArrayType();
            }

            Type[] args = type.GetGenericArguments();
            for (int i = 0; i < args.Length; ++i)
                args[i] = InflateType(args[i], type_args, method_args)!;

            Type gtd = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
            return gtd.MakeGenericType(args);
        }

        public override Type? BaseType
        {
            get { return generic_type.BaseType; }
        }

        public override Type[] GetInterfaces()
        {
            throw new NotSupportedException();
        }

        protected override bool IsValueTypeImpl()
        {
            return generic_type.IsValueType;
        }

        internal override MethodInfo GetMethod(MethodInfo fromNoninstanciated)
        {
            if (methods == null)
                methods = new Dictionary<MethodInfo, MethodInfo>();
            if (!methods.ContainsKey(fromNoninstanciated))
                methods[fromNoninstanciated] = new MethodOnTypeBuilderInst(this, fromNoninstanciated);
            return methods[fromNoninstanciated]!;
        }

        internal override ConstructorInfo GetConstructor(ConstructorInfo fromNoninstanciated)
        {
            if (ctors == null)
                ctors = new Dictionary<ConstructorInfo, ConstructorInfo>();
            if (!ctors.ContainsKey(fromNoninstanciated))
                ctors[fromNoninstanciated] = new ConstructorOnTypeBuilderInst(this, fromNoninstanciated);
            return ctors[fromNoninstanciated]!;
        }

        internal override FieldInfo GetField(FieldInfo fromNoninstanciated)
        {
            if (fields == null)
                fields = new Dictionary<FieldInfo, FieldInfo>();
            if (!fields.ContainsKey(fromNoninstanciated))
                fields[fromNoninstanciated] = new FieldOnTypeBuilderInst(this, fromNoninstanciated);
            return fields[fromNoninstanciated]!;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bf)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bf)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bf)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bf)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bf)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bf)
        {
            throw new NotSupportedException();
        }

        public override bool IsAssignableFrom(Type? c)
        {
            throw new NotSupportedException();
        }

        public override Type UnderlyingSystemType
        {
            get { return this; }
        }

        public override Assembly Assembly
        {
            get { return generic_type.Assembly; }
        }

        public override Module Module
        {
            get { return generic_type.Module; }
        }

        public override string Name
        {
            get { return generic_type.Name; }
        }

        public override string? Namespace
        {
            get { return generic_type.Namespace; }
        }

        public override string? FullName
        {
            get { return format_name(true, false); }
        }

        public override string? AssemblyQualifiedName
        {
            get { return format_name(true, true); }
        }

        public override Guid GUID
        {
            get { throw new NotSupportedException(); }
        }

        private string? format_name(bool full_name, bool assembly_qualified)
        {
            StringBuilder sb = new StringBuilder(generic_type.FullName);

            sb.Append('[');
            for (int i = 0; i < type_arguments.Length; ++i)
            {
                if (i > 0)
                    sb.Append(',');

                string? name;
                if (full_name)
                {
                    string? assemblyName = type_arguments[i].Assembly.FullName;
                    name = type_arguments[i].FullName;
                    if (name != null && assemblyName != null)
                        name = name + ", " + assemblyName;
                }
                else
                {
                    name = type_arguments[i].ToString();
                }
                if (name == null)
                {
                    return null;
                }
                if (full_name)
                    sb.Append('[');
                sb.Append(name);
                if (full_name)
                    sb.Append(']');
            }
            sb.Append(']');
            if (assembly_qualified)
            {
                sb.Append(", ");
                sb.Append(generic_type.Assembly.FullName);
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return format_name(false, false)!;
        }

        public override Type GetGenericTypeDefinition()
        {
            return generic_type;
        }

        public override Type[] GetGenericArguments()
        {
            Type[] ret = new Type[type_arguments.Length];
            type_arguments.CopyTo(ret, 0);
            return ret;
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                foreach (Type t in type_arguments)
                {
                    if (t.ContainsGenericParameters)
                        return true;
                }
                return false;
            }
        }

        public override bool IsGenericTypeDefinition
        {
            get { return false; }
        }

        public override bool IsGenericType
        {
            get { return true; }
        }

        public override Type? DeclaringType
        {
            get { return generic_type.DeclaringType; }
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override Type MakeArrayType()
        {
            return new ArrayType(this, 0);
        }

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

        public override Type GetElementType()
        {
            throw new NotSupportedException();
        }

        protected override bool HasElementTypeImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsArrayImpl()
        {
            return false;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return false;
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return generic_type.Attributes;
        }

        //stuff that throws
        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr,
                             Binder? binder, object? target, object?[]? args,
                             ParameterModifier[]? modifiers,
                             CultureInfo? culture, string[]? namedParameters)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
                                                     CallingConventions callConvention, Type[]? types,
                                                     ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                                                         Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr,
                                       Binder? binder,
                                       CallingConventions callConvention,
                                       Type[]? types,
                                       ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException();
        }

        //MemberInfo
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            if (IsCreated)
                return generic_type.GetCustomAttributes(inherit);
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (IsCreated)
                return generic_type.GetCustomAttributes(attributeType, inherit);
            throw new NotSupportedException();
        }

        internal override bool IsUserType
        {
            get
            {
                foreach (Type t in type_arguments)
                {
                    if (t.IsUserType)
                        return true;
                }
                return false;
            }
        }

        internal static Type MakeGenericType(Type type, Type[] typeArguments)
        {
            return new TypeBuilderInstantiation(type, typeArguments);
        }

        public override bool IsTypeDefinition => false;

        public override bool IsConstructedGenericType => true;
    }
}
#else
namespace System.Reflection.Emit
{
	abstract class TypeBuilderInstantiation : TypeInfo
	{
		internal static Type MakeGenericType (Type type, Type[] typeArguments)
		{
			throw new NotSupportedException("User types are not supported under full aot");
		}
	}
}
#endif
