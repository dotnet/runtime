// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit/MethodBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class MethodBuilder : MethodInfo
    {
#region Sync with MonoReflectionMethodBuilder in object-internals.h
        private RuntimeMethodHandle mhandle;
        private Type? rtype;
        internal Type[]? parameters;
        private MethodAttributes attrs;
        private MethodImplAttributes iattrs;
        private string name;
        private int table_idx;
        private byte[]? code;
        private ILGenerator? ilgen;
        private TypeBuilder type;
        internal ParameterBuilder[]? pinfo;
        private CustomAttributeBuilder[]? cattrs;
        private MethodInfo[]? override_methods;
        private string? pi_dll;
        private string? pi_entry;
        private CharSet charset;
        private uint extra_flags; /* this encodes set_last_error etc */
        private CallingConvention native_cc;
        private CallingConventions call_conv;
        private bool init_locals = true;
        private IntPtr generic_container;
        internal GenericTypeParameterBuilder[]? generic_params;
        private Type[]? returnModReq;
        private Type[]? returnModOpt;
        private Type[][]? paramModReq;
        private Type[][]? paramModOpt;
#endregion

        private RuntimeMethodInfo? created;

        [DynamicDependency(nameof(paramModOpt))]  // Automatically keeps all previous fields too due to StructLayout
        internal MethodBuilder(TypeBuilder tb, string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? returnModReq, Type[]? returnModOpt, Type[]? parameterTypes, Type[][]? paramModReq, Type[][]? paramModOpt)
        {
            this.name = name;
            this.attrs = attributes;
            this.call_conv = callingConvention;
            this.rtype = returnType;
            this.returnModReq = returnModReq;
            this.returnModOpt = returnModOpt;
            this.paramModReq = paramModReq;
            this.paramModOpt = paramModOpt;
            // The MSDN docs does not specify this, but the MS MethodBuilder
            // appends a HasThis flag if the method is not static
            if ((attributes & MethodAttributes.Static) == 0)
                this.call_conv |= CallingConventions.HasThis;
            if (parameterTypes != null)
            {
                for (int i = 0; i < parameterTypes.Length; ++i)
                    if (parameterTypes[i] == null)
                        throw new ArgumentException("Elements of the parameterTypes array cannot be null", nameof(parameterTypes));

                this.parameters = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, this.parameters, parameterTypes.Length);
            }
            type = tb;
            table_idx = get_next_table_index(0x06, 1);

            ((ModuleBuilder)tb.Module).RegisterToken(this, MetadataToken);
        }

        internal MethodBuilder(TypeBuilder tb, string name, MethodAttributes attributes,
                                CallingConventions callingConvention, Type? returnType, Type[]? returnModReq, Type[]? returnModOpt, Type[]? parameterTypes, Type[][]? paramModReq, Type[][]? paramModOpt,
            string dllName, string entryName, CallingConvention nativeCConv, CharSet nativeCharset)
            : this(tb, name, attributes, callingConvention, returnType, returnModReq, returnModOpt, parameterTypes, paramModReq, paramModOpt)
        {
            pi_dll = dllName;
            pi_entry = entryName;
            native_cc = nativeCConv;
            charset = nativeCharset;
        }

        public override bool ContainsGenericParameters
        {
            get { throw new NotSupportedException(); }
        }

        public bool InitLocals
        {
            get { return init_locals; }
            set { init_locals = value; }
        }

        internal TypeBuilder TypeBuilder
        {
            get { return type; }
        }

        public override int MetadataToken => 0x06000000 | table_idx;

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw NotSupported();
            }
        }

        internal RuntimeMethodHandle MethodHandleInternal
        {
            get
            {
                return mhandle;
            }
        }

        public override Type ReturnType
        {
            get { return rtype!; }
        }

        public override Type? ReflectedType
        {
            get { return type; }
        }

        public override Type? DeclaringType
        {
            get { return type; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override MethodAttributes Attributes
        {
            get { return attrs; }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { return null!; } // FIXME: coreclr returns an empty instance
        }

        public override CallingConventions CallingConvention
        {
            get { return call_conv; }
        }

        /* Used by mcs */
        internal bool BestFitMapping
        {
            set
            {
                extra_flags = (uint)((extra_flags & ~0x30) | (uint)(value ? 0x10 : 0x20));
            }
        }

        /* Used by mcs */
        internal bool ThrowOnUnmappableChar
        {
            set
            {
                extra_flags = (uint)((extra_flags & ~0x3000) | (uint)(value ? 0x1000 : 0x2000));
            }
        }

        /* Used by mcs */
        internal bool ExactSpelling
        {
            set
            {
                extra_flags = (uint)((extra_flags & ~0x01) | (uint)(value ? 0x01 : 0x00));
            }
        }

        /* Used by mcs */
        internal bool SetLastError
        {
            set
            {
                extra_flags = (uint)((extra_flags & ~0x40) | (uint)(value ? 0x40 : 0x00));
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return iattrs;
        }

        public override ParameterInfo[] GetParameters()
        {
            if (!type.is_created)
                throw NotSupported();

            return GetParametersInternal();
        }

        internal override ParameterInfo[] GetParametersInternal()
        {
            if (parameters == null)
                return Array.Empty<ParameterInfo>();

            ParameterInfo[] retval = new ParameterInfo[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                retval[i] = RuntimeParameterInfo.New(pinfo?[i + 1], parameters[i], this, i + 1);
            }
            return retval;
        }

        internal override int GetParametersCount()
        {
            if (parameters == null)
                return 0;

            return parameters.Length;
        }

        internal override Type GetParameterType(int pos)
        {
            return parameters![pos];
        }

        internal MethodBase RuntimeResolve()
        {
            return type.RuntimeResolve().GetMethod(this);
        }

        internal Module GetModule()
        {
            return type.Module;
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw NotSupported();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw NotSupported();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            /*
             * On MS.NET, this always returns not_supported, but we can't do this
             * since there would be no way to obtain custom attributes of
             * dynamically created ctors.
             */
            if (type.is_created)
                return CustomAttribute.GetCustomAttributes(this, inherit);
            else
                throw NotSupported();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (type.is_created)
                return CustomAttribute.GetCustomAttributes(this, attributeType, inherit);
            else
                throw NotSupported();
        }

        public ILGenerator GetILGenerator()
        {
            return GetILGenerator(64);
        }

        public ILGenerator GetILGenerator(int size)
        {
            if (((iattrs & MethodImplAttributes.CodeTypeMask) !=
                 MethodImplAttributes.IL) ||
                ((iattrs & MethodImplAttributes.ManagedMask) !=
                 MethodImplAttributes.Managed))
                throw new InvalidOperationException("Method body should not exist.");
            if (ilgen != null)
                return ilgen;
            ilgen = new ILGenerator(type.Module, ((ModuleBuilder)type.Module).GetTokenGenerator(), size);
            return ilgen;
        }

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string strParamName)
        {
            RejectIfCreated();

            //
            // Extension: Mono allows position == 0 for the return attribute
            //
            if ((position < 0) || parameters == null || (position > parameters.Length))
                throw new ArgumentOutOfRangeException(nameof(position));

            ParameterBuilder pb = new ParameterBuilder(this, position, attributes, strParamName);
            if (pinfo == null)
                pinfo = new ParameterBuilder[parameters.Length + 1];
            pinfo[position] = pb;
            return pb;
        }

        internal void check_override()
        {
            if (override_methods != null)
            {
                foreach (MethodInfo m in override_methods)
                {
                    if (m.IsVirtual && !IsVirtual)
                        throw new TypeLoadException(string.Format("Method '{0}' override '{1}' but it is not virtual", name, m));
                }
            }
        }

        internal void fixup()
        {
            if (((attrs & (MethodAttributes.Abstract | MethodAttributes.PinvokeImpl)) == 0) && ((iattrs & (MethodImplAttributes.Runtime | MethodImplAttributes.InternalCall)) == 0))
            {
                // do not allow zero length method body on MS.NET 2.0 (and higher)
                if (((ilgen == null) || (ilgen.ILOffset == 0)) && (code == null || code.Length == 0))
                    throw new InvalidOperationException(
                                         string.Format("Method '{0}.{1}' does not have a method body.",
                                                DeclaringType!.FullName, Name));
            }
            if (ilgen != null)
                ilgen.label_fixup(this);
        }

        internal void ResolveUserTypes()
        {
            rtype = TypeBuilder.ResolveUserType(rtype);
            TypeBuilder.ResolveUserTypes(parameters);
            TypeBuilder.ResolveUserTypes(returnModReq);
            TypeBuilder.ResolveUserTypes(returnModOpt);
            if (paramModReq != null)
            {
                foreach (Type[] types in paramModReq)
                    TypeBuilder.ResolveUserTypes(types);
            }
            if (paramModOpt != null)
            {
                foreach (Type[] types in paramModOpt)
                    TypeBuilder.ResolveUserTypes(types);
            }
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            switch (customBuilder.Ctor.ReflectedType!.FullName)
            {
                case "System.Runtime.CompilerServices.MethodImplAttribute":
                    byte[] data = customBuilder.Data;
                    int impla; // the (stupid) ctor takes a short or an int ...
                    impla = (int)data[2];
                    impla |= ((int)data[3]) << 8;
                    iattrs |= (MethodImplAttributes)impla;
                    return;

                case "System.Runtime.InteropServices.DllImportAttribute":
                    CustomAttributeBuilder.CustomAttributeInfo attr = CustomAttributeBuilder.decode_cattr(customBuilder);
                    bool preserveSig = true;

                    /*
                     * It would be easier to construct a DllImportAttribute from
                     * the custom attribute builder, but the DllImportAttribute
                     * does not contain all the information required here, ie.
                     * - some parameters, like BestFitMapping has three values
                     *   ("on", "off", "missing"), but DllImportAttribute only
                     *   contains two (on/off).
                     * - PreserveSig is true by default, while it is false by
                     *   default in DllImportAttribute.
                     */

                    pi_dll = (string?)attr.ctorArgs[0];
                    if (pi_dll == null || pi_dll.Length == 0)
                        throw new ArgumentException("DllName cannot be empty");

                    native_cc = Runtime.InteropServices.CallingConvention.Winapi;

                    for (int i = 0; i < attr.namedParamNames.Length; ++i)
                    {
                        string name = attr.namedParamNames[i];
                        object? value = attr.namedParamValues[i];

                        if (name == "CallingConvention")
                            native_cc = (CallingConvention)value!;
                        else if (name == "CharSet")
                            charset = (CharSet)value!;
                        else if (name == "EntryPoint")
                            pi_entry = (string)value!;
                        else if (name == "ExactSpelling")
                            ExactSpelling = (bool)value!;
                        else if (name == "SetLastError")
                            SetLastError = (bool)value!;
                        else if (name == "PreserveSig")
                            preserveSig = (bool)value!;
                        else if (name == "BestFitMapping")
                            BestFitMapping = (bool)value!;
                        else if (name == "ThrowOnUnmappableChar")
                            ThrowOnUnmappableChar = (bool)value!;
                    }

                    attrs |= MethodAttributes.PinvokeImpl;
                    if (preserveSig)
                        iattrs |= MethodImplAttributes.PreserveSig;
                    return;

                case "System.Runtime.InteropServices.PreserveSigAttribute":
                    iattrs |= MethodImplAttributes.PreserveSig;
                    return;
                case "System.Runtime.CompilerServices.SpecialNameAttribute":
                    attrs |= MethodAttributes.SpecialName;
                    return;
                case "System.Security.SuppressUnmanagedCodeSecurityAttribute":
                    attrs |= MethodAttributes.HasSecurity;
                    break;
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

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);
            SetCustomAttribute(new CustomAttributeBuilder(con, binaryAttribute));
        }

        public void SetImplementationFlags(MethodImplAttributes attributes)
        {
            RejectIfCreated();
            iattrs = attributes;
        }

        public override string ToString()
        {
            return "MethodBuilder [" + type.Name + "::" + name + "]";
        }

        // FIXME:
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        internal override int get_next_table_index(int table, int count)
        {
            return type.get_next_table_index(table, count);
        }

        private static void ExtendArray<T>([NotNull] ref T[]? array, T elem)
        {
            if (array == null)
            {
                array = new T[1];
            }
            else
            {
                var newa = new T[array.Length + 1];
                Array.Copy(array, newa, array.Length);
                array = newa;
            }
            array[array.Length - 1] = elem;
        }

        internal void set_override(MethodInfo mdecl)
        {
            ExtendArray<MethodInfo>(ref override_methods, mdecl);
        }

        private void RejectIfCreated()
        {
            if (type.is_created)
                throw new InvalidOperationException("Type definition of the method is complete.");
        }

        private static Exception NotSupported()
        {
            return new NotSupportedException("The invoked member is not supported in a dynamic module.");
        }

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException("Method is not a generic method definition");
            ArgumentNullException.ThrowIfNull(typeArguments);
            foreach (Type type in typeArguments)
            {
                ArgumentNullException.ThrowIfNull(type, nameof(typeArguments));
            }

            return new MethodOnTypeBuilderInst(this, typeArguments);
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return generic_params != null;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                return generic_params != null;
            }
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException();

            return this;
        }

        public override Type[] GetGenericArguments()
        {
            if (generic_params == null)
                return Type.EmptyTypes;

            Type[] result = new Type[generic_params.Length];
            for (int i = 0; i < generic_params.Length; i++)
                result[i] = generic_params[i];

            return result;
        }

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            ArgumentNullException.ThrowIfNull(names);
            if (names.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(names));
            type.check_not_created();
            generic_params = new GenericTypeParameterBuilder[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string item = names[i];
                if (item == null)
                    throw new ArgumentNullException(nameof(names));
                generic_params[i] = new GenericTypeParameterBuilder(type, this, item, i);
            }

            return generic_params;
        }

        public void SetReturnType(Type? returnType)
        {
            rtype = returnType;
        }

        public void SetParameters(params Type[]? parameterTypes)
        {
            if (parameterTypes != null)
            {
                for (int i = 0; i < parameterTypes.Length; ++i)
                    if (parameterTypes[i] == null)
                        throw new ArgumentNullException(nameof(parameterTypes), "Elements of the parameterTypes array cannot be null");

                this.parameters = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, this.parameters, parameterTypes.Length);
            }
        }

        public void SetSignature(Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            SetReturnType(returnType);
            SetParameters(parameterTypes);
            this.returnModReq = returnTypeRequiredCustomModifiers;
            this.returnModOpt = returnTypeOptionalCustomModifiers;
            this.paramModReq = parameterTypeRequiredCustomModifiers;
            this.paramModOpt = parameterTypeOptionalCustomModifiers;
        }

        public override Module Module
        {
            get
            {
                return GetModule();
            }
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                if (!type.is_created)
                    throw new InvalidOperationException(SR.InvalidOperation_TypeNotCreated);
                created ??= (RuntimeMethodInfo)GetMethodFromHandle(mhandle)!;
                return created.ReturnParameter;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ExceptionHandler : IEquatable<ExceptionHandler>
    {
        internal readonly int m_exceptionClass;
        internal readonly int m_tryStartOffset;
        internal readonly int m_tryEndOffset;
        internal readonly int m_filterOffset;
        internal readonly int m_handlerStartOffset;
        internal readonly int m_handlerEndOffset;
        internal readonly ExceptionHandlingClauseOptions m_kind;

        public int ExceptionTypeToken
        {
            get { return m_exceptionClass; }
        }

        public int TryOffset
        {
            get { return m_tryStartOffset; }
        }

        public int TryLength
        {
            get { return m_tryEndOffset - m_tryStartOffset; }
        }

        public int FilterOffset
        {
            get { return m_filterOffset; }
        }

        public int HandlerOffset
        {
            get { return m_handlerStartOffset; }
        }

        public int HandlerLength
        {
            get { return m_handlerEndOffset - m_handlerStartOffset; }
        }

        public ExceptionHandlingClauseOptions Kind
        {
            get { return m_kind; }
        }

        internal ExceptionHandler(int tryStartOffset, int tryEndOffset, int filterOffset, int handlerStartOffset, int handlerEndOffset,
            int kind, int exceptionTypeToken)
        {
            m_tryStartOffset = tryStartOffset;
            m_tryEndOffset = tryEndOffset;
            m_filterOffset = filterOffset;
            m_handlerStartOffset = handlerStartOffset;
            m_handlerEndOffset = handlerEndOffset;
            m_kind = (ExceptionHandlingClauseOptions)kind;
            m_exceptionClass = exceptionTypeToken;
        }

        private static bool IsValidKind(ExceptionHandlingClauseOptions kind)
        {
            switch (kind)
            {
                case ExceptionHandlingClauseOptions.Clause:
                case ExceptionHandlingClauseOptions.Filter:
                case ExceptionHandlingClauseOptions.Finally:
                case ExceptionHandlingClauseOptions.Fault:
                    return true;

                default:
                    return false;
            }
        }

        public override int GetHashCode()
        {
            return m_exceptionClass ^ m_tryStartOffset ^ m_tryEndOffset ^ m_filterOffset ^ m_handlerStartOffset ^ m_handlerEndOffset ^ (int)m_kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is ExceptionHandler && Equals((ExceptionHandler)obj);
        }

        public bool Equals(ExceptionHandler other)
        {
            return
                other.m_exceptionClass == m_exceptionClass &&
                other.m_tryStartOffset == m_tryStartOffset &&
                other.m_tryEndOffset == m_tryEndOffset &&
                other.m_filterOffset == m_filterOffset &&
                other.m_handlerStartOffset == m_handlerStartOffset &&
                other.m_handlerEndOffset == m_handlerEndOffset &&
                other.m_kind == m_kind;
        }

        public static bool operator ==(ExceptionHandler left, ExceptionHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ExceptionHandler left, ExceptionHandler right)
        {
            return !left.Equals(right);
        }
    }
}
#endif
