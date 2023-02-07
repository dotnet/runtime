// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed class MethodOnTypeBuilderInstantiation : MethodInfo
    {
        #region Internal Static Members
        internal static MethodInfo GetMethod(MethodInfo method, TypeBuilderInstantiation type)
        {
            return new MethodOnTypeBuilderInstantiation(method, type);
        }
        #endregion

        #region Private Data Members
        internal MethodInfo _method;
        private Type _type;
        // Below fields only used for mono
        private Type[]? _typeArguments;
        private MethodInfo? _genericMethodDefinition;
        #endregion

        #region Constructor
        internal MethodOnTypeBuilderInstantiation(MethodInfo method, Type type)
        {
            Debug.Assert(method is MethodBuilder || method is RuntimeMethodInfo);

            _method = method;
            _type = type;
        }

        internal MethodOnTypeBuilderInstantiation(MethodOnTypeBuilderInstantiation gmd, Type[] typeArguments)
            : this(gmd._method, gmd._type)
        {
            _typeArguments = new Type[typeArguments.Length];
            typeArguments.CopyTo(_typeArguments, 0);
            _genericMethodDefinition = gmd;
        }

        internal MethodOnTypeBuilderInstantiation(MethodInfo method, Type[] typeArguments)
            : this(ExtractBaseMethod(method), method.DeclaringType!)
        {
            _typeArguments = new Type[typeArguments.Length];
            typeArguments.CopyTo(_typeArguments, 0);
            if (_method != method)
                _genericMethodDefinition = method;
        }
        #endregion

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Reflection.Emit is not subject to trimming")]
        private static MethodInfo ExtractBaseMethod(MethodInfo info)
        {
            if (info is MethodBuilder)
                return info;
            if (info is MethodOnTypeBuilderInstantiation mbi)
                return mbi._method;

            if (info.IsGenericMethod)
                info = info.GetGenericMethodDefinition();

            Type t = info.DeclaringType!;
            if (!t.IsGenericType || t.IsGenericTypeDefinition)
                return info;

            return (MethodInfo)t.Module.ResolveMethod(info.MetadataToken)!;
        }

        #region MemberInfo Overrides
        public override MemberTypes MemberType => _method.MemberType;
        public override string Name => _method.Name;
        public override Type? DeclaringType => _type;
        public override Type? ReflectedType => _type;
        public override object[] GetCustomAttributes(bool inherit) { return _method.GetCustomAttributes(inherit); }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { return _method.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return _method.IsDefined(attributeType, inherit); }
        public override Module Module => _method.Module;
        #endregion

        #region MethodBase Members
        public override ParameterInfo[] GetParameters() { return _method.GetParameters(); }
        public override MethodImplAttributes GetMethodImplementationFlags() { return _method.GetMethodImplementationFlags(); }
        public override RuntimeMethodHandle MethodHandle => _method.MethodHandle;
        public override MethodAttributes Attributes => _method.Attributes;
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }
        public override CallingConventions CallingConvention => _method.CallingConvention;
        public override Type[] GetGenericArguments()
        {
#if MONO
            if (!_method.IsGenericMethodDefinition)
                return Type.EmptyTypes;
            Type[] source = _typeArguments ?? _method.GetGenericArguments();
            Type[] result = new Type[source.Length];
            source.CopyTo(result, 0);
            return result;
#else
            return _method.GetGenericArguments();
#endif
        }
        public override MethodInfo GetGenericMethodDefinition() { return _genericMethodDefinition ?? _method; }
        public override bool IsGenericMethodDefinition => _method.IsGenericMethodDefinition && _typeArguments == null;
        public override bool ContainsGenericParameters
        {
            get
            {
#if MONO
                if (_method.ContainsGenericParameters)
                    return true;
                if (!_method.IsGenericMethodDefinition)
                    throw new NotSupportedException();
                if (_typeArguments == null)
                    return true;
                foreach (Type t in _typeArguments)
                {
                    if (t.ContainsGenericParameters)
                        return true;
                }
                return false;
#else
                return _method.ContainsGenericParameters;
#endif
            }
        }

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] typeArgs)
        {
#if MONO
            if (!_method.IsGenericMethodDefinition || (_typeArguments != null))
                throw new InvalidOperationException("Method is not a generic method definition");

            ArgumentNullException.ThrowIfNull(typeArgs);

            if (_method.GetGenericArguments().Length != typeArgs.Length)
                throw new ArgumentException("Incorrect length", nameof(typeArgs));

            foreach (Type type in typeArgs)
            {
                ArgumentNullException.ThrowIfNull(type, nameof(typeArgs));
            }

            return new MethodOnTypeBuilderInstantiation(this, typeArgs);
#else
            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));

            return MethodBuilderInstantiation.MakeGenericMethod(this, typeArgs);
#endif
        }

        public override bool IsGenericMethod => _method.IsGenericMethod;

#endregion

        #region Public Abstract\Virtual Members
        public override Type ReturnType => _method.ReturnType;
        public override ParameterInfo ReturnParameter => throw new NotSupportedException();
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotSupportedException();
        public override MethodInfo GetBaseDefinition() { throw new NotSupportedException(); }
        #endregion

        #region Internal overrides
        internal override Type[] GetParameterTypes()
        {
            return _method.GetParameterTypes();
        }

#if MONO
        // Called from the runtime to return the corresponding finished MethodInfo object
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "MethodOnTypeBuilderInst is Reflection.Emit's underlying implementation of MakeGenericMethod. " +
                "Callers of the outer calls to MakeGenericMethod will be warned as appropriate.")]
        internal MethodInfo RuntimeResolve()
        {
            Type type = _type.InternalResolve();
            MethodInfo m = type.GetMethod(_method);
            if (_typeArguments != null)
            {
                var args = new Type[_typeArguments.Length];
                for (int i = 0; i < _typeArguments.Length; ++i)
                    args[i] = _typeArguments[i].InternalResolve();
                m = m.MakeGenericMethod(args);
            }
            return m;
        }

        internal override int GetParametersCount()
        {
            return _method.GetParametersCount();
        }
#endif
        #endregion

    }
}
