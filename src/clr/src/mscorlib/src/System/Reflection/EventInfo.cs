// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection
{
    [Serializable]
    public abstract class EventInfo : MemberInfo
    {
        #region Constructor
        protected EventInfo() { }
        #endregion

        public static bool operator ==(EventInfo left, EventInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null ||
                left is RuntimeEventInfo || right is RuntimeEventInfo)
            {
                return false;
            }
            return left.Equals(right);
        }

        public static bool operator !=(EventInfo left, EventInfo right)
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

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return MemberTypes.Event; } }
        #endregion

        #region Public Abstract\Virtual Members
        public virtual MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            throw new NotImplementedException();
        }

        public abstract MethodInfo GetAddMethod(bool nonPublic);

        public abstract MethodInfo GetRemoveMethod(bool nonPublic);

        public abstract MethodInfo GetRaiseMethod(bool nonPublic);

        public abstract EventAttributes Attributes { get; }
        #endregion

        #region Public Members
        public virtual MethodInfo AddMethod
        {
            get
            {
                return GetAddMethod(true);
            }
        }

        public virtual MethodInfo RemoveMethod
        {
            get
            {
                return GetRemoveMethod(true);
            }
        }

        public virtual MethodInfo RaiseMethod
        {
            get
            {
                return GetRaiseMethod(true);
            }
        }

        public MethodInfo[] GetOtherMethods() { return GetOtherMethods(false); }

        public MethodInfo GetAddMethod() { return GetAddMethod(false); }

        public MethodInfo GetRemoveMethod() { return GetRemoveMethod(false); }

        public MethodInfo GetRaiseMethod() { return GetRaiseMethod(false); }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public virtual void AddEventHandler(Object target, Delegate handler)
        {
            MethodInfo addMethod = GetAddMethod();

            if (addMethod == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoPublicAddMethod"));

#if FEATURE_COMINTEROP
            if (addMethod.ReturnType == typeof(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotSupportedOnWinRTEvent"));

            // Must be a normal non-WinRT event
            Debug.Assert(addMethod.ReturnType == typeof(void));
#endif // FEATURE_COMINTEROP

            addMethod.Invoke(target, new object[] { handler });
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public virtual void RemoveEventHandler(Object target, Delegate handler)
        {
            MethodInfo removeMethod = GetRemoveMethod();

            if (removeMethod == null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NoPublicRemoveMethod"));

#if FEATURE_COMINTEROP
            ParameterInfo[] parameters = removeMethod.GetParametersNoCopy();
            Debug.Assert(parameters != null && parameters.Length == 1);

            if (parameters[0].ParameterType == typeof(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotSupportedOnWinRTEvent"));

            // Must be a normal non-WinRT event
            Debug.Assert(parameters[0].ParameterType.BaseType == typeof(MulticastDelegate));
#endif // FEATURE_COMINTEROP

            removeMethod.Invoke(target, new object[] { handler });
        }

        public virtual Type EventHandlerType
        {
            get
            {
                MethodInfo m = GetAddMethod(true);

                ParameterInfo[] p = m.GetParametersNoCopy();

                Type del = typeof(Delegate);

                for (int i = 0; i < p.Length; i++)
                {
                    Type c = p[i].ParameterType;

                    if (c.IsSubclassOf(del))
                        return c;
                }
                return null;
            }
        }
        public bool IsSpecialName
        {
            get
            {
                return (Attributes & EventAttributes.SpecialName) != 0;
            }
        }

        public virtual bool IsMulticast
        {
            get
            {
                Type cl = EventHandlerType;
                Type mc = typeof(MulticastDelegate);
                return mc.IsAssignableFrom(cl);
            }
        }
        #endregion
    }
}
