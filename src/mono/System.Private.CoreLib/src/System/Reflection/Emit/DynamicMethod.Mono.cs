// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed partial class DynamicMethod : MethodInfo
    {
#region Sync with MonoReflectionDynamicMethod in object-internals.h
        private RuntimeMethodHandle mhandle;
        private RuntimeType returnType;
        private RuntimeType[] parameterTypes;
        private Module module;
        private bool skipVisibility;
        private bool restrictedSkipVisibility;
        private bool initLocals;
        private ILGenerator? ilGenerator;
        private int nrefs;
        private object?[]? refs;
        private IntPtr referenced_by;
        private RuntimeType? typeOwner;
#endregion
        // We want the creator of the DynamicMethod to control who has access to the
        // DynamicMethod (just like we do for delegates). However, a user can get to
        // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc.
        // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would
        // not be able to bound access to the DynamicMethod. Hence, we need to ensure that
        // we do not allow direct use of RTDynamicMethod.
        private RTDynamicMethod dynMethod;

        private Delegate? deleg;
        private RuntimeMethodInfo? method;
        private bool creating;
        private DynamicILInfo? dynamicILInfo;

        private object? methodHandle; // unused

        public sealed override Delegate CreateDelegate(Type delegateType)
        {
            ArgumentNullException.ThrowIfNull(delegateType);
            if (deleg is not null)
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

        public DynamicILInfo GetDynamicILInfo() => dynamicILInfo ??= new DynamicILInfo(this);

        public ILGenerator GetILGenerator(int streamSize) =>
            ilGenerator ??= new ILGenerator(Module, new DynamicMethodTokenGenerator(this), streamSize);

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(SR.NotSupported_CallToVarArg);

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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void create_dynamic_method(DynamicMethod m, string name, MethodAttributes attributes, CallingConventions callingConvention);

        private void CreateDynMethod()
        {
            // Clearing of ilgen in create_dynamic_method is not yet synchronized for multiple threads
            lock (this)
            {
                if (mhandle.Value == IntPtr.Zero)
                {
                    if (ilGenerator == null || ilGenerator.ILOffset == 0)
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, Name));

                    ilGenerator.label_fixup(this);

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
                    create_dynamic_method(this, Name, Attributes, CallingConvention);
                    ilGenerator = null;
                }
            }
        }

        private int AddRef(object reference)
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

        internal override ParameterInfo[] GetParametersNoCopy() => dynMethod.GetParametersNoCopy();

        internal override int GetParametersCount() => GetParametersNoCopy().Length;

        private sealed class DynamicMethodTokenGenerator : ITokenGenerator
        {
            private readonly DynamicMethod m;

            public DynamicMethodTokenGenerator(DynamicMethod m)
            {
                this.m = m;
            }

            public int GetToken(string str)
            {
                return m.AddRef(str);
            }

            public int GetToken(MethodBase method, Type[] opt_paratypes)
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
}
