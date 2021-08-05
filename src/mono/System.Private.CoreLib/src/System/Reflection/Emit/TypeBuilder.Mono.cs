// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit.TypeBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class TypeBuilder : TypeInfo
    {
#region Sync with MonoReflectionTypeBuilder in object-internals.h
        private string tname; // name in internal form
        private string nspace; // namespace in internal form

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private Type? parent;

        private Type? nesting_type;
        internal Type[]? interfaces;
        internal int num_methods;
        internal MethodBuilder[]? methods;
        internal ConstructorBuilder[]? ctors;
        internal PropertyBuilder[]? properties;
        internal int num_fields;
        internal FieldBuilder[]? fields;
        internal EventBuilder[]? events;
        private CustomAttributeBuilder[]? cattrs;
        internal TypeBuilder[]? subtypes;
        internal TypeAttributes attrs;
        private int table_idx;
        private ModuleBuilder pmodule;
        private int class_size;
        private PackingSize packing_size;
        private IntPtr generic_container;
        private GenericTypeParameterBuilder[]? generic_params;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        private TypeInfo? created;

        private int state;
#endregion

        private ITypeName fullname;
        private bool createTypeCalled;
        private Type? underlying_type;

        public const int UnspecifiedTypeSize = 0;

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return attrs;
        }

        [DynamicDependency(nameof(state))]  // Automatically keeps all previous fields too due to StructLayout
        [DynamicDependency(nameof(IsAssignableToInternal))] // Used from reflection.c: mono_reflection_call_is_assignable_to
        internal TypeBuilder(ModuleBuilder mb, TypeAttributes attr, int table_idx)
        {
            this.parent = null;
            this.attrs = attr;
            this.class_size = UnspecifiedTypeSize;
            this.table_idx = table_idx;
            this.tname = table_idx == 1 ? "<Module>" : "type_" + table_idx.ToString();
            this.nspace = string.Empty;
            this.fullname = TypeIdentifiers.WithoutEscape(this.tname);
            pmodule = mb;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2074:UnrecognizedReflectionPattern",
            Justification = "Linker doesn't analyze ResolveUserType but it's an identity function")]

        [DynamicDependency(nameof(state))]  // Automatically keeps all previous fields too due to StructLayout
        [DynamicDependency(nameof(IsAssignableToInternal))] // Used from reflection.c: mono_reflection_call_is_assignable_to
        internal TypeBuilder(ModuleBuilder mb, string fullname, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]Type? parent, Type[]? interfaces, PackingSize packing_size, int type_size, Type? nesting_type)
        {
            int sep_index;
            this.parent = ResolveUserType(parent);
            this.attrs = attr;
            this.class_size = type_size;
            this.packing_size = packing_size;
            this.nesting_type = nesting_type;

            check_name(nameof(fullname), fullname);

            if (parent == null && (attr & TypeAttributes.Interface) != 0 && (attr & TypeAttributes.Abstract) == 0)
                throw new InvalidOperationException(SR.InvalidOperation_BadInterfaceNotAbstract);

            sep_index = fullname.LastIndexOf('.');
            if (sep_index != -1)
            {
                this.tname = fullname.Substring(sep_index + 1);
                this.nspace = fullname.Substring(0, sep_index);
            }
            else
            {
                this.tname = fullname;
                this.nspace = string.Empty;
            }
            if (interfaces != null)
            {
                this.interfaces = new Type[interfaces.Length];
                Array.Copy(interfaces, this.interfaces, interfaces.Length);
            }
            pmodule = mb;

            if (((attr & TypeAttributes.Interface) == 0) && (parent == null))
                this.parent = typeof(object);

            // skip .<Module> ?
            table_idx = mb.get_next_table_index(this, 0x02, 1);
            this.fullname = GetFullName();
        }

        public override Assembly Assembly
        {
            get { return pmodule.Assembly; }
        }

        public override string? AssemblyQualifiedName
        {
            get
            {
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.AssemblyQualifiedName);
            }
        }

        public override Type? BaseType
        {
            get
            {
                return parent;
            }
        }

        public override Type? DeclaringType
        {
            get { return nesting_type; }
        }

        [ComVisible(true)]
        public override bool IsSubclassOf(Type c)
        {
            Type? t;
            if (c == null)
                return false;
            if (c == this)
                return false;
            t = parent;
            while (t != null)
            {
                if (c == t)
                    return true;
                t = t.BaseType;
            }
            return false;
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                if (is_created)
                    return created!.UnderlyingSystemType;

                if (IsEnum)
                {
                    if (underlying_type != null)
                        return underlying_type;
                    throw new InvalidOperationException(
                        "Enumeration type is not defined.");
                }

                return this;
            }
        }

        private ITypeName GetFullName()
        {
            ITypeIdentifier ident = TypeIdentifiers.FromInternal(tname);
            if (nesting_type != null)
                return TypeNames.FromDisplay(nesting_type.FullName!).NestedName(ident);
            if ((nspace != null) && (nspace.Length > 0))
                return TypeIdentifiers.FromInternal(nspace, ident);
            return ident;
        }

        public override string? FullName
        {
            get
            {
                return TypeNameBuilder.ToString(this, TypeNameBuilder.Format.FullName);
            }
        }

        public override Guid GUID
        {
            get
            {
                check_created();
                return created!.GUID;
            }
        }

        public override Module Module
        {
            get { return pmodule; }
        }

        public override string Name
        {
            get { return tname; }
        }

        public override string? Namespace
        {
            get { return nspace; }
        }

        public PackingSize PackingSize
        {
            get { return packing_size; }
        }

        public int Size
        {
            get { return class_size; }
        }

        public override Type? ReflectedType
        {
            get { return nesting_type; }
        }

        [ComVisible(true)]
        public void AddInterfaceImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException(nameof(interfaceType));
            if (interfaceType.IsByRef)
                throw new ArgumentException(SR.Argument_CannotGetTypeTokenForByRef);
            check_not_created();

            if (interfaces != null)
            {
                // Check for duplicates
                foreach (Type t in interfaces)
                    if (t == interfaceType)
                        return;

                Type[] ifnew = new Type[interfaces.Length + 1];
                interfaces.CopyTo(ifnew, 0);
                ifnew[interfaces.Length] = interfaceType;
                interfaces = ifnew;
            }
            else
            {
                interfaces = new Type[1];
                interfaces[0] = interfaceType;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                                       CallingConventions callConvention, Type[] types,
                                       ParameterModifier[]? modifiers)
        {
            check_created();

            if (created == typeof(object))
            {
                /*
                 * This happens when building corlib. Calling created.GetConstructor
                 * would return constructors from the real mscorlib, instead of the
                 * newly built one.
                 */

                if (ctors == null)
                    return null;

                ConstructorBuilder? found = null;
                int count = 0;

                foreach (ConstructorBuilder cb in ctors)
                {
                    if (callConvention != CallingConventions.Any && cb.CallingConvention != callConvention)
                        continue;
                    found = cb;
                    count++;
                }

                if (count == 0)
                    return null;
                if (types == null)
                {
                    if (count > 1)
                        throw new AmbiguousMatchException();
                    return found;
                }
                MethodBase[] match = new MethodBase[count];
                if (count == 1)
                    match[0] = found!;
                else
                {
                    count = 0;
                    foreach (ConstructorInfo m in ctors)
                    {
                        if (callConvention != CallingConventions.Any && m.CallingConvention != callConvention)
                            continue;
                        match[count++] = m;
                    }
                }

                binder ??= DefaultBinder;
                return (ConstructorInfo?)binder.SelectMethod(bindingAttr, match, types, modifiers);
            }

            return created!.GetConstructor(bindingAttr, binder, callConvention, types!, modifiers); // FIXME: types shouldn't be null
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (!is_created)
                throw new NotSupportedException();
            /*
             * MS throws NotSupported here, but we can't because some corlib
             * classes make calls to IsDefined.
             */
            return CustomAttribute.IsDefined(this, attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            check_created();

            return created!.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            check_created();

            return created!.GetCustomAttributes(attributeType, inherit);
        }

        public TypeBuilder DefineNestedType(string name)
        {
            return DefineNestedType(name, TypeAttributes.NestedPrivate,
                                     typeof(object), null);
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr)
        {
            return DefineNestedType(name, attr, typeof(object), null);
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
        {
            return DefineNestedType(name, attr, parent, null);
        }

        private TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces,
                              PackingSize packSize, int typeSize)
        {
            // Visibility must be NestedXXX
            /* This breaks mcs
            if (((attrs & TypeAttributes.VisibilityMask) == TypeAttributes.Public) ||
                ((attrs & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic))
                throw new ArgumentException (nameof(attr), "Bad type flags for nested type.");
            */
            if (interfaces != null)
            {
                foreach (Type iface in interfaces)
                {
                    if (iface == null)
                        throw new ArgumentNullException(nameof(interfaces));
                    if (iface.IsByRef)
                        throw new ArgumentException(nameof(interfaces));
                }
            }

            TypeBuilder res = new TypeBuilder(pmodule, name, attr, parent, interfaces, packSize, typeSize, this);
            res.fullname = res.GetFullName();
            pmodule.RegisterTypeName(res, res.fullname);
            if (subtypes != null)
            {
                TypeBuilder[] new_types = new TypeBuilder[subtypes.Length + 1];
                Array.Copy(subtypes, new_types, subtypes.Length);
                new_types[subtypes.Length] = res;
                subtypes = new_types;
            }
            else
            {
                subtypes = new TypeBuilder[1];
                subtypes[0] = res;
            }
            return res;
        }

        [ComVisible(true)]
        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
        {
            return DefineNestedType(name, attr, parent, interfaces, PackingSize.Unspecified, UnspecifiedTypeSize);
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typeSize)
        {
            return DefineNestedType(name, attr, parent, null, PackingSize.Unspecified, typeSize);
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize)
        {
            return DefineNestedType(name, attr, parent, null, packSize, UnspecifiedTypeSize);
        }

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize,
                                         int typeSize)
        {
            return DefineNestedType(name, attr, parent, null, packSize, typeSize);
        }

        [ComVisible(true)]
        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
        {
            return DefineConstructor(attributes, callingConvention, parameterTypes, null, null);
        }

        [ComVisible(true)]
        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
        {
            check_not_created();
            if (IsInterface && (attributes & MethodAttributes.Static) == 0)
                throw new InvalidOperationException();
            ConstructorBuilder cb = new ConstructorBuilder(this, attributes,
                callingConvention, parameterTypes, requiredCustomModifiers,
                optionalCustomModifiers);
            if (ctors != null)
            {
                ConstructorBuilder[] new_ctors = new ConstructorBuilder[ctors.Length + 1];
                Array.Copy(ctors, new_ctors, ctors.Length);
                new_ctors[ctors.Length] = cb;
                ctors = new_ctors;
            }
            else
            {
                ctors = new ConstructorBuilder[1];
                ctors[0] = cb;
            }
            return cb;
        }

        [ComVisible(true)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
        {
            Type parent_type, old_parent_type;

            if (IsInterface)
                throw new InvalidOperationException();
            if ((attributes & (MethodAttributes.Static | MethodAttributes.Virtual)) > 0)
                throw new ArgumentException(SR.Arg_NoStaticVirtual);

            if (parent != null)
                parent_type = parent;
            else
                parent_type = typeof(object);

            old_parent_type = parent_type;
            parent_type = parent_type.InternalResolve();
            /*This avoids corlib to have self references.*/
            if (parent_type == typeof(object) || parent_type == typeof(ValueType))
                parent_type = old_parent_type;

            ConstructorInfo? parent_constructor =
                parent_type.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null, EmptyTypes, null);
            if (parent_constructor == null)
            {
                throw new NotSupportedException("Parent does"
                    + " not have a default constructor."
                    + " The default constructor must be"
                    + " explicitly defined.");
            }

            ConstructorBuilder cb = DefineConstructor(attributes,
                CallingConventions.Standard, EmptyTypes);
            ILGenerator ig = cb.GetILGenerator();
            ig.Emit(OpCodes.Ldarg_0);
            ig.Emit(OpCodes.Call, parent_constructor);
            ig.Emit(OpCodes.Ret);
            cb.finished = true;
            return cb;
        }

        private void append_method(MethodBuilder mb)
        {
            if (methods != null)
            {
                if (methods.Length == num_methods)
                {
                    MethodBuilder[] new_methods = new MethodBuilder[methods.Length * 2];
                    Array.Copy(methods, new_methods, num_methods);
                    methods = new_methods;
                }
            }
            else
            {
                methods = new MethodBuilder[1];
            }
            methods[num_methods] = mb;
            num_methods++;
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
        {
            return DefineMethod(name, attributes, CallingConventions.Standard,
                returnType, parameterTypes);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
        {
            return DefineMethod(name, attributes, callingConvention, returnType,
                null, null, parameterTypes, null, null);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            check_name(nameof(name), name);
            check_not_created();
            if (IsInterface && (
                !((attributes & MethodAttributes.Abstract) != 0) ||
                !((attributes & MethodAttributes.Virtual) != 0)) &&
                !(((attributes & MethodAttributes.Static) != 0)))
                throw new ArgumentException("Interface method must be abstract and virtual.");

            if (returnType == null)
                returnType = typeof(void);
            MethodBuilder res = new MethodBuilder(this, name, attributes,
                callingConvention, returnType,
                returnTypeRequiredCustomModifiers,
                returnTypeOptionalCustomModifiers, parameterTypes,
                parameterTypeRequiredCustomModifiers,
                parameterTypeOptionalCustomModifiers);
            append_method(res);
            return res;
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            return DefinePInvokeMethod(name, dllName, entryName, attributes,
                callingConvention, returnType, null, null, parameterTypes,
                null, null, nativeCallConv, nativeCharSet);
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(
                        string name,
                        string dllName,
                        string entryName, MethodAttributes attributes,
                        CallingConventions callingConvention,
                        Type? returnType,
                        Type[]? returnTypeRequiredCustomModifiers,
                        Type[]? returnTypeOptionalCustomModifiers,
                        Type[]? parameterTypes,
                        Type[][]? parameterTypeRequiredCustomModifiers,
                        Type[][]? parameterTypeOptionalCustomModifiers,
                        CallingConvention nativeCallConv,
                        CharSet nativeCharSet)
        {
            check_name(nameof(name), name);
            check_name(nameof(dllName), dllName);
            check_name(nameof(entryName), entryName);
            if ((attributes & MethodAttributes.Abstract) != 0)
                throw new ArgumentException("PInvoke methods must be static and native and cannot be abstract.");
            if (IsInterface)
                throw new ArgumentException("PInvoke methods cannot exist on interfaces.");
            check_not_created();

            MethodBuilder res
                = new MethodBuilder(
                        this,
                        name,
                        attributes,
                        callingConvention,
                        returnType,
                        returnTypeRequiredCustomModifiers,
                        returnTypeOptionalCustomModifiers,
                        parameterTypes,
                        parameterTypeRequiredCustomModifiers,
                        parameterTypeOptionalCustomModifiers,
                        dllName,
                        entryName,
                        nativeCallConv,
                        nativeCharSet);
            append_method(res);
            return res;
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            return DefinePInvokeMethod(name, dllName, name, attributes, callingConvention, returnType, parameterTypes,
                nativeCallConv, nativeCharSet);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
        {
            return DefineMethod(name, attributes, CallingConventions.Standard);
        }

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
        {
            return DefineMethod(name, attributes, callingConvention, null, null);
        }

        public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            if (methodInfoBody == null)
                throw new ArgumentNullException(nameof(methodInfoBody));
            if (methodInfoDeclaration == null)
                throw new ArgumentNullException(nameof(methodInfoDeclaration));
            check_not_created();
            if (methodInfoBody.DeclaringType != this)
                throw new ArgumentException("method body must belong to this type");

            if (methodInfoBody is MethodBuilder mb)
            {
                mb.set_override(methodInfoDeclaration);
            }
        }

        public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
        {
            return DefineField(fieldName, type, null, null, attributes);
        }

        public FieldBuilder DefineField(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes)
        {
            check_name(nameof(fieldName), fieldName);
            if (type == typeof(void))
                throw new ArgumentException("Bad field type in defining field.");
            check_not_created();

            FieldBuilder res = new FieldBuilder(this, fieldName, type, attributes, requiredCustomModifiers, optionalCustomModifiers);
            if (fields != null)
            {
                if (fields.Length == num_fields)
                {
                    FieldBuilder[] new_fields = new FieldBuilder[fields.Length * 2];
                    Array.Copy(fields, new_fields, num_fields);
                    fields = new_fields;
                }
                fields[num_fields] = res;
                num_fields++;
            }
            else
            {
                fields = new FieldBuilder[1];
                fields[0] = res;
                num_fields++;
            }

            if (IsEnum)
            {
                if (underlying_type == null && (attributes & FieldAttributes.Static) == 0)
                    underlying_type = type;
            }

            return res;
        }

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[]? parameterTypes)
        {
            return DefineProperty(name, attributes, 0, returnType, null, null, parameterTypes, null, null);
        }

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? parameterTypes)
        {
            return DefineProperty(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);
        }

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            return DefineProperty(name, attributes, 0, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
        }

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention, Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            check_name(nameof(name), name);
            if (parameterTypes != null)
                foreach (Type param in parameterTypes)
                    if (param == null)
                        throw new ArgumentNullException(nameof(parameterTypes));
            check_not_created();

            PropertyBuilder res = new PropertyBuilder(this, name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

            if (properties != null)
            {
                Array.Resize(ref properties, properties.Length + 1);
                properties[properties.Length - 1] = res;
            }
            else
            {
                properties = new PropertyBuilder[1] { res };
            }
            return res;
        }

        [ComVisible(true)]
        public ConstructorBuilder DefineTypeInitializer()
        {
            return DefineConstructor(MethodAttributes.Public |
                MethodAttributes.Static | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName, CallingConventions.Standard,
                null);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2110:ReflectionToDynamicallyAccessedMembers",
            Justification = "For instance member internal calls, the linker preserves all fields of the declaring type. " +
            "The parent and created fields have DynamicallyAccessedMembersAttribute requirements, but creating the runtime class is safe " +
            "because the annotations fully preserve the parent type, and the type created via Reflection.Emit is not subject to trimming.")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern TypeInfo create_runtime_class();

        private bool is_nested_in(Type? t)
        {
            while (t != null)
            {
                if (t == this)
                    return true;
                else
                    t = t.DeclaringType;
            }
            return false;
        }

        // Return whenever this type has a ctor defined using DefineMethod ()
        private bool has_ctor_method()
        {
            MethodAttributes ctor_attrs = MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

            for (int i = 0; i < num_methods; ++i)
            {
                MethodBuilder mb = (MethodBuilder)(methods![i]);

                if (mb.Name == ConstructorInfo.ConstructorName && (mb.Attributes & ctor_attrs) == ctor_attrs)
                    return true;
            }

            return false;
        }

        // We require emitted types to have all members on their bases to be accessible.
        // This is basically an identity function for `this`.
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type? CreateType()
        {
            return CreateTypeInfo();
        }

        // We require emitted types to have all members on their bases to be accessible.
        // This is basically an identity function for `this`.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2083:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public TypeInfo? CreateTypeInfo()
        {
            /* handle nesting_type */
            if (createTypeCalled)
                return created;

            if (!IsInterface && (parent == null) && (this != typeof(object)) && (FullName != "<Module>"))
            {
                SetParent(typeof(object));
            }

            // Fire TypeResolve events for fields whose type is an unfinished
            // value type.
            if (fields != null)
            {
                foreach (FieldBuilder fb in fields)
                {
                    if (fb == null)
                        continue;
                    Type ft = fb.FieldType;
                    if (!fb.IsStatic && (ft is TypeBuilder builder) && ft.IsValueType && (ft != this) && is_nested_in(ft))
                    {
                        TypeBuilder tb = builder;
                        if (!tb.is_created)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
            }

            if (parent != null)
            {
                if (parent.IsByRef)
                    throw new ArgumentException();
                if (IsInterface)
                    throw new TypeLoadException();
            }

            //
            // On classes, define a default constructor if not provided
            //
            if (!(IsInterface || IsValueType) && (ctors == null) && (tname != "<Module>") &&
                (GetAttributeFlagsImpl() & TypeAttributes.Abstract | TypeAttributes.Sealed) != (TypeAttributes.Abstract | TypeAttributes.Sealed) && !has_ctor_method())
                DefineDefaultConstructor(MethodAttributes.Public);

            createTypeCalled = true;

            if (parent != null)
            {
                if (parent.IsSealed)
                    throw new TypeLoadException("Could not load type '" + fullname.DisplayName + "' from assembly '" + Assembly + "' because the parent type is sealed.");
                if (parent.IsGenericTypeDefinition)
                    throw new BadImageFormatException();
            }

            if (parent == typeof(Enum) && methods != null)
                throw new TypeLoadException("Could not load type '" + fullname.DisplayName + "' from assembly '" + Assembly + "' because it is an enum with methods.");
            if (interfaces != null)
            {
                foreach (Type iface in interfaces)
                {
                    if (iface.IsNestedPrivate && iface.Assembly != Assembly)
                        throw new TypeLoadException("Could not load type '" + fullname.DisplayName + "' from assembly '" + Assembly + "' because it is implements the inaccessible interface '" + iface.FullName + "'.");
                    if (iface.IsGenericTypeDefinition)
                        throw new BadImageFormatException();
                    if (!iface.IsInterface)
                        throw new TypeLoadException();
                    if (iface is TypeBuilder builder && !builder.is_created)
                        throw new TypeLoadException();
                }
            }

            if (fields != null)
            {
                foreach (FieldBuilder fb in fields)
                {
                    if (fb == null)
                        continue;
                    if (fb.FieldType.IsByRef)
                        throw new COMException();
                }
            }

            if (methods != null)
            {
                bool is_concrete = !IsAbstract;
                for (int i = 0; i < num_methods; ++i)
                {
                    MethodBuilder mb = (MethodBuilder)(methods[i]);
                    if (is_concrete && mb.IsAbstract)
                        throw new InvalidOperationException("Type is concrete but has abstract method " + mb);
                    mb.check_override();
                    mb.fixup();
                }
            }

            if (ctors != null)
            {
                foreach (ConstructorBuilder ctor in ctors)
                    ctor.fixup();
            }

            ResolveUserTypes();

            created = create_runtime_class();
            if (created != null)
                return created;
            return this;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2074:UnrecognizedReflectionPattern",
            Justification = "Linker doesn't analyze ResolveUserType but it's an identity function")]
        private void ResolveUserTypes()
        {
            parent = ResolveUserType(parent);
            ResolveUserTypes(interfaces);
            if (fields != null)
            {
                foreach (FieldBuilder fb in fields)
                {
                    if (fb != null)
                        fb.ResolveUserTypes();
                }
            }
            if (methods != null)
            {
                foreach (MethodBuilder mb in methods)
                {
                    if (mb != null)
                        mb.ResolveUserTypes();
                }
            }
            if (ctors != null)
            {
                foreach (ConstructorBuilder cb in ctors)
                {
                    if (cb != null)
                        cb.ResolveUserTypes();
                }
            }
        }

        internal static void ResolveUserTypes(Type?[]? types)
        {
            if (types != null)
                for (int i = 0; i < types.Length; ++i)
                    types[i] = ResolveUserType(types[i]);
        }

        [return: NotNullIfNotNull("t")]
        internal static Type? ResolveUserType(Type? t)
        {
            if (t != null && ((t.GetType().Assembly != typeof(int).Assembly) || (t is TypeDelegator)))
            {
                t = t.UnderlyingSystemType;
                if (t != null && ((t.GetType().Assembly != typeof(int).Assembly) || (t is TypeDelegator)))
                    throw new NotSupportedException("User defined subclasses of System.Type are not yet supported.");
                return t;
            }
            else
            {
                return t;
            }
        }
        /*
                internal void GenerateDebugInfo (ISymbolWriter symbolWriter)
                {
                    symbolWriter.OpenNamespace (this.Namespace);

                    if (methods != null) {
                        for (int i = 0; i < num_methods; ++i) {
                            MethodBuilder metb = (MethodBuilder) methods[i];
                            metb.GenerateDebugInfo (symbolWriter);
                        }
                    }

                    if (ctors != null) {
                        foreach (ConstructorBuilder ctor in ctors)
                            ctor.GenerateDebugInfo (symbolWriter);
                    }

                    symbolWriter.CloseNamespace ();

                    if (subtypes != null) {
                        for (int i = 0; i < subtypes.Length; ++i)
                            subtypes [i].GenerateDebugInfo (symbolWriter);
                    }
                }
        */
        [ComVisible(true)]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            if (is_created)
                return created!.GetConstructors(bindingAttr);

            throw new NotSupportedException();
        }

        internal ConstructorInfo[] GetConstructorsInternal(BindingFlags bindingAttr)
        {
            if (ctors == null)
                return Array.Empty<ConstructorInfo>();
            List<ConstructorInfo> result = new List<ConstructorInfo>();
            bool match;
            MethodAttributes mattrs;

            foreach (ConstructorBuilder c in ctors)
            {
                match = false;
                mattrs = c.Attributes;
                if ((mattrs & MethodAttributes.MemberAccessMask) == MethodAttributes.Public)
                {
                    if ((bindingAttr & BindingFlags.Public) != 0)
                        match = true;
                }
                else
                {
                    if ((bindingAttr & BindingFlags.NonPublic) != 0)
                        match = true;
                }
                if (!match)
                    continue;
                match = false;
                if ((mattrs & MethodAttributes.Static) != 0)
                {
                    if ((bindingAttr & BindingFlags.Static) != 0)
                        match = true;
                }
                else
                {
                    if ((bindingAttr & BindingFlags.Instance) != 0)
                        match = true;
                }
                if (!match)
                    continue;
                result.Add(c);
            }
            return result.ToArray();
        }

        public override Type GetElementType()
        {
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        {
            check_created();
            return created!.GetEvent(name, bindingAttr);
        }

        /* Needed to keep signature compatibility with MS.NET */
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2085:UnrecognizedReflectionPattern",
            Justification = "Linker doesn't recognize GetEvents(BindingFlags.Public) but this is what the body is doing")]
        public override EventInfo[] GetEvents()
        {
            const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            return GetEvents(DefaultBindingFlags);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            if (is_created)
                return created!.GetEvents(bindingAttr);
            throw new NotSupportedException();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            check_created();
            return created!.GetField(name, bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            check_created();
            return created!.GetFields(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase)
        {
            check_created();
            return created!.GetInterface(name, ignoreCase);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
        {
            if (is_created)
                return created!.GetInterfaces();

            if (interfaces != null)
            {
                Type[] ret = new Type[interfaces.Length];
                interfaces.CopyTo(ret, 0);
                return ret;
            }
            else
            {
                return EmptyTypes;
            }
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMember(string name, MemberTypes type,
                                                BindingFlags bindingAttr)
        {
            check_created();
            return created!.GetMember(name, type, bindingAttr);
        }

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            check_created();
            return created!.GetMembers(bindingAttr);
        }

        private MethodInfo[] GetMethodsByName(string? name, BindingFlags bindingAttr, bool ignoreCase)
        {
            MethodInfo[]? candidates;
            bool match;
            MethodAttributes mattrs;

            if (((bindingAttr & BindingFlags.DeclaredOnly) == 0) && (parent != null))
            {
                MethodInfo[] parent_methods = parent.GetMethods(bindingAttr);
                List<MethodInfo> parent_candidates = new List<MethodInfo>(parent_methods.Length);

                bool flatten = (bindingAttr & BindingFlags.FlattenHierarchy) != 0;

                for (int i = 0; i < parent_methods.Length; i++)
                {
                    MethodInfo m = parent_methods[i];

                    mattrs = m.Attributes;

                    if (m.IsStatic && !flatten)
                        continue;

                    match = (mattrs & MethodAttributes.MemberAccessMask) switch
                    {
                        MethodAttributes.Public => (bindingAttr & BindingFlags.Public) != 0,
                        MethodAttributes.Assembly => (bindingAttr & BindingFlags.NonPublic) != 0,
                        MethodAttributes.Private => false,
                        _ => (bindingAttr & BindingFlags.NonPublic) != 0,
                    };

                    if (match)
                        parent_candidates.Add(m);
                }

                if (methods == null)
                {
                    candidates = new MethodInfo[parent_candidates.Count];
                    parent_candidates.CopyTo(candidates);
                }
                else
                {
                    candidates = new MethodInfo[methods.Length + parent_candidates.Count];
                    parent_candidates.CopyTo(candidates, 0);
                    methods.CopyTo(candidates, parent_candidates.Count);
                }
            }
            else
                candidates = methods;

            if (candidates == null)
                return Array.Empty<MethodInfo>();

            List<MethodInfo> result = new List<MethodInfo>();

            foreach (MethodInfo c in candidates)
            {
                if (c == null)
                    continue;
                if (name != null)
                {
                    if (string.Compare(c.Name, name, ignoreCase) != 0)
                        continue;
                }
                match = false;
                mattrs = c.Attributes;
                if ((mattrs & MethodAttributes.MemberAccessMask) == MethodAttributes.Public)
                {
                    if ((bindingAttr & BindingFlags.Public) != 0)
                        match = true;
                }
                else
                {
                    if ((bindingAttr & BindingFlags.NonPublic) != 0)
                        match = true;
                }
                if (!match)
                    continue;
                match = false;
                if ((mattrs & MethodAttributes.Static) != 0)
                {
                    if ((bindingAttr & BindingFlags.Static) != 0)
                        match = true;
                }
                else
                {
                    if ((bindingAttr & BindingFlags.Instance) != 0)
                        match = true;
                }
                if (!match)
                    continue;
                result.Add(c);
            }
            return result.ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            check_created();

            return GetMethodsByName(null, bindingAttr, false);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr,
                                 Binder? binder,
                                 CallingConventions callConvention,
                                 Type[]? types, ParameterModifier[]? modifiers)
        {
            check_created();

            if (types == null)
                return created!.GetMethod(name, bindingAttr);

            return created!.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr)
        {
            check_created();

            if (subtypes == null)
                return null;

            foreach (TypeBuilder t in subtypes)
            {
                if (!t.is_created)
                    continue;
                if ((t.attrs & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic)
                {
                    if ((bindingAttr & BindingFlags.Public) == 0)
                        continue;
                }
                else
                {
                    if ((bindingAttr & BindingFlags.NonPublic) == 0)
                        continue;
                }
                if (t.Name == name)
                    return t.created;
            }

            return null;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            if (!is_created)
                throw new NotSupportedException();

            bool match;
            List<Type> result = new List<Type>();

            if (subtypes == null)
                return EmptyTypes;
            foreach (TypeBuilder t in subtypes)
            {
                match = false;
                if ((t.attrs & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic)
                {
                    if ((bindingAttr & BindingFlags.Public) != 0)
                        match = true;
                }
                else
                {
                    if ((bindingAttr & BindingFlags.NonPublic) != 0)
                        match = true;
                }
                if (!match)
                    continue;
                result.Add(t);
            }
            return result.ToArray();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            check_created();
            return created!.GetProperties(bindingAttr);
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            throw not_supported();
        }

        protected override bool HasElementTypeImpl()
        {
            // a TypeBuilder can never represent an array, pointer
            if (!is_created)
                return false;

            return created!.HasElementType;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
        {
            check_created();
            return created!.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

        protected override bool IsArrayImpl()
        {
            return false; /*A TypeBuilder never represents a non typedef type.*/
        }

        protected override bool IsByRefImpl()
        {
            return false; /*A TypeBuilder never represents a non typedef type.*/
        }

        protected override bool IsCOMObjectImpl()
        {
            return ((GetAttributeFlagsImpl() & TypeAttributes.Import) != 0);
        }

        protected override bool IsPointerImpl()
        {
            return false; /*A TypeBuilder never represents a non typedef type.*/
        }

        protected override bool IsPrimitiveImpl()
        {
            // FIXME
            return false;
        }

        // FIXME: I doubt just removing this still works.
        protected override bool IsValueTypeImpl()
        {
            Type? parent_type = parent;
            while (parent_type != null)
            {
                if (parent_type == typeof(ValueType))
                    return true;
                parent_type = parent_type.BaseType;
            }
            return false;
        }

        public override bool IsSZArray
        {
            get
            {
                return false;
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

        public override Type MakeGenericType(params Type[] typeArguments)
        {
            //return base.MakeGenericType (typeArguments);

            if (!IsGenericTypeDefinition)
                throw new InvalidOperationException("not a generic type definition");
            if (typeArguments == null)
                throw new ArgumentNullException(nameof(typeArguments));

            if (generic_params!.Length != typeArguments.Length)
                throw new ArgumentException(string.Format("The type or method has {0} generic parameter(s) but {1} generic argument(s) where provided. A generic argument must be provided for each generic parameter.", generic_params.Length, typeArguments.Length), nameof(typeArguments));

            foreach (Type t in typeArguments)
            {
                if (t == null)
                    throw new ArgumentNullException(nameof(typeArguments));
            }

            Type[] copy = new Type[typeArguments.Length];
            typeArguments.CopyTo(copy, 0);
            return AssemblyBuilder.MakeGenericType(this, copy);
        }

        public override Type MakePointerType()
        {
            return new PointerType(this);
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                check_created();
                return created!.TypeHandle;
            }
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException(nameof(customBuilder));

            string? attrname = customBuilder.Ctor.ReflectedType!.FullName;
            if (attrname == "System.Runtime.InteropServices.StructLayoutAttribute")
            {
                byte[] data = customBuilder.Data;
                int layout_kind; /* the (stupid) ctor takes a short or an int ... */
                layout_kind = (int)data[2];
                layout_kind |= ((int)data[3]) << 8;
                attrs &= ~TypeAttributes.LayoutMask;
                attrs |= ((LayoutKind)layout_kind) switch
                {
                    LayoutKind.Auto => TypeAttributes.AutoLayout,
                    LayoutKind.Explicit => TypeAttributes.ExplicitLayout,
                    LayoutKind.Sequential => TypeAttributes.SequentialLayout,
                    _ => throw new Exception("Error in customattr"), // we should ignore it since it can be any value anyway...
                };

                Type ctor_type = customBuilder.Ctor is ConstructorBuilder builder ? builder.parameters![0] : customBuilder.Ctor.GetParametersInternal()[0].ParameterType;
                int pos = 6;
                if (ctor_type.FullName == "System.Int16")
                    pos = 4;
                int nnamed = (int)data[pos++];
                nnamed |= ((int)data[pos++]) << 8;
                for (int i = 0; i < nnamed; ++i)
                {
                    //byte named_type = data [pos++];
                    pos++;
                    byte type = data[pos++];
                    int len;
                    string named_name;

                    if (type == 0x55)
                    {
                        len = CustomAttributeBuilder.decode_len(data, pos, out pos);
                        //string named_typename =
                        CustomAttributeBuilder.string_from_bytes(data, pos, len);
                        pos += len;
                        // FIXME: Check that 'named_type' and 'named_typename' match, etc.
                        //        See related code/FIXME in mono/mono/metadata/reflection.c
                    }

                    len = CustomAttributeBuilder.decode_len(data, pos, out pos);
                    named_name = CustomAttributeBuilder.string_from_bytes(data, pos, len);
                    pos += len;
                    /* all the fields are integers in StructLayout */
                    int value = (int)data[pos++];
                    value |= ((int)data[pos++]) << 8;
                    value |= ((int)data[pos++]) << 16;
                    value |= ((int)data[pos++]) << 24;
                    switch (named_name)
                    {
                        case "CharSet":
                            switch ((CharSet)value)
                            {
                                case CharSet.None:
                                case CharSet.Ansi:
                                    attrs &= ~(TypeAttributes.UnicodeClass | TypeAttributes.AutoClass);
                                    break;
                                case CharSet.Unicode:
                                    attrs &= ~TypeAttributes.AutoClass;
                                    attrs |= TypeAttributes.UnicodeClass;
                                    break;
                                case CharSet.Auto:
                                    attrs &= ~TypeAttributes.UnicodeClass;
                                    attrs |= TypeAttributes.AutoClass;
                                    break;
                                default:
                                    break; // error out...
                            }
                            break;
                        case "Pack":
                            packing_size = (PackingSize)value;
                            break;
                        case "Size":
                            class_size = value;
                            break;
                        default:
                            break; // error out...
                    }
                }
                return;
            }
            else if (attrname == "System.Runtime.CompilerServices.SpecialNameAttribute")
            {
                attrs |= TypeAttributes.SpecialName;
                return;
            }
            else if (attrname == "System.SerializableAttribute")
            {
                attrs |= TypeAttributes.Serializable;
                return;
            }
            else if (attrname == "System.Runtime.InteropServices.ComImportAttribute")
            {
                attrs |= TypeAttributes.Import;
                return;
            }
            else if (attrname == "System.Security.SuppressUnmanagedCodeSecurityAttribute")
            {
                attrs |= TypeAttributes.HasSecurity;
            }

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

        [ComVisible(true)]
        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        public EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventtype)
        {
            check_name(nameof(name), name);
            if (eventtype == null)
                throw new ArgumentNullException(nameof(eventtype));
            check_not_created();
            if (eventtype.IsByRef)
                throw new ArgumentException(SR.Argument_CannotGetTypeTokenForByRef);
            EventBuilder res = new EventBuilder(this, name, attributes, eventtype);
            if (events != null)
            {
                EventBuilder[] new_events = new EventBuilder[events.Length + 1];
                Array.Copy(events, new_events, events.Length);
                new_events[events.Length] = res;
                events = new_events;
            }
            else
            {
                events = new EventBuilder[1];
                events[0] = res;
            }
            return res;
        }

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            FieldBuilder res = DefineUninitializedData(name, data.Length, attributes);
            res.SetRVAData(data);
            return res;
        }

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0)
                throw new ArgumentException("Empty name is not legal", nameof(name));
            if ((size <= 0) || (size > 0x3f0000))
                throw new ArgumentException("Data size must be > 0 and < 0x3f0000");
            check_not_created();

            string typeName = "$ArrayType$" + size;
            ITypeIdentifier ident = TypeIdentifiers.WithoutEscape(typeName);
            Type? datablobtype = pmodule.GetRegisteredType(fullname.NestedName(ident));
            if (datablobtype == null)
            {
                TypeBuilder tb = DefineNestedType(typeName,
                    TypeAttributes.NestedPrivate | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
                                                   typeof(ValueType), null, PackingSize.Size1, size);
                tb.CreateType();
                datablobtype = tb;
            }
            return DefineField(name, datablobtype, attributes | FieldAttributes.Static | FieldAttributes.HasFieldRVA);
        }

        public override int MetadataToken => 0x02000000 | table_idx;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2074:UnrecognizedReflectionPattern",
            Justification = "Linker doesn't analyze ResolveUserType but it's an identity function")]
        public void SetParent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
        {
            check_not_created();

            if (parent == null)
            {
                if ((attrs & TypeAttributes.Interface) != 0)
                {
                    if ((attrs & TypeAttributes.Abstract) == 0)
                        throw new InvalidOperationException("Interface must be declared abstract.");
                    this.parent = null;
                }
                else
                {
                    this.parent = typeof(object);
                }
            }
            else
            {
                if (parent.IsInterface)
                    throw new ArgumentException(SR.Argument_CannotSetParentToInterface);
                this.parent = parent;
            }
            this.parent = ResolveUserType(this.parent);
        }

        internal int get_next_table_index(object obj, int table, int count)
        {
            return pmodule.get_next_table_index(obj, table, count);
        }

        [ComVisible(true)]
        public override InterfaceMapping GetInterfaceMap([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type interfaceType)
        {
            if (created == null)
                throw new NotSupportedException("This method is not implemented for incomplete types.");

            return created.GetInterfaceMap(interfaceType);
        }

        internal override Type InternalResolve()
        {
            check_created();
            return created!;
        }

        internal override Type RuntimeResolve()
        {
            check_created();
            return created!;
        }

        internal bool is_created
        {
            get
            {
                return createTypeCalled;
            }
        }

        private static Exception not_supported()
        {
            return new NotSupportedException("The invoked member is not supported in a dynamic module.");
        }

        internal void check_not_created()
        {
            if (is_created)
                throw new InvalidOperationException("Unable to change after type has been created.");
        }

        private void check_created()
        {
            if (!is_created)
                throw not_supported();
        }

        private static void check_name(string argName, string name)
        {
            if (name == null)
                throw new ArgumentNullException(argName);
            if (name.Length == 0 || name[0] == ((char)0))
                throw new ArgumentException(SR.Argument_EmptyName, argName);
        }

        public override string ToString()
        {
            return FullName!;
        }

        // FIXME:
        public override bool IsAssignableFrom([NotNullWhen(true)] Type? c)
        {
            return base.IsAssignableFrom(c);
        }

        // FIXME: "arrays"
        internal bool IsAssignableToInternal([NotNullWhen(true)] Type? c)
        {
            if (c == this)
                return true;

            if (c!.IsInterface)
            {
                if (parent != null && is_created)
                {
                    if (c.IsAssignableFrom(parent))
                        return true;
                }

                if (interfaces == null)
                    return false;
                foreach (Type t in interfaces)
                    if (c.IsAssignableFrom(t))
                        return true;
                if (!is_created)
                    return false;
            }

            if (parent == null)
                return c == typeof(object);
            else
                return c.IsAssignableFrom(parent);
        }

        public bool IsCreated()
        {
            return is_created;
        }

        public override Type[] GetGenericArguments()
        {
            if (generic_params == null)
                return Type.EmptyTypes;
            Type[] args = new Type[generic_params.Length];
            generic_params.CopyTo(args, 0);
            return args;
        }

        public override Type GetGenericTypeDefinition()
        {
            if (generic_params == null)
                throw new InvalidOperationException("Type is not generic");
            return this;
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                return generic_params != null;
            }
        }

        public override bool IsGenericParameter
        {
            get
            {
                return false;
            }
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get { return GenericParameterAttributes.None; }
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                return generic_params != null;
            }
        }

        public override bool IsGenericType
        {
            get { return IsGenericTypeDefinition; }
        }

        // FIXME:
        public override int GenericParameterPosition
        {
            get
            {
                return 0;
            }
        }

        public override MethodBase? DeclaringMethod
        {
            get
            {
                return null;
            }
        }

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));
            if (names.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(names));

            generic_params = new GenericTypeParameterBuilder[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string item = names[i];
                if (item == null)
                    throw new ArgumentNullException(nameof(names));
                generic_params[i] = new GenericTypeParameterBuilder(this, null, item, i);
            }

            return generic_params;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Linker thinks Type.GetConstructor(ConstructorInfo) is one of the public APIs because it doesn't analyze method signatures. We already have ConstructorInfo.")]
        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {
            /*FIXME I would expect the same checks of GetMethod here*/
            if (type == null)
                throw new ArgumentException("Type is not generic", nameof(type));

            if (!type.IsGenericType)
                throw new ArgumentException("Type is not a generic type", nameof(type));

            if (type.IsGenericTypeDefinition)
                throw new ArgumentException("Type cannot be a generic type definition", nameof(type));

            if (constructor == null)
                throw new NullReferenceException(); //MS raises this instead of an ArgumentNullException

            if (!constructor.DeclaringType!.IsGenericTypeDefinition)
                throw new ArgumentException("constructor declaring type is not a generic type definition", nameof(constructor));
            if (constructor.DeclaringType != type.GetGenericTypeDefinition())
                throw new ArgumentException("constructor declaring type is not the generic type definition of type", nameof(constructor));

            ConstructorInfo res = type.GetConstructor(constructor);
            if (res == null)
                throw new ArgumentException("constructor not found");

            return res;
        }

        private static bool IsValidGetMethodType(Type type)
        {
            if (type is TypeBuilder || type is TypeBuilderInstantiation)
                return true;
            /*GetMethod() must work with TypeBuilders after CreateType() was called.*/
            if (type.Module is ModuleBuilder)
                return true;
            if (type.IsGenericParameter)
                return false;

            Type[] inst = type.GetGenericArguments();
            if (inst == null)
                return false;
            for (int i = 0; i < inst.Length; ++i)
            {
                if (IsValidGetMethodType(inst[i]))
                    return true;
            }
            return false;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "Type.MakeGenericType is used to create a typical instantiation")]
        public static MethodInfo GetMethod(Type type, MethodInfo method)
        {
            if (!IsValidGetMethodType(type))
                throw new ArgumentException("type is not TypeBuilder but " + type.GetType(), nameof(type));

            if (type is TypeBuilder && type.ContainsGenericParameters)
                type = type.MakeGenericType(type.GetGenericArguments());

            if (!type.IsGenericType)
                throw new ArgumentException("type is not a generic type", nameof(type));

            if (!method.DeclaringType!.IsGenericTypeDefinition)
                throw new ArgumentException("method declaring type is not a generic type definition", nameof(method));
            if (method.DeclaringType != type.GetGenericTypeDefinition())
                throw new ArgumentException("method declaring type is not the generic type definition of type", nameof(method));
            if (method == null)
                throw new NullReferenceException(); //MS raises this instead of an ArgumentNullException

            MethodInfo res = type.GetMethod(method);
            if (res == null)
                throw new ArgumentException(string.Format("method {0} not found in type {1}", method.Name, type));

            return res;
        }

        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            if (!type.IsGenericType)
                throw new ArgumentException("Type is not a generic type", nameof(type));

            if (type.IsGenericTypeDefinition)
                throw new ArgumentException("Type cannot be a generic type definition", nameof(type));

            if (field is FieldOnTypeBuilderInst)
                throw new ArgumentException("The specified field must be declared on a generic type definition.", nameof(field));

            if (field.DeclaringType != type.GetGenericTypeDefinition())
                throw new ArgumentException("field declaring type is not the generic type definition of type", nameof(field));

            FieldInfo res = type.GetField(field);
            if (res == null)
                throw new System.Exception("field not found");
            else
                return res;
        }

        internal override bool IsUserType
        {
            get
            {
                return false;
            }
        }

        internal override bool IsTypeBuilder() => true;

        public override bool IsConstructedGenericType
        {
            get { return false; }
        }

        public override bool IsAssignableFrom([NotNullWhen(true)] TypeInfo? typeInfo)
        {
            return base.IsAssignableFrom(typeInfo);
        }

        internal static bool SetConstantValue(Type destType, object? value, ref object? destValue)
        {
            // Mono: This is based on the CoreCLR
            // TypeBuilder.SetConstantValue except it writes to an
            // out argument instead of doing an icall, and it uses
            // TypeCode instead of CorElementType (like
            // MonoTypeEnum) which we don't have in our corlib and
            // our member fields are different.

            // This is a helper function that is used by ParameterBuilder, PropertyBuilder,
            // and FieldBuilder to validate a default value and save it in the meta-data.

            if (value != null)
            {
                Type type = value.GetType();

                // We should allow setting a constant value on a ByRef parameter
                if (destType.IsByRef)
                    destType = destType.GetElementType()!;

                // Convert nullable types to their underlying type.
                // This is necessary for nullable enum types to pass the IsEnum check that's coming next.
                destType = Nullable.GetUnderlyingType(destType) ?? destType;

                if (destType.IsEnum)
                {
                    //                                   |  UnderlyingSystemType     |  Enum.GetUnderlyingType() |  IsEnum
                    // ----------------------------------|---------------------------|---------------------------|---------
                    // runtime Enum Type                 |  self                     |  underlying type of enum  |  TRUE
                    // EnumBuilder                       |  underlying type of enum  |  underlying type of enum* |  TRUE
                    // TypeBuilder of enum types**       |  underlying type of enum  |  Exception                |  TRUE
                    // TypeBuilder of enum types (baked) |  runtime enum type        |  Exception                |  TRUE

                    //  *: the behavior of Enum.GetUnderlyingType(EnumBuilder) might change in the future
                    //     so let's not depend on it.
                    // **: created with System.Enum as the parent type.

                    // The above behaviors might not be the most consistent but we have to live with them.

                    Type? underlyingType;
                    if (destType is EnumBuilder enumBldr)
                    {
                        underlyingType = enumBldr.GetEnumUnderlyingType();

                        // The constant value supplied should match either the baked enum type or its underlying type
                        // we don't need to compare it with the EnumBuilder itself because you can never have an object of that type
                        if (!((enumBldr.GetTypeBuilder().is_created && type == enumBldr.GetTypeBuilder().created) ||
                              type == underlyingType))
                            throw_argument_ConstantDoesntMatch();
                    }
                    else if (destType is TypeBuilder typeBldr)
                    {
                        underlyingType = typeBldr.underlying_type;

                        // The constant value supplied should match either the baked enum type or its underlying type
                        // typeBldr.m_enumUnderlyingType is null if the user hasn't created a "value__" field on the enum
                        if (underlyingType == null || (type != typeBldr.UnderlyingSystemType && type != underlyingType))
                            throw_argument_ConstantDoesntMatch();
                    }
                    else
                    {
                        // must be a runtime Enum Type

                        // Debug.Assert(destType is RuntimeType, "destType is not a runtime type, an EnumBuilder, or a TypeBuilder.");

                        underlyingType = Enum.GetUnderlyingType(destType);

                        // The constant value supplied should match either the enum itself or its underlying type
                        if (type != destType && type != underlyingType)
                            throw_argument_ConstantDoesntMatch();
                    }

                    type = underlyingType!;
                }
                else
                {
                    // Note that it is non CLS compliant if destType != type. But RefEmit never guarantees CLS-Compliance.
                    if (!destType.IsAssignableFrom(type))
                        throw_argument_ConstantDoesntMatch();
                }

                TypeCode corType = GetTypeCode(type);

                switch (corType)
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Boolean:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Char:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Single:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Double:
                        destValue = value;
                        return true;
                    case TypeCode.String:
                        destValue = value;
                        return true;
                    case TypeCode.DateTime:
                        //date is a I8 representation
                        long ticks = ((DateTime)value).Ticks;
                        destValue = ticks;
                        return true;
                    default:
                        throw new ArgumentException(type!.ToString() + " is not a supported constant type.");
                }
            }
            else
            {
                // A null default value in metadata is permissible even for non-nullable value types.
                // (See ECMA-335 II.15.4.1.4 "The .param directive" and II.22.9 "Constant" for details.)
                // This is how the Roslyn compilers generally encode `default(TValueType)` default values.

                destValue = null;
                return true;
            }
        }

        private static void throw_argument_ConstantDoesntMatch()
        {
            throw new ArgumentException("Constant does not match the defined type.");
        }

        public override bool IsTypeDefinition => true;

        public override bool IsByRefLike => false;
    }
}
#endif
