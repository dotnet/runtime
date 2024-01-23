// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed partial class ConstructorOnTypeBuilderInstantiation : ConstructorInfo
    {
        #region Private Static Members
        internal static ConstructorInfo GetConstructor(ConstructorInfo constructor, TypeBuilderInstantiation type)
        {
            return new ConstructorOnTypeBuilderInstantiation(constructor, type);
        }
        #endregion

        #region Private Data Members
        internal ConstructorInfo _ctor;
        private TypeBuilderInstantiation _type;
        #endregion

        #region Constructor
        internal ConstructorOnTypeBuilderInstantiation(ConstructorInfo constructor, TypeBuilderInstantiation type)
        {
            _ctor = constructor;
            _type = type;
        }
        #endregion

        #region Internal Overrides
        internal override Type[] GetParameterTypes()
        {
            return _ctor.GetParameterTypes();
        }
        #endregion

        #region MemberInfo Overrides
        public override MemberTypes MemberType => _ctor.MemberType;
        public override string Name => _ctor.Name;
        public override Type? DeclaringType => _type;
        public override Type? ReflectedType => _type;
        public override object[] GetCustomAttributes(bool inherit) { return _ctor.GetCustomAttributes(inherit); }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { return _ctor.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return _ctor.IsDefined(attributeType, inherit); }
        public override int MetadataToken => _ctor.MetadataToken;
        public override Module Module => _ctor.Module;
        #endregion

        #region MethodBase Members
        public override ParameterInfo[] GetParameters() { return _ctor.GetParameters(); }
        public override MethodImplAttributes GetMethodImplementationFlags() { return _ctor.GetMethodImplementationFlags(); }
        public override RuntimeMethodHandle MethodHandle => _ctor.MethodHandle;
        public override MethodAttributes Attributes => _ctor.Attributes;
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }
        public override CallingConventions CallingConvention => _ctor.CallingConvention;
        public override Type[] GetGenericArguments() { return _ctor.GetGenericArguments(); }
        public override bool IsGenericMethodDefinition => false;
        public override bool ContainsGenericParameters => _ctor.ContainsGenericParameters;

        public override bool IsGenericMethod => false;
        #endregion

        #region ConstructorInfo Members
        public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new InvalidOperationException();
        }
        #endregion
    }
}
