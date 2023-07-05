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
        private RuntimeMethodHandle _mhandle;
        private RuntimeType _returnType;
        private RuntimeType[] _parameterTypes;
        private Module _module;
        private bool _skipVisibility;
        private bool _restrictedSkipVisibility;
        private bool _initLocals;
        private RuntimeILGenerator? _ilGenerator;
        private int _nrefs;
        private object?[]? _refs;
        private IntPtr _referencedBy;
        private RuntimeType? _typeOwner;
#endregion

        private string _name;
        private MethodAttributes _attributes;
        private CallingConventions _callingConvention;
        private RuntimeParameterInfo[]? _parameters;
        private Delegate? _deleg;
        private RuntimeMethodInfo? _method;
        private bool _creating;
        private DynamicILInfo? _dynamicILInfo;

        private object? _methodHandle; // unused

        public sealed override Delegate CreateDelegate(Type delegateType)
        {
            ArgumentNullException.ThrowIfNull(delegateType);
            if (_deleg is not null)
                return _deleg;

            CreateDynMethod();

            return _deleg = Delegate.CreateDelegate(delegateType, null, this);
        }

        public sealed override Delegate CreateDelegate(Type delegateType, object? target)
        {
            ArgumentNullException.ThrowIfNull(delegateType);

            CreateDynMethod();

            /* Can't cache the delegate since it is different for each target */
            return Delegate.CreateDelegate(delegateType, target, this);
        }

        public DynamicILInfo GetDynamicILInfo() => _dynamicILInfo ??= new DynamicILInfo(this);

        public ILGenerator GetILGenerator(int streamSize) => GetILGeneratorInternal(streamSize);

        internal RuntimeILGenerator GetRuntimeILGenerator() => GetILGeneratorInternal(64);

        private RuntimeILGenerator GetILGeneratorInternal(int streamSize) =>
            _ilGenerator ??= new RuntimeILGenerator(Module, new DynamicMethodTokenGenerator(this), streamSize);

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(SR.NotSupported_CallToVarArg);

            try
            {
                CreateDynMethod();
                _method ??= new RuntimeMethodInfo(_mhandle);

                return _method.Invoke(obj, invokeAttr, binder, parameters, culture);
            }
            catch (MethodAccessException mae)
            {
                throw new TargetInvocationException(SR.TargetInvocation_MethodCannotBeInvoked, mae);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void create_dynamic_method(DynamicMethod m, string name, MethodAttributes attributes, CallingConventions callingConvention);

        private void CreateDynMethod()
        {
            // Clearing of ilgen in create_dynamic_method is not yet synchronized for multiple threads
            lock (this)
            {
                if (_mhandle.Value == IntPtr.Zero)
                {
                    if (_ilGenerator == null || _ilGenerator.ILOffset == 0)
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, Name));

                    _ilGenerator.label_fixup(this);

                    // Have to create all DynamicMethods referenced by this one
                    try
                    {
                        // Used to avoid cycles
                        _creating = true;
                        if (_refs != null)
                        {
                            for (int i = 0; i < _refs.Length; ++i)
                            {
                                if (_refs[i] is DynamicMethod m)
                                {
                                    if (!m._creating)
                                        m.CreateDynMethod();
                                }
                            }
                        }
                    }
                    finally
                    {
                        _creating = false;
                    }
                    create_dynamic_method(this, Name, Attributes, CallingConvention);
                    _ilGenerator = null;
                }
            }
        }

        private int AddRef(object reference)
        {
            _refs ??= new object?[4];
            if (_nrefs >= _refs.Length - 1)
            {
                object[] new_refs = new object[_refs.Length * 2];
                Array.Copy(_refs, new_refs, _refs.Length);
                _refs = new_refs;
            }
            _refs[_nrefs] = reference;
            /* Reserved by the runtime */
            _refs[_nrefs + 1] = null;
            _nrefs += 2;
            return _nrefs - 1;
        }

        internal override int GetParametersCount() => GetParametersNoCopy().Length;

        private sealed class DynamicMethodTokenGenerator : ITokenGenerator
        {
            private readonly DynamicMethod _m;

            public DynamicMethodTokenGenerator(DynamicMethod m)
            {
                _m = m;
            }

            public int GetToken(string str)
            {
                return _m.AddRef(str);
            }

            public int GetToken(MethodBase method, Type[] opt_paratypes)
            {
                throw new InvalidOperationException();
            }

            public int GetToken(MemberInfo member, bool create_open_instance)
            {
                return _m.AddRef(member);
            }

            public int GetToken(SignatureHelper helper)
            {
                return _m.AddRef(helper);
            }
        }
    }
}
