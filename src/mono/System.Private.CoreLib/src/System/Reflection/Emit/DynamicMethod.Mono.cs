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

        /// <summary>
        /// Completes the dynamic method and creates a delegate that can be used to execute it.
        /// </summary>
        /// <param name="delegateType">A delegate type whose signature matches that of the dynamic method.</param>
        /// <returns>A delegate of the specified type, which can be used to execute the dynamic method.</returns>
        /// <exception cref="InvalidOperationException">The dynamic method has no method body.</exception>
        /// <exception cref="ArgumentException"><paramref name="delegateType" /> has the wrong number of parameters or the wrong parameter types.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        public sealed override Delegate CreateDelegate(Type delegateType)
        {
            ArgumentNullException.ThrowIfNull(delegateType);
            if (_deleg is not null)
                return _deleg;

            CreateDynMethod();

            return _deleg = Delegate.CreateDelegate(delegateType, null, this);
        }

        /// <summary>
        /// Completes the dynamic method and creates a delegate that can be used to execute it, specifying the delegate type and an object the delegate is bound to.
        /// </summary>
        /// <param name="delegateType">A delegate type whose signature matches that of the dynamic method, minus the first parameter.</param>
        /// <param name="target">An object the delegate is bound to. Must be of the same type as the first parameter of the dynamic method.</param>
        /// <returns>A delegate of the specified type, which can be used to execute the dynamic method with the specified target object.</returns>
        /// <exception cref="InvalidOperationException">The dynamic method has no method body.</exception>
        /// <exception cref="ArgumentException"><paramref name="delegateType" /> has the wrong number of parameters or the wrong parameter types.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        public sealed override Delegate CreateDelegate(Type delegateType, object? target)
        {
            ArgumentNullException.ThrowIfNull(delegateType);

            CreateDynMethod();

            /* Can't cache the delegate since it is different for each target */
            return Delegate.CreateDelegate(delegateType, target, this);
        }

        /// <summary>
        /// Returns a <see cref="DynamicILInfo" /> object that can be used to generate a method body from metadata tokens, scopes, and Microsoft intermediate language (MSIL) streams.
        /// </summary>
        /// <returns>A <see cref="DynamicILInfo" /> object that can be used to generate a method body from metadata tokens, scopes, and MSIL streams.</returns>
        public DynamicILInfo GetDynamicILInfo() => _dynamicILInfo ??= new DynamicILInfo(this);

        /// <summary>
        /// Returns a Microsoft intermediate language (MSIL) generator for the method with the specified MSIL stream size.
        /// </summary>
        /// <param name="streamSize">The size of the MSIL stream, in bytes.</param>
        /// <returns>An <see cref="ILGenerator" /> object for the method.</returns>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        public ILGenerator GetILGenerator(int streamSize) => GetILGeneratorInternal(streamSize);

        internal RuntimeILGenerator GetRuntimeILGenerator() => GetILGeneratorInternal(64);

        private RuntimeILGenerator GetILGeneratorInternal(int streamSize) =>
            _ilGenerator ??= new RuntimeILGenerator(Module, new DynamicMethodTokenGenerator(this), streamSize);

        /// <summary>
        /// Invokes the dynamic method using the specified parameters, under the constraints of the specified binder, with the specified culture information.
        /// </summary>
        /// <param name="obj">This parameter is ignored for dynamic methods, because they are static. Specify <see langword="null" />.</param>
        /// <param name="invokeAttr">A bitwise combination of <see cref="BindingFlags" /> values.</param>
        /// <param name="binder">A <see cref="Binder" /> object that enables the binding, coercion of argument types, invocation of members, and retrieval of <see cref="System.Reflection.MemberInfo" /> objects through reflection. If <paramref name="binder" /> is <see langword="null" />, the default binder is used.</param>
        /// <param name="parameters">An argument list. This is an array of arguments with the same number, order, and type as the parameters of the method to be invoked. If there are no parameters this parameter should be <see langword="null" />.</param>
        /// <param name="culture">An instance of <see cref="CultureInfo" /> used to govern the coercion of types. If this is <see langword="null" />, the <see cref="CultureInfo" /> for the current thread is used.</param>
        /// <returns>An <see cref="object" /> containing the return value of the invoked method.</returns>
        /// <exception cref="TargetParameterCountException">The number of elements in <paramref name="parameters" /> does not match the number of parameters in the dynamic method.</exception>
        /// <exception cref="NotSupportedException">The dynamic method contains unverifiable code.</exception>
        /// <exception cref="InvalidOperationException">The dynamic method has no method body.</exception>
        /// <remarks>
        /// For more information about this API, see <see href="https://raw.githubusercontent.com/dotnet/docs/main/docs/fundamentals/runtime-libraries/system-reflection-emit-dynamicmethod.md">Supplemental API remarks for DynamicMethod</see>.
        /// </remarks>
        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(SR.NotSupported_CallToVarArg);

            try
            {
                CreateDynMethod();
                return GetRuntimeMethodInfo().Invoke(obj, invokeAttr, binder, parameters, culture);
            }
            catch (MethodAccessException mae)
            {
                throw new TargetInvocationException(SR.TargetInvocation_MethodCannotBeInvoked, mae);
            }
        }

        internal RuntimeMethodInfo GetRuntimeMethodInfo()
        {
            _method ??= new RuntimeMethodInfo(_mhandle);
            return _method;
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

        internal override int GetParametersCount() => GetParametersAsSpan().Length;

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
