// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

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
                ReadOnlySpan<ParameterInfo> p = m.GetParametersAsSpan();
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

            addMethod.Invoke(target, new object?[] { handler });
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        public virtual void RemoveEventHandler(object? target, Delegate? handler)
        {
            MethodInfo? removeMethod = GetRemoveMethod(nonPublic: false);

            if (removeMethod == null)
                throw new InvalidOperationException(SR.InvalidOperation_NoPublicRemoveMethod);

            removeMethod.Invoke(target, new object?[] { handler });
        }

        public override bool Equals(object? obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(EventInfo? left, EventInfo? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return left is not null && left.Equals(right);
        }

        public static bool operator !=(EventInfo? left, EventInfo? right) => !(left == right);
    }
}
