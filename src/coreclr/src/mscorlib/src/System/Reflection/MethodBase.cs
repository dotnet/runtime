// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    //
    // Invocation cached flags. Those are used in unmanaged code as well
    // so be careful if you change them
    //
    [Flags]
    internal enum INVOCATION_FLAGS : uint
    {
        INVOCATION_FLAGS_UNKNOWN = 0x00000000,
        INVOCATION_FLAGS_INITIALIZED = 0x00000001,
        // it's used for both method and field to signify that no access is allowed
        INVOCATION_FLAGS_NO_INVOKE = 0x00000002,
        INVOCATION_FLAGS_NEED_SECURITY = 0x00000004,
        // Set for static ctors and ctors on abstract types, which
        // can be invoked only if the "this" object is provided (even if it's null).
        INVOCATION_FLAGS_NO_CTOR_INVOKE = 0x00000008,
        // because field and method are different we can reuse the same bits
        // method
        INVOCATION_FLAGS_IS_CTOR = 0x00000010,
        INVOCATION_FLAGS_RISKY_METHOD = 0x00000020,
        INVOCATION_FLAGS_NON_W8P_FX_API = 0x00000040,
        INVOCATION_FLAGS_IS_DELEGATE_CTOR = 0x00000080,
        INVOCATION_FLAGS_CONTAINS_STACK_POINTERS = 0x00000100,
        // field
        INVOCATION_FLAGS_SPECIAL_FIELD = 0x00000010,
        INVOCATION_FLAGS_FIELD_SPECIAL_CAST = 0x00000020,

        // temporary flag used for flagging invocation of method vs ctor
        // this flag never appears on the instance m_invocationFlag and is simply
        // passed down from within ConstructorInfo.Invoke()
        INVOCATION_FLAGS_CONSTRUCTOR_INVOKE = 0x10000000,
    }

    [Serializable]
    public abstract class MethodBase : MemberInfo
    {
        #region Static Members
        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));

            MethodBase m = RuntimeType.GetMethodBase(handle.GetMethodInfo());

            Type declaringType = m.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType)
                throw new ArgumentException(String.Format(
                    CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_MethodDeclaringTypeGeneric"),
                    m, declaringType.GetGenericTypeDefinition()));

            return m;
        }

        public static MethodBase GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));

            return RuntimeType.GetMethodBase(declaringType.GetRuntimeType(), handle.GetMethodInfo());
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        public static MethodBase GetCurrentMethod()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return RuntimeMethodInfo.InternalGetCurrentMethod(ref stackMark);
        }
        #endregion

        #region Constructor
        protected MethodBase() { }
        #endregion

        public static bool operator ==(MethodBase left, MethodBase right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            MethodInfo method1, method2;
            ConstructorInfo constructor1, constructor2;

            if ((method1 = left as MethodInfo) != null && (method2 = right as MethodInfo) != null)
                return method1 == method2;
            else if ((constructor1 = left as ConstructorInfo) != null && (constructor2 = right as ConstructorInfo) != null)
                return constructor1 == constructor2;

            return false;
        }

        public static bool operator !=(MethodBase left, MethodBase right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region Internal Members
        // used by EE
        private IntPtr GetMethodDesc() { return MethodHandle.Value; }

#if FEATURE_APPX
#endif
        #endregion

        #region Public Abstract\Virtual Members
        internal virtual ParameterInfo[] GetParametersNoCopy() { return GetParameters(); }

        [System.Diagnostics.Contracts.Pure]
        public abstract ParameterInfo[] GetParameters();

        public virtual MethodImplAttributes MethodImplementationFlags
        {
            get
            {
                return GetMethodImplementationFlags();
            }
        }

        public abstract MethodImplAttributes GetMethodImplementationFlags();

        public abstract RuntimeMethodHandle MethodHandle { get; }

        public abstract MethodAttributes Attributes { get; }

        public abstract Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture);

        public virtual CallingConventions CallingConvention { get { return CallingConventions.Standard; } }

        public virtual Type[] GetGenericArguments() { throw new NotSupportedException(Environment.GetResourceString("NotSupported_SubclassOverride")); }

        public virtual bool IsGenericMethodDefinition { get { return false; } }

        public virtual bool ContainsGenericParameters { get { return false; } }

        public virtual bool IsGenericMethod { get { return false; } }

        public virtual bool IsSecurityCritical { get { throw new NotImplementedException(); } }

        public virtual bool IsSecuritySafeCritical { get { throw new NotImplementedException(); } }

        public virtual bool IsSecurityTransparent { get { throw new NotImplementedException(); } }

        #endregion

        #region Public Members
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public Object Invoke(Object obj, Object[] parameters)
        {
            // Theoretically we should set up a LookForMyCaller stack mark here and pass that along.
            // But to maintain backward compatibility we can't switch to calling an 
            // internal overload that takes a stack mark.
            // Fortunately the stack walker skips all the reflection invocation frames including this one.
            // So this method will never be returned by the stack walker as the caller.
            // See SystemDomain::CallersMethodCallbackWithStackMark in AppDomain.cpp.
            return Invoke(obj, BindingFlags.Default, null, parameters, null);
        }

        public bool IsPublic { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public; } }

        public bool IsPrivate { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private; } }

        public bool IsFamily { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family; } }

        public bool IsAssembly { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly; } }

        public bool IsFamilyAndAssembly { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem; } }

        public bool IsFamilyOrAssembly { get { return (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem; } }

        public bool IsStatic { get { return (Attributes & MethodAttributes.Static) != 0; } }

        public bool IsFinal
        {
            get { return (Attributes & MethodAttributes.Final) != 0; }
        }
        public bool IsVirtual
        {
            get { return (Attributes & MethodAttributes.Virtual) != 0; }
        }
        public bool IsHideBySig { get { return (Attributes & MethodAttributes.HideBySig) != 0; } }

        public bool IsAbstract { get { return (Attributes & MethodAttributes.Abstract) != 0; } }

        public bool IsSpecialName { get { return (Attributes & MethodAttributes.SpecialName) != 0; } }

        public bool IsConstructor
        {
            get
            {
                // To be backward compatible we only return true for instance RTSpecialName ctors.
                return (this is ConstructorInfo &&
                        !IsStatic &&
                        ((Attributes & MethodAttributes.RTSpecialName) == MethodAttributes.RTSpecialName));
            }
        }

        public virtual MethodBody GetMethodBody()
        {
            throw new InvalidOperationException();
        }
        #endregion

        #region Internal Methods
        // helper method to construct the string representation of the parameter list

        internal static string ConstructParameters(Type[] parameterTypes, CallingConventions callingConvention, bool serialization)
        {
            StringBuilder sbParamList = new StringBuilder();
            string comma = "";

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                Type t = parameterTypes[i];

                sbParamList.Append(comma);

                string typeName = t.FormatTypeName(serialization);

                // Legacy: Why use "ByRef" for by ref parameters? What language is this? 
                // VB uses "ByRef" but it should precede (not follow) the parameter name.
                // Why don't we just use "&"?
                if (t.IsByRef && !serialization)
                {
                    sbParamList.Append(typeName.TrimEnd('&'));
                    sbParamList.Append(" ByRef");
                }
                else
                {
                    sbParamList.Append(typeName);
                }

                comma = ", ";
            }

            if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
            {
                sbParamList.Append(comma);
                sbParamList.Append("...");
            }

            return sbParamList.ToString();
        }

        internal string FullName
        {
            get
            {
                return String.Format("{0}.{1}", DeclaringType.FullName, FormatNameAndSig());
            }
        }
        internal string FormatNameAndSig()
        {
            return FormatNameAndSig(false);
        }

        internal virtual string FormatNameAndSig(bool serialization)
        {
            // Serialization uses ToString to resolve MethodInfo overloads.
            StringBuilder sbName = new StringBuilder(Name);

            sbName.Append("(");
            sbName.Append(ConstructParameters(GetParameterTypes(), CallingConvention, serialization));
            sbName.Append(")");

            return sbName.ToString();
        }

        internal virtual Type[] GetParameterTypes()
        {
            ParameterInfo[] paramInfo = GetParametersNoCopy();

            Type[] parameterTypes = new Type[paramInfo.Length];
            for (int i = 0; i < paramInfo.Length; i++)
                parameterTypes[i] = paramInfo[i].ParameterType;

            return parameterTypes;
        }

        internal Object[] CheckArguments(Object[] parameters, Binder binder,
            BindingFlags invokeAttr, CultureInfo culture, Signature sig)
        {
            // copy the arguments in a different array so we detach from any user changes 
            Object[] copyOfParameters = new Object[parameters.Length];

            ParameterInfo[] p = null;
            for (int i = 0; i < parameters.Length; i++)
            {
                Object arg = parameters[i];
                RuntimeType argRT = sig.Arguments[i];

                if (arg == Type.Missing)
                {
                    if (p == null)
                        p = GetParametersNoCopy();
                    if (p[i].DefaultValue == System.DBNull.Value)
                        throw new ArgumentException(Environment.GetResourceString("Arg_VarMissNull"), nameof(parameters));
                    arg = p[i].DefaultValue;
                }
                copyOfParameters[i] = argRT.CheckValue(arg, binder, culture, invokeAttr);
            }

            return copyOfParameters;
        }
        #endregion
    }
}
