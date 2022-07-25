// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Reflection.Emit.DynamicMethod.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//   Zoltan Varga (vargaz@freemail.hu)
//
// (C) 2003 Ximian, Inc.  http://www.ximian.com
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

using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{

    [ComVisible(true)]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class DynamicMethod : MethodInfo
    {
#region Sync with MonoReflectionDynamicMethod in object-internals.h
        private RuntimeMethodHandle mhandle;
        private string name;
        private Type returnType;
        private Type[]? parameters;
        private MethodAttributes attributes;
        private CallingConventions callingConvention;
        private Module module;
        private bool skipVisibility;
        private bool init_locals = true;
        private ILGenerator? ilgen;
        private int nrefs;
        private object?[]? refs;
        private IntPtr referenced_by;
        private Type? owner;
#endregion

        private Delegate? deleg;
        private RuntimeMethodInfo? method;
        private ParameterBuilder[]? pinfo;
        internal bool creating;
        private DynamicILInfo? il_info;

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Module m) : this(name, returnType, parameterTypes, m, false)
        {
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Type owner) : this(name, returnType, parameterTypes, owner, false)
        {
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Module m, bool skipVisibility) : this(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, m, skipVisibility)
        {
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, Type owner, bool skipVisibility) : this(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, skipVisibility)
        {
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type owner, bool skipVisibility) : this(name, attributes, callingConvention, returnType, parameterTypes, owner, owner?.Module, skipVisibility, false, true)
        {
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Module m, bool skipVisibility) : this(name, attributes, callingConvention, returnType, parameterTypes, null, m, skipVisibility, false, false)
        {
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes) : this(name, returnType, parameterTypes, false)
        {
        }

        // FIXME: "Visibility is not restricted"
        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name, Type? returnType, Type[]? parameterTypes, bool restrictedSkipVisibility)
            : this(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, null, null, restrictedSkipVisibility, true, false)
        {
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        [DynamicDependency(nameof(owner))]  // Automatically keeps all previous fields too due to StructLayout
        private DynamicMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes, Type? owner, Module? m, bool skipVisibility, bool anonHosted, bool typeOwner)
        {
            ArgumentNullException.ThrowIfNull(name);
            returnType ??= typeof(void);
            if (typeOwner)
                ArgumentNullException.ThrowIfNull(owner);
            if (!anonHosted)
                ArgumentNullException.ThrowIfNull(m);
            if (parameterTypes != null)
            {
                for (int i = 0; i < parameterTypes.Length; ++i)
                    if (parameterTypes[i] == null)
                        throw new ArgumentException($"Parameter {i} is null");
            }
            if (owner != null && (owner.IsArray || owner.IsInterface))
            {
                throw new ArgumentException("Owner can't be an array or an interface.");
            }

            m ??= AnonHostModuleHolder.AnonHostModule;

            this.name = name;
            this.attributes = attributes | MethodAttributes.Static;
            this.callingConvention = callingConvention;
            this.returnType = returnType;
            this.parameters = parameterTypes;
            this.owner = owner;
            this.module = m;
            this.skipVisibility = skipVisibility;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void create_dynamic_method(DynamicMethod m);

        private void CreateDynMethod()
        {
            // Clearing of ilgen in create_dynamic_method is not yet synchronized for multiple threads
            lock (this)
            {
                if (mhandle.Value == IntPtr.Zero)
                {
                    if (ilgen == null || ilgen.ILOffset == 0)
                        throw new InvalidOperationException("Method '" + name + "' does not have a method body.");

                    ilgen.label_fixup(this);

                    // Have to create all DynamicMethods referenced by this one
                    try
                    {
                        // Used to avoid cycles
                        creating = true;
                        if (refs != null)
                        {
                            for (int i = 0; i < refs.Length; ++i)
                            {
                                if (refs[i] is DynamicMethod m)
                                {
                                    if (!m.creating)
                                        m.CreateDynMethod();
                                }
                            }
                        }
                    }
                    finally
                    {
                        creating = false;
                    }
                    create_dynamic_method(this);
                    ilgen = null;
                }
            }
        }

        public sealed override Delegate CreateDelegate(Type delegateType)
        {
            ArgumentNullException.ThrowIfNull(delegateType);
            if (deleg != null)
                return deleg;

            CreateDynMethod();

            deleg = Delegate.CreateDelegate(delegateType, null, this);
            return deleg;
        }

        public sealed override Delegate CreateDelegate(Type delegateType, object? target)
        {
            ArgumentNullException.ThrowIfNull(delegateType);

            CreateDynMethod();

            /* Can't cache the delegate since it is different for each target */
            return Delegate.CreateDelegate(delegateType, target, this);
        }

        public ParameterBuilder? DefineParameter(int position, ParameterAttributes attributes, string? parameterName)
        {
            //
            // Extension: Mono allows position == 0 for the return attribute
            //
            if ((position < 0) || (position > parameters!.Length))
                throw new ArgumentOutOfRangeException(nameof(position));

            RejectIfCreated();

            ParameterBuilder pb = new ParameterBuilder(this, position, attributes, parameterName);
            pinfo ??= new ParameterBuilder[parameters.Length + 1];
            pinfo[position] = pb;
            return pb;
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            // support for MethodImplAttribute PCA
            return new object[] { new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags()) };
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
            {
                // avoid calling CreateInstance() in the common case where the type is Attribute
                object[] result = attributeType == typeof(Attribute) ? new Attribute[1] : (object[])Array.CreateInstance(attributeType, 1);
                result[0] = new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags());
                return result;
            }

            return (object[])Array.CreateInstance(attributeType.IsValueType || attributeType.ContainsGenericParameters ? typeof(object) : attributeType, 0);
        }

        public DynamicILInfo GetDynamicILInfo() => il_info ??= new DynamicILInfo(this);

        public ILGenerator GetILGenerator()
        {
            return GetILGenerator(64);
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            if (((GetMethodImplementationFlags() & MethodImplAttributes.CodeTypeMask) !=
                 MethodImplAttributes.IL) ||
                ((GetMethodImplementationFlags() & MethodImplAttributes.ManagedMask) !=
                 MethodImplAttributes.Managed))
                throw new InvalidOperationException("Method body should not exist.");
            if (ilgen != null)
                return ilgen;
            ilgen = new ILGenerator(Module, new DynamicMethodTokenGenerator(this), streamSize);
            return ilgen;
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return MethodImplAttributes.IL | MethodImplAttributes.Managed | MethodImplAttributes.NoInlining;
        }

        public override ParameterInfo[] GetParameters()
        {
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
            return parameters == null ? 0 : parameters.Length;
        }

        internal override Type GetParameterType(int pos)
        {
            return parameters![pos];
        }

        /*
        public override object Invoke (object obj, object[] parameters) {
            CreateDynMethod ();
            method ??= new RuntimeMethodInfo (mhandle);
            return method.Invoke (obj, parameters);
        }
        */

        public override object? Invoke(object? obj, BindingFlags invokeAttr,
                                       Binder? binder, object?[]? parameters,
                                       CultureInfo? culture)
        {
            try
            {
                CreateDynMethod();
                method ??= new RuntimeMethodInfo(mhandle);

                return method.Invoke(obj, invokeAttr, binder, parameters, culture);
            }
            catch (MethodAccessException mae)
            {
                throw new TargetInvocationException("Method cannot be invoked.", mae);
            }
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            var sbName = new ValueStringBuilder(MethodNameBufferSize);
            sbName.Append(ReturnType.FormatTypeName());
            sbName.Append(' ');
            sbName.Append(Name);
            sbName.Append('(');
            AppendParameters(ref sbName, parameters ?? Type.EmptyTypes, CallingConvention);
            sbName.Append(')');
            return sbName.ToString();
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return attributes;
            }
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                return callingConvention;
            }
        }

        public override Type? DeclaringType
        {
            get
            {
                return null;
            }
        }

        public bool InitLocals
        {
            get
            {
                return init_locals;
            }
            set
            {
                init_locals = value;
            }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                return mhandle;
            }
        }

        public override Module Module
        {
            get
            {
                return module;
            }
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override Type? ReflectedType
        {
            get
            {
                return null;
            }
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                if (deleg == null)
                {
                    return new RuntimeParameterInfo((ParameterBuilder?)null, returnType, this, -1);
                }
                return deleg.Method.ReturnParameter;
            }
        }

        public override Type ReturnType
        {
            get
            {
                return returnType;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

        /*
                public override int MetadataToken {
                    get {
                        return 0;
                    }
                }
        */

        private void RejectIfCreated()
        {
            if (mhandle.Value != IntPtr.Zero)
                throw new InvalidOperationException("Type definition of the method is complete.");
        }

        internal int AddRef(object reference)
        {
            refs ??= new object?[4];
            if (nrefs >= refs.Length - 1)
            {
                object[] new_refs = new object[refs.Length * 2];
                Array.Copy(refs, new_refs, refs.Length);
                refs = new_refs;
            }
            refs[nrefs] = reference;
            /* Reserved by the runtime */
            refs[nrefs + 1] = null;
            nrefs += 2;
            return nrefs - 1;
        }

        // This class takes care of constructing the module in a thread safe manner
        private static class AnonHostModuleHolder
        {
            public static readonly Module anon_host_module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName() { Name = "Anonymously Hosted DynamicMethods Assembly" }, AssemblyBuilderAccess.Run).ManifestModule;

            public static Module AnonHostModule
            {
                get
                {
                    return anon_host_module;
                }
            }
        }
    }

    internal sealed class DynamicMethodTokenGenerator : ITokenGenerator
    {

        private DynamicMethod m;

        public DynamicMethodTokenGenerator(DynamicMethod m)
        {
            this.m = m;
        }

        public int GetToken(string str)
        {
            return m.AddRef(str);
        }

        public int GetToken(MethodBase method, Type[] opt_param_types)
        {
            throw new InvalidOperationException();
        }

        public int GetToken(MemberInfo member, bool create_open_instance)
        {
            return m.AddRef(member);
        }

        public int GetToken(SignatureHelper helper)
        {
            return m.AddRef(helper);
        }
    }
}

#endif
