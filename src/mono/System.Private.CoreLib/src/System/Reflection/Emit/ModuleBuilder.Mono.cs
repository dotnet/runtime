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
// System.Reflection.Emit/ModuleBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    public partial class ModuleBuilder : Module
    {
#region Sync with MonoReflectionModuleBuilder in object-internals.h

#region This class inherits from Module, but the runtime expects it to have the same layout as MonoModule
        internal IntPtr _impl; /* a pointer to a MonoImage */
        internal Assembly assembly;
        internal string fqname;
        internal string name;
        internal string scopename;
        internal bool is_resource;
        internal int token;
#endregion

        private UIntPtr dynamic_image; /* GC-tracked */
        private int num_types;
        private TypeBuilder[]? types;
        private CustomAttributeBuilder[]? cattrs;
        private int table_idx;
        internal AssemblyBuilder assemblyb;
        private object[]? global_methods;
        private object[]? global_fields;
        private bool is_main;
        private object? resources;
        private IntPtr unparented_classes;
        private int[]? table_indexes;
#endregion

        private byte[] guid;
        private TypeBuilder? global_type;
        private bool global_type_created;
        // name_cache keys are display names
        private Dictionary<ITypeName, TypeBuilder> name_cache;
        private Dictionary<string, int> us_string_cache;
        private ModuleBuilderTokenGenerator? token_gen;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void basic_init(ModuleBuilder ab);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void set_wrappers_type(ModuleBuilder mb, Type? ab);

        [DynamicDependency(nameof(table_indexes))]  // Automatically keeps all previous fields too due to StructLayout
        internal ModuleBuilder(AssemblyBuilder assb, string name)
        {
            this.name = this.scopename = name;
            this.fqname = name;
            this.assembly = this.assemblyb = assb;
            guid = Guid.NewGuid().ToByteArray();
            table_idx = get_next_table_index(0x00, 1);
            name_cache = new Dictionary<ITypeName, TypeBuilder>();
            us_string_cache = new Dictionary<string, int>(512);
            this.global_type_created = false;

            basic_init(this);

            CreateGlobalType();

            TypeBuilder tb = new TypeBuilder(this, TypeAttributes.Abstract, 0xFFFFFF); /*last valid token*/
            Type? type = tb.CreateTypeInfo();
            set_wrappers_type(this, type);
        }

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        public override string FullyQualifiedName
        {
            get
            {
                string fullyQualifiedName = fqname;
                if (fullyQualifiedName == null)
                    return null!; // FIXME: this should not return null

                return fullyQualifiedName;
            }
        }

        public void CreateGlobalFunctions()
        {
            if (global_type_created)
                throw new InvalidOperationException("global methods already created");
            if (global_type != null)
            {
                global_type_created = true;
                global_type.CreateTypeInfo();
            }
        }

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        {
            ArgumentNullException.ThrowIfNull(data);

            FieldAttributes maskedAttributes = attributes & ~FieldAttributes.ReservedMask;
            FieldBuilder fb = DefineDataImpl(name, data.Length, maskedAttributes | FieldAttributes.HasFieldRVA);
            fb.SetRVAData(data);

            return fb;
        }

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
        {
            return DefineDataImpl(name, size, attributes & ~FieldAttributes.ReservedMask);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Reflection.Emit is not subject to trimming")]
        private FieldBuilder DefineDataImpl(string name, int size, FieldAttributes attributes)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            if (global_type_created)
                throw new InvalidOperationException("global fields already created");
            if ((size <= 0) || (size >= 0x3f0000))
                throw new ArgumentException("Data size must be > 0 and < 0x3f0000", null as string);

            CreateGlobalType();

            string typeName = "$ArrayType$" + size;
            Type? datablobtype = GetType(typeName, false, false);
            if (datablobtype == null)
            {
                TypeBuilder tb = DefineType(typeName,
                    TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
                                             typeof(ValueType), null, FieldBuilder.RVADataPackingSize(size), size);
                tb.CreateType();
                datablobtype = tb;
            }
            FieldBuilder fb = global_type!.DefineField(name, datablobtype, attributes | FieldAttributes.Static);

            if (global_fields != null)
            {
                FieldBuilder[] new_fields = new FieldBuilder[global_fields.Length + 1];
                Array.Copy(global_fields, new_fields, global_fields.Length);
                new_fields[global_fields.Length] = fb;
                global_fields = new_fields;
            }
            else
            {
                global_fields = new FieldBuilder[] { fb };
            }
            return fb;
        }

        private void addGlobalMethod(MethodBuilder mb)
        {
            if (global_methods != null)
            {
                MethodBuilder[] new_methods = new MethodBuilder[global_methods.Length + 1];
                Array.Copy(global_methods, new_methods, global_methods.Length);
                new_methods[global_methods.Length] = mb;
                global_methods = new_methods;
            }
            else
            {
                global_methods = new MethodBuilder[] { mb };
            }
        }

        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers, Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
        {
            ArgumentNullException.ThrowIfNull(name);
            if ((attributes & MethodAttributes.Static) == 0)
                throw new ArgumentException("global methods must be static");
            if (global_type_created)
                throw new InvalidOperationException("global methods already created");
            CreateGlobalType();
            MethodBuilder mb = global_type!.DefineMethod(name, attributes, callingConvention, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);

            addGlobalMethod(mb);
            return mb;
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            ArgumentNullException.ThrowIfNull(name);
            if ((attributes & MethodAttributes.Static) == 0)
                throw new ArgumentException("global methods must be static");
            if (global_type_created)
                throw new InvalidOperationException("global methods already created");
            CreateGlobalType();
            MethodBuilder mb = global_type!.DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention, returnType, parameterTypes, nativeCallConv, nativeCharSet);

            addGlobalMethod(mb);
            return mb;
        }

        private void AddType(TypeBuilder tb)
        {
            if (types != null)
            {
                if (types.Length == num_types)
                {
                    TypeBuilder[] new_types = new TypeBuilder[types.Length * 2];
                    Array.Copy(types, new_types, num_types);
                    types = new_types;
                }
            }
            else
            {
                types = new TypeBuilder[1];
            }
            types[num_types] = tb;
            num_types++;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "Reflection.Emit is not subject to trimming")]
        private TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packingSize, int typesize)
        {
            ArgumentNullException.ThrowIfNull(name, "fullname");
            ITypeIdentifier ident = TypeIdentifiers.FromInternal(name);
            if (name_cache.ContainsKey(ident))
                throw new ArgumentException("Duplicate type name within an assembly.");
            TypeBuilder res = new TypeBuilder(this, name, attr, parent, interfaces, packingSize, typesize, null);
            AddType(res);

            name_cache.Add(ident, res);

            return res;
        }

        internal void RegisterTypeName(TypeBuilder tb, ITypeName name)
        {
            name_cache.Add(name, tb);
        }

        internal TypeBuilder? GetRegisteredType(ITypeName name)
        {
            TypeBuilder? result;
            name_cache.TryGetValue(name, out result);
            return result;
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
        {
            return DefineType(name, attr, parent, interfaces, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packingSize, int typesize)
        {
            return DefineType(name, attr, parent, null, packingSize, typesize);
        }

        public MethodInfo GetArrayMethod(Type arrayClass, string methodName, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
        {
            return new MonoArrayMethod(arrayClass, methodName, callingConvention, returnType!, parameterTypes!); // FIXME: nulls should be allowed
        }

        public EnumBuilder DefineEnum(string name, TypeAttributes visibility, Type underlyingType)
        {
            ITypeIdentifier ident = TypeIdentifiers.FromInternal(name);
            if (name_cache.ContainsKey(ident))
                throw new ArgumentException("Duplicate type name within an assembly.");

            EnumBuilder eb = new EnumBuilder(this, name, visibility, underlyingType);
            TypeBuilder res = eb.GetTypeBuilder();
            AddType(res);
            name_cache.Add(ident, res);
            return eb;
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type? GetType(string className)
        {
            return GetType(className, false, false);
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type? GetType(string className, bool ignoreCase)
        {
            return GetType(className, false, ignoreCase);
        }

        private static TypeBuilder? search_in_array(TypeBuilder[] arr, int validElementsInArray, ITypeName className)
        {
            int i;
            for (i = 0; i < validElementsInArray; ++i)
            {
                if (string.Compare(className.DisplayName, arr[i].FullName, true, CultureInfo.InvariantCulture) == 0)
                {
                    return arr[i];
                }
            }
            return null;
        }

        private static TypeBuilder? search_nested_in_array(TypeBuilder[] arr, int validElementsInArray, ITypeName className)
        {
            int i;
            for (i = 0; i < validElementsInArray; ++i)
            {
                if (string.Compare(className.DisplayName, arr[i].Name, true, CultureInfo.InvariantCulture) == 0)
                    return arr[i];
            }
            return null;
        }

        private static TypeBuilder? GetMaybeNested(TypeBuilder t, IEnumerable<ITypeName> nested)
        {
            TypeBuilder? result = t;

            foreach (ITypeName pname in nested)
            {
                if (result.subtypes == null)
                    return null;
                result = search_nested_in_array(result.subtypes, result.subtypes.Length, pname);
                if (result == null)
                    return null;
            }
            return result;
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type? GetType(string className, bool throwOnError, bool ignoreCase)
        {
            ArgumentException.ThrowIfNullOrEmpty(className);

            TypeBuilder? result = null;

            if (types == null && throwOnError)
                throw new TypeLoadException(className);

            TypeSpec ts = TypeSpec.Parse(className);

            if (!ignoreCase)
            {
                ITypeName displayNestedName = ts.TypeNameWithoutModifiers();
                name_cache.TryGetValue(displayNestedName, out result);
            }
            else
            {
                if (types != null)
                    result = search_in_array(types, num_types, ts.Name!);
                if (!ts.IsNested && result != null)
                {
                    result = GetMaybeNested(result, ts.Nested);
                }
            }
            if ((result == null) && throwOnError)
                throw new TypeLoadException(className);
            if (result != null && (ts.HasModifiers || ts.IsByRef))
            {
                Type mt = result;
                if (result is TypeBuilder)
                {
                    var tb = result as TypeBuilder;
                    if (tb.is_created)
                        mt = tb.CreateType()!;
                }
                foreach (IModifierSpec mod in ts.Modifiers)
                {
                    if (mod is PointerSpec)
                        mt = mt.MakePointerType()!;
                    else if (mod is IArraySpec)
                    {
                        var spec = (mod as IArraySpec)!;
                        if (spec.IsBound)
                            return null;
                        if (spec.Rank == 1)
                            mt = mt.MakeArrayType();
                        else
                            mt = mt.MakeArrayType(spec.Rank);
                    }
                }
                if (ts.IsByRef)
                    mt = mt.MakeByRefType();
                result = mt as TypeBuilder;
                if (result == null)
                    return mt;
            }
            if (result != null && result.is_created)
                return result.CreateType();
            else
                return result;
        }

        internal int get_next_table_index(int table, int count)
        {
            if (table_indexes == null)
            {
                table_indexes = new int[64];
                for (int i = 0; i < 64; ++i)
                    table_indexes[i] = 1;
                /* allow room for .<Module> in TypeDef table */
                table_indexes[0x02] = 2;
            }
            // Console.WriteLine ("getindex for table "+table.ToString()+" got "+table_indexes [table].ToString());
            int index = table_indexes[table];
            table_indexes[table] += count;
            return index;
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);
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

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }
        /*
                internal ISymbolDocumentWriter? DefineDocument (string url, Guid language, Guid languageVendor, Guid documentType)
                {
                    if (symbolWriter != null)
                        return symbolWriter.DefineDocument (url, language, languageVendor, documentType);
                    else
                        return null;
                }
        */
        [RequiresUnreferencedCode("Types might be removed")]
        public override Type[] GetTypes()
        {
            if (types == null)
                return Type.EmptyTypes;

            int n = num_types;
            Type[] copy = new Type[n];
            Array.Copy(types, copy, n);

            // MS replaces the typebuilders with their created types
            for (int i = 0; i < copy.Length; ++i)
                if (types[i].is_created)
                    copy[i] = types[i].CreateType()!;

            return copy;
        }

        internal static int GetMethodToken(MethodInfo method)
        {
            ArgumentNullException.ThrowIfNull(method);

            return method.MetadataToken;
        }

        internal int GetArrayMethodToken(Type arrayClass, string methodName, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
        {
            return GetMethodToken(GetArrayMethod(arrayClass, methodName, callingConvention, returnType, parameterTypes));
        }

        internal static int GetConstructorToken(ConstructorInfo con)
        {
            ArgumentNullException.ThrowIfNull(con);

            return con.MetadataToken;
        }

        internal static int GetFieldToken(FieldInfo field)
        {
            ArgumentNullException.ThrowIfNull(field);

            return field.MetadataToken;
        }

        // FIXME:
        internal int GetSignatureToken(byte[] sigBytes, int sigLength)
        {
            throw new NotImplementedException();
        }

        internal int GetSignatureToken(SignatureHelper sigHelper)
        {
            ArgumentNullException.ThrowIfNull(sigHelper);
            return GetToken(sigHelper);
        }

        internal int GetStringConstant(string str)
        {
            ArgumentNullException.ThrowIfNull(str);
            return GetToken(str);
        }

        internal static int GetTypeToken(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            if (type.IsByRef)
                throw new ArgumentException("type can't be a byref type", nameof(type));
            return type.MetadataToken;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Reflection.Emit is not subject to trimming")]
        internal int GetTypeToken(string name)
        {
            return GetTypeToken(GetType(name)!);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int getUSIndex(ModuleBuilder mb, string str);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int getToken(ModuleBuilder mb, object obj, bool create_open_instance);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int getMethodToken(ModuleBuilder mb, MethodBase method,
                              Type[] opt_param_types);

        internal int GetToken(string str)
        {
            int result;
            if (!us_string_cache.TryGetValue(str, out result))
            {
                result = getUSIndex(this, str);
                us_string_cache[str] = result;
            }

            return result;
        }

        private static int typeref_tokengen = 0x01ffffff;
        private static int typedef_tokengen = 0x02ffffff;
        private static int typespec_tokengen = 0x1bffffff;
        private static int memberref_tokengen = 0x0affffff;
        private static int methoddef_tokengen = 0x06ffffff;
        private Dictionary<MemberInfo, int>? inst_tokens, inst_tokens_open;

        //
        // Assign a pseudo token to the various TypeBuilderInst objects, so the runtime
        // doesn't have to deal with them.
        // For Run assemblies, the tokens will not be fixed up, so the runtime will
        // still encounter these objects, it will resolve them by calling their
        // RuntimeResolve () methods.
        //
        private int GetPseudoToken(MemberInfo member, bool create_open_instance)
        {
            int token;
            Dictionary<MemberInfo, int>? dict = create_open_instance ? inst_tokens_open : inst_tokens;
            if (dict == null)
            {
                dict = new Dictionary<MemberInfo, int>(ReferenceEqualityComparer.Instance);
                if (create_open_instance)
                    inst_tokens_open = dict;
                else
                    inst_tokens = dict;
            }
            else if (dict.TryGetValue(member, out token))
            {
                return token;
            }

            // Count backwards to avoid collisions with the tokens
            // allocated by the runtime
            if (member is TypeBuilderInstantiation || member is SymbolType)
                token = typespec_tokengen--;
            else if (member is FieldOnTypeBuilderInst)
                token = memberref_tokengen--;
            else if (member is ConstructorOnTypeBuilderInst)
                token = memberref_tokengen--;
            else if (member is MethodOnTypeBuilderInst)
                token = memberref_tokengen--;
            else if (member is FieldBuilder)
                token = memberref_tokengen--;
            else if (member is TypeBuilder tb)
            {
                if (create_open_instance && tb.ContainsGenericParameters)
                    token = typespec_tokengen--;
                else if (member.Module == this)
                    token = typedef_tokengen--;
                else
                    token = typeref_tokengen--;
            }
            else if (member is EnumBuilder eb)
            {
                token = GetPseudoToken(eb.GetTypeBuilder(), create_open_instance);
                dict[member] = token;
                // n.b. don't register with the runtime, the TypeBuilder already did it.
                return token;
            }
            else if (member is ConstructorBuilder cb)
            {
                if (member.Module == this && !cb.TypeBuilder.ContainsGenericParameters)
                    token = methoddef_tokengen--;
                else
                    token = memberref_tokengen--;
            }
            else if (member is MethodBuilder mb)
            {
                if (member.Module == this && !mb.TypeBuilder.ContainsGenericParameters && !mb.IsGenericMethodDefinition)
                    token = methoddef_tokengen--;
                else
                    token = memberref_tokengen--;
            }
            else if (member is GenericTypeParameterBuilder)
            {
                token = typespec_tokengen--;
            }
            else
                throw new NotImplementedException();

            dict[member] = token;
            RegisterToken(member, token);
            return token;
        }

        internal int GetToken(MemberInfo member)
        {
            if (member is ConstructorBuilder || member is MethodBuilder || member is FieldBuilder)
                return GetPseudoToken(member, false);
            return getToken(this, member, true);
        }

        internal int GetToken(MemberInfo member, bool create_open_instance)
        {
            if (member is TypeBuilderInstantiation || member is FieldOnTypeBuilderInst || member is ConstructorOnTypeBuilderInst || member is MethodOnTypeBuilderInst || member is SymbolType || member is FieldBuilder || member is TypeBuilder || member is ConstructorBuilder || member is MethodBuilder || member is GenericTypeParameterBuilder ||
                member is EnumBuilder)
                return GetPseudoToken(member, create_open_instance);
            return getToken(this, member, create_open_instance);
        }

        internal int GetToken(MethodBase method, IEnumerable<Type> opt_param_types)
        {
            if (method is ConstructorBuilder || method is MethodBuilder)
                return GetPseudoToken(method, false);

            if (opt_param_types == null)
                return getToken(this, method, true);

            var optParamTypes = new List<Type>(opt_param_types);
            return getMethodToken(this, method, optParamTypes.ToArray());
        }

        internal int GetToken(MethodBase method, Type[] opt_param_types)
        {
            if (method is ConstructorBuilder || method is MethodBuilder)
                return GetPseudoToken(method, false);
            return getMethodToken(this, method, opt_param_types);
        }

        internal int GetToken(SignatureHelper helper)
        {
            return getToken(this, helper, true);
        }

        /*
         * Register the token->obj mapping with the runtime so the Module.Resolve...
         * methods will work for obj.
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern void RegisterToken(object obj, int token);

        /*
         * Returns MemberInfo registered with the given token.
         */
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern object GetRegisteredToken(int token);

        internal ITokenGenerator GetTokenGenerator() => token_gen ??= new ModuleBuilderTokenGenerator(this);

        // Called from the runtime to return the corresponding finished reflection object
        internal static object RuntimeResolve(object obj)
        {
            if (obj is MethodBuilder mb)
                return mb.RuntimeResolve();
            if (obj is ConstructorBuilder cb)
                return cb.RuntimeResolve();
            if (obj is FieldBuilder fb)
                return fb.RuntimeResolve();
            if (obj is GenericTypeParameterBuilder gtpb)
                return gtpb.RuntimeResolve();
            if (obj is FieldOnTypeBuilderInst fotbi)
                return fotbi.RuntimeResolve();
            if (obj is MethodOnTypeBuilderInst motbi)
                return motbi.RuntimeResolve();
            if (obj is ConstructorOnTypeBuilderInst cotbi)
                return cotbi.RuntimeResolve();
            if (obj is Type t)
                return t.RuntimeResolve();
            throw new NotImplementedException(obj.GetType().FullName);
        }

        internal string FileName
        {
            get
            {
                return fqname;
            }
        }

        internal bool IsMain
        {
            set
            {
                is_main = value;
            }
        }

        internal void CreateGlobalType()
        {
            global_type ??= new TypeBuilder(this, 0, 1, true);
        }

        public override Assembly Assembly
        {
            get { return assemblyb; }
        }

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        public override string Name
        {
            get { return name; }
        }

        public override string ScopeName
        {
            get { return name; }
        }

        public override Guid ModuleVersionId
        {
            get
            {
                return new Guid(guid);
            }
        }

        public override bool IsResource()
        {
            return false;
        }

        internal ModuleBuilder InternalModule => this;

        internal IntPtr GetUnderlyingNativeHandle() { return _impl; }

        protected override ModuleHandle GetModuleHandleImpl() => new ModuleHandle(_impl);

        [RequiresUnreferencedCode("Methods might be removed")]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            if (!global_type_created)
                return null;
            if (types == null)
                return global_type!.AsType().GetMethod(name);
            return global_type!.AsType().GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override FieldInfo? ResolveField(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            return RuntimeModule.ResolveField(this, _impl, metadataToken, genericTypeArguments, genericMethodArguments);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override MemberInfo? ResolveMember(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            return RuntimeModule.ResolveMember(this, _impl, metadataToken, genericTypeArguments, genericMethodArguments);
        }

        internal MemberInfo ResolveOrGetRegisteredToken(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            ResolveTokenError error;
            MemberInfo? m = RuntimeModule.ResolveMemberToken(_impl, metadataToken, RuntimeModule.ptrs_from_types(genericTypeArguments), RuntimeModule.ptrs_from_types(genericMethodArguments), out error);
            if (m != null)
                return m;

            m = GetRegisteredToken(metadataToken) as MemberInfo;
            if (m == null)
                throw RuntimeModule.resolve_token_exception(this, metadataToken, error, "MemberInfo");
            else
                return m;
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override MethodBase? ResolveMethod(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            return RuntimeModule.ResolveMethod(this, _impl, metadataToken, genericTypeArguments, genericMethodArguments);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override string ResolveString(int metadataToken)
        {
            return RuntimeModule.ResolveString(this, _impl, metadataToken);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override byte[] ResolveSignature(int metadataToken)
        {
            return RuntimeModule.ResolveSignature(this, _impl, metadataToken);
        }

        [RequiresUnreferencedCode("Trimming changes metadata tokens")]
        public override Type ResolveType(int metadataToken, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            return RuntimeModule.ResolveType(this, _impl, metadataToken, genericTypeArguments, genericMethodArguments);
        }

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return base.IsDefined(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return GetCustomAttributes(null!, inherit); // FIXME: coreclr doesn't allow null attributeType
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (cattrs == null || cattrs.Length == 0)
                return Array.Empty<object>();

            if (attributeType is TypeBuilder)
                throw new InvalidOperationException("First argument to GetCustomAttributes can't be a TypeBuilder");

            List<object> results = new List<object>();
            for (int i = 0; i < cattrs.Length; i++)
            {
                Type t = cattrs[i].Ctor.GetType();

                if (t is TypeBuilder)
                    throw new InvalidOperationException("Can't construct custom attribute for TypeBuilder type");

                if (attributeType == null || attributeType.IsAssignableFrom(t))
                    results.Add(cattrs[i].Invoke());
            }

            return results.ToArray();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttribute.GetCustomAttributesData(this);
        }

        [RequiresUnreferencedCode("Fields might be removed")]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            if (!global_type_created)
                throw new InvalidOperationException("Module-level fields cannot be retrieved until after the CreateGlobalFunctions method has been called for the module.");
            return global_type!.AsType().GetField(name, bindingAttr);
        }

        [RequiresUnreferencedCode("Fields might be removed")]
        public override FieldInfo[] GetFields(BindingFlags bindingFlags)
        {
            if (!global_type_created)
                throw new InvalidOperationException("Module-level fields cannot be retrieved until after the CreateGlobalFunctions method has been called for the module.");
            return global_type!.AsType().GetFields(bindingFlags);
        }

        [RequiresUnreferencedCode("Methods might be removed")]
        public override MethodInfo[] GetMethods(BindingFlags bindingFlags)
        {
            if (!global_type_created)
                throw new InvalidOperationException("Module-level methods cannot be retrieved until after the CreateGlobalFunctions method has been called for the module.");
            return global_type!.AsType().GetMethods(bindingFlags);
        }

        public override int MetadataToken
        {
            get
            {
                return RuntimeModule.get_MetadataToken(this);
            }
        }
    }

    internal sealed class ModuleBuilderTokenGenerator : ITokenGenerator
    {

        private ModuleBuilder mb;

        public ModuleBuilderTokenGenerator(ModuleBuilder mb)
        {
            this.mb = mb;
        }

        public int GetToken(string str)
        {
            return mb.GetToken(str);
        }

        public int GetToken(MemberInfo member, bool create_open_instance)
        {
            return mb.GetToken(member, create_open_instance);
        }

        public int GetToken(MethodBase method, Type[] opt_param_types)
        {
            return mb.GetToken(method, opt_param_types);
        }

        public int GetToken(SignatureHelper helper)
        {
            return mb.GetToken(helper);
        }
    }
}

#endif
