// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    public abstract partial class Delegate : ICloneable, ISerializable
    {
        public virtual object Clone() => MemberwiseClone();

        [return: NotNullIfNotNull(nameof(a))]
        [return: NotNullIfNotNull(nameof(b))]
        public static Delegate? Combine(Delegate? a, Delegate? b)
        {
            if (a is null)
                return b;

            return a.CombineImpl(b);
        }

        public static Delegate? Combine(params Delegate?[]? delegates)
        {
            if (delegates == null || delegates.Length == 0)
                return null;

            Delegate? d = delegates[0];
            for (int i = 1; i < delegates.Length; i++)
                d = Combine(d, delegates[i]);

            return d;
        }

        // V2 api: Creates open or closed delegates to static or instance methods - relaxed signature checking allowed.
        public static Delegate CreateDelegate(Type type, object? firstArgument, MethodInfo method) => CreateDelegate(type, firstArgument, method, throwOnBindFailure: true)!;

        // V1 api: Creates open delegates to static or instance methods - relaxed signature checking allowed.
        public static Delegate CreateDelegate(Type type, MethodInfo method) => CreateDelegate(type, method, throwOnBindFailure: true)!;

        // V1 api: Creates closed delegates to instance methods only, relaxed signature checking disallowed.
        [RequiresUnreferencedCode("The target method might be removed")]
        public static Delegate CreateDelegate(Type type, object target, string method) => CreateDelegate(type, target, method, ignoreCase: false, throwOnBindFailure: true)!;
        [RequiresUnreferencedCode("The target method might be removed")]
        public static Delegate CreateDelegate(Type type, object target, string method, bool ignoreCase) => CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure: true)!;

        // V1 api: Creates open delegates to static methods only, relaxed signature checking disallowed.
        public static Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method) => CreateDelegate(type, target, method, ignoreCase: false, throwOnBindFailure: true)!;
        public static Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method, bool ignoreCase) => CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure: true)!;

        protected virtual Delegate CombineImpl(Delegate? d) => throw new MulticastNotSupportedException(SR.Multicast_Combine);

        protected virtual Delegate? RemoveImpl(Delegate d) => d.Equals(this) ? null : this;

        public virtual Delegate[] GetInvocationList() => new Delegate[] { this };

        public object? DynamicInvoke(params object?[]? args)
        {
            return DynamicInvokeImpl(args);
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) => throw new PlatformNotSupportedException();

        public MethodInfo Method => GetMethodImpl();

        public static Delegate? Remove(Delegate? source, Delegate? value)
        {
            if (source == null)
                return null;

            if (value == null)
                return source;

            if (!InternalEqualTypes(source, value))
                throw new ArgumentException(SR.Arg_DlgtTypeMis);

            return source.RemoveImpl(value);
        }

        public static Delegate? RemoveAll(Delegate? source, Delegate? value)
        {
            Delegate? newDelegate;

            do
            {
                newDelegate = source;
                source = Remove(source, value);
            }
            while (newDelegate != source);

            return newDelegate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Delegate? d1, Delegate? d2)
        {
            // Test d2 first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (d2 is null)
            {
                return d1 is null;
            }

            return ReferenceEquals(d2, d1) ? true : d2.Equals((object?)d1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Delegate? d1, Delegate? d2)
        {
            // Test d2 first to allow branch elimination when inlined for not null checks (!= null)
            // so it can become a simple test
            if (d2 is null)
            {
                return d1 is not null;
            }

            return ReferenceEquals(d2, d1) ? false : !d2.Equals(d1);
        }
    }
}
