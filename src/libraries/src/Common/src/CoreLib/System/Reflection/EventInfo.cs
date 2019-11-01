// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

#if FEATURE_COMINTEROP
using EventRegistrationToken = System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken;
#endif //#if FEATURE_COMINTEROP

namespace System.Reflection
{
    public abstract class EventInfo : MemberInfo
    {
        protected EventInfo() { }

        public override MemberTypes MemberType => MemberTypes.Event;

        public abstract EventAttributes Attributes { get; }
        public bool IsSpecialName => (Attributes & EventAttributes.SpecialName) != 0;

        public MethodInfo[] GetOtherMethods() => GetOtherMethods(nonPublic: false);
        public virtual MethodInfo[] GetOtherMethods(bool nonPublic) { throw NotImplemented.ByDesign; }

        public virtual MethodInfo? AddMethod => GetAddMethod(nonPublic: true);
        public virtual MethodInfo? RemoveMethod => GetRemoveMethod(nonPublic: true);
        public virtual MethodInfo? RaiseMethod => GetRaiseMethod(nonPublic: true);

        public MethodInfo? GetAddMethod() => GetAddMethod(nonPublic: false);
        public MethodInfo? GetRemoveMethod() => GetRemoveMethod(nonPublic: false);
        public MethodInfo? GetRaiseMethod() => GetRaiseMethod(nonPublic: false);

        public abstract MethodInfo? GetAddMethod(bool nonPublic);
        public abstract MethodInfo? GetRemoveMethod(bool nonPublic);
        public abstract MethodInfo? GetRaiseMethod(bool nonPublic);

        public virtual bool IsMulticast
        {
            get
            {
                Type? cl = EventHandlerType;
                Type mc = typeof(MulticastDelegate);
                return mc.IsAssignableFrom(cl);
            }
        }

        public virtual Type? EventHandlerType
        {
            get
            {
                MethodInfo m = GetAddMethod(true)!;
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

        [DebuggerHidden]
        [DebuggerStepThrough]
        public virtual void AddEventHandler(object? target, Delegate? handler)
        {
            MethodInfo? addMethod = GetAddMethod(nonPublic: false);

            if (addMethod == null)
                throw new InvalidOperationException(SR.InvalidOperation_NoPublicAddMethod);

#if FEATURE_COMINTEROP
            if (addMethod.ReturnType == typeof(EventRegistrationToken))
                throw new InvalidOperationException(SR.InvalidOperation_NotSupportedOnWinRTEvent);
#endif //#if FEATURE_COMINTEROP

            addMethod.Invoke(target, new object?[] { handler });
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        public virtual void RemoveEventHandler(object? target, Delegate? handler)
        {
            MethodInfo? removeMethod = GetRemoveMethod(nonPublic: false);

            if (removeMethod == null)
                throw new InvalidOperationException(SR.InvalidOperation_NoPublicRemoveMethod);

#if FEATURE_COMINTEROP
            ParameterInfo[] parameters = removeMethod.GetParametersNoCopy();
            if (parameters[0].ParameterType == typeof(EventRegistrationToken))
                throw new InvalidOperationException(SR.InvalidOperation_NotSupportedOnWinRTEvent);
#endif //#if FEATURE_COMINTEROP

            removeMethod.Invoke(target, new object?[] { handler });
        }

        public override bool Equals(object? obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(EventInfo? left, EventInfo? right)
        {
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (right is null)
            {
                // return true/false not the test result https://github.com/dotnet/coreclr/issues/914
                return (left is null) ? true : false;
            }

            // Try fast reference equality and opposite null check prior to calling the slower virtual Equals
            if ((object?)left == (object)right)
            {
                return true;
            }

            return (left is null) ? false : left.Equals(right);
        }

        public static bool operator !=(EventInfo? left, EventInfo? right) => !(left == right);
    }
}
