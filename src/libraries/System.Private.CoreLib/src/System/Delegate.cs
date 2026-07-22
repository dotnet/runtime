// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public abstract partial class Delegate : ICloneable, ISerializable
    {
        /// <summary>
        /// Gets a value that indicates whether the <see cref="Delegate"/> has a single invocation target.
        /// </summary>
        /// <value>true if the <see cref="Delegate"/> has a single invocation target.</value>
        public partial bool HasSingleTarget { get; }

        // V2 api: Creates open or closed delegates to static or instance methods - relaxed signature checking allowed.
        public static Delegate CreateDelegate(Type type, object? firstArgument, MethodInfo method) =>
            CreateDelegate(type, firstArgument, method, throwOnBindFailure: true)!;

        // V1 api: Creates open delegates to static or instance methods - relaxed signature checking allowed.
        public static Delegate CreateDelegate(Type type, MethodInfo method) =>
            CreateDelegate(type, method, throwOnBindFailure: true)!;

        // V1 api: Creates closed delegates to instance methods only, relaxed signature checking disallowed.
        [RequiresUnreferencedCode("The target method might be removed")]
        public static Delegate CreateDelegate(Type type, object target, string method) =>
            CreateDelegate(type, target, method, ignoreCase: false, throwOnBindFailure: true)!;
        [RequiresUnreferencedCode("The target method might be removed")]
        public static Delegate CreateDelegate(Type type, object target, string method, bool ignoreCase) =>
            CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure: true)!;

        // V1 api: Creates open delegates to static methods only, relaxed signature checking disallowed.
        public static Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllMethods)] Type target, string method) =>
            CreateDelegate(type, target, method, ignoreCase: false, throwOnBindFailure: true)!;
        public static Delegate CreateDelegate(Type type, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllMethods)] Type target, string method, bool ignoreCase) =>
            CreateDelegate(type, target, method, ignoreCase, throwOnBindFailure: true)!;

        internal object GetTargetForSingleCastInstanceDelegate()
        {
            Debug.Assert(HasSingleTarget && Target == _target && _target != null);
            return _target;
        }

        /// <summary>
        /// Gets an enumerator for the invocation targets of this delegate.
        /// </summary>
        /// <typeparam name="TDelegate">Delegate type being enumerated.</typeparam>
        /// <param name="d">The delegate being enumerated.</param>
        /// <returns>A <see cref="InvocationListEnumerator{TDelegate}" /> that follows the IEnumerable pattern and
        /// thus can be used in a C# 'foreach' statement to retrieve the invocation targets of this delegate without allocations.
        /// The method returns an empty enumerator for <see langword="null" /> delegate.</returns>
        /// <remarks>
        /// The order of the delegates returned by the enumerator is the same order in which the current delegate invokes the methods that those delegates represent.
        /// The method returns an empty enumerator for null delegate.
        /// </remarks>
        public static InvocationListEnumerator<TDelegate> EnumerateInvocationList<TDelegate>(TDelegate? d) where TDelegate : Delegate
            => new InvocationListEnumerator<TDelegate>(Unsafe.As<MulticastDelegate>(d));

        /// <summary>
        /// Provides an enumerator for the invocation list of a delegate.
        /// </summary>
        /// <typeparam name="TDelegate">Delegate type being enumerated.</typeparam>
        public struct InvocationListEnumerator<TDelegate> where TDelegate : Delegate
        {
            private readonly MulticastDelegate? _delegate;
            private int _index;
            private TDelegate? _current;

            internal InvocationListEnumerator(MulticastDelegate? d)
            {
                _delegate = d;
                _index = -1;
            }

            /// <summary>
            /// Implements the IEnumerator pattern.
            /// </summary>
            public TDelegate Current
            {
                get => _current!;
            }

            /// <summary>
            /// Implements the IEnumerator pattern.
            /// </summary>
            /// <returns><see langword="true" /> if the enumerator was successfully advanced to the next element;
            /// otherwise, <see langword="false" /> if the enumerator has passed the end of the collection. </returns>
            public bool MoveNext()
            {
                int index = _index + 1;
                if ((_current = Unsafe.As<TDelegate>(_delegate?.TryGetAt(index))) == null)
                {
                    return false;
                }
                _index = index;
                return true;
            }

            /// <summary>
            /// Implement IEnumerable.GetEnumerator() to return 'this' as the IEnumerator.
            /// </summary>
            /// <returns>An IEnumerator instance that can be used to iterate through the invocation targets of the delegate.</returns>
            [EditorBrowsable(EditorBrowsableState.Never)] // Only here to make foreach work
            public InvocationListEnumerator<TDelegate> GetEnumerator() => this;
        }

        public object? DynamicInvoke(params object?[]? args)
        {
            return DynamicInvokeImpl(args);
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) => throw new SerializationException(SR.Serialization_DelegatesNotSupported);

        public MethodInfo Method => GetMethodImpl();

        public virtual object Clone() => MemberwiseClone();

        [return: NotNullIfNotNull(nameof(a))]
        [return: NotNullIfNotNull(nameof(b))]
        public static Delegate? Combine(Delegate? a, Delegate? b)
        {
            return a is null ? b : a.CombineImpl(b);
        }

        public static Delegate? Combine(params Delegate?[]? delegates) =>
            Combine((ReadOnlySpan<Delegate?>)delegates);

        /// <summary>
        /// Concatenates the invocation lists of an span of delegates.
        /// </summary>
        /// <param name="delegates">The span of delegates to combine.</param>
        /// <returns>
        /// A new delegate with an invocation list that concatenates the invocation lists of the delegates in the <paramref name="delegates"/> span.
        /// Returns <see langword="null" /> if <paramref name="delegates"/> is <see langword="null" />,
        /// if <paramref name="delegates"/> contains zero elements, or if every entry in <paramref name="delegates"/> is <see langword="null" />.
        /// </returns>
        public static Delegate? Combine(params ReadOnlySpan<Delegate?> delegates)
        {
            if (delegates.IsEmpty)
            {
                return null;
            }

            Delegate? combined = delegates[0];
            foreach (Delegate? del in delegates[1..])
            {
                combined = Combine(combined, del);
            }

            return combined;
        }

        public static Delegate? Remove(Delegate? source, Delegate? value)
        {
            if (source == null)
                return null;

            if (value == null)
                return source;

            if (!RuntimeHelpers.TypeEquivalent(source, value))
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

            return ReferenceEquals(d2, d1) || d2.Equals(d1);
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

            return !ReferenceEquals(d2, d1) && !d2.Equals(d1);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The trimmer will never remove the Invoke method from delegates.")]
        internal static MethodInfo GetInvokeMethod(Type delegateType)
        {
            Debug.Assert(delegateType.IsAssignableTo(typeof(Delegate)));
            Debug.Assert(delegateType != typeof(Delegate));
            Debug.Assert(delegateType != typeof(MulticastDelegate));

            MethodInfo? invoke = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(invoke is not null);

            return invoke;
        }

#if !MONO
        internal struct Wrapper(Delegate? value) : IEquatable<Wrapper>
        {
            internal Delegate? Value = value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool Equals(Wrapper other)
            {
                // we should never get null here
                Debug.Assert(Value is not null);
                Debug.Assert(other.Value is not null);

                // use EqualsCore since the type is always the same here
                return ReferenceEquals(Value, other.Value) || Value.EqualsCore(other.Value);
            }

            public override readonly bool Equals(object? obj)
            {
                // we should never get another type here
                Debug.Assert(obj is Wrapper);
                return Equals((Wrapper)obj);
            }

            public override readonly int GetHashCode()
            {
                // we should never get null here
                Debug.Assert(Value is not null);
                return Value.GetHashCode();
            }
        }

        public partial bool HasSingleTarget => _helperObject is null || _helperObject.GetType() != typeof(Wrapper[]);

        // This method returns the Invocation list of this multicast delegate.
        public Delegate[] GetInvocationList()
        {
            if (!TryGetInvocations(out ReadOnlySpan<Wrapper> invocations))
            {
                return [this];
            }

            Delegate[] invocationList = new Delegate[invocations.Length];
            for (int i = 0; i < invocations.Length; i++)
            {
                invocationList[i] = invocations[i].Value!;
            }
            return invocationList;
        }

        // Used by delegate invocation list enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Delegate? TryGetAt(int index)
        {
            if (TryGetInvocations(out ReadOnlySpan<Wrapper> invocations))
            {
                if ((uint)index < (uint)invocations.Length)
                    return invocations[index].Value;
            }
            else if (index == 0)
            {
                return this;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TrySetSlot(ref Delegate? d, Delegate o)
        {
            Delegate? previous = d;
            if (previous is null)
            {
                previous = Interlocked.CompareExchange(ref d, o, null);
                if (previous == null)
                    return true;
            }

            // The slot may be already set because we have added and removed the same method before.
            // Optimize this case, because it's cheaper than copying the array.
            return SlotEquals(previous, o);
        }

        // This method will combine this delegate with the passed delegate
        //    to form a new delegate.
        protected Delegate CombineImpl(Delegate? d)
        {
            if (d is null)
                return this;

            // Verify that the types are the same...
            if (!RuntimeHelpers.TypeEquivalent(this, d))
                throw new ArgumentException(SR.Arg_DlgtTypeMis);

            Wrapper wrapper = new Wrapper(d);
            ReadOnlySpan<Wrapper> followList = d.TryGetInvocations(out ReadOnlySpan<Wrapper> span) ? span : new ReadOnlySpan<Wrapper>(ref wrapper);

            if (!TryGetInvocations(out ReadOnlySpan<Wrapper> invocationList))
            {
                int newResultCount = 1 + followList.Length;
                Wrapper[] newResultList = new Wrapper[newResultCount];
                newResultList[0] = new Wrapper(this);
                followList.CopyTo(newResultList.AsSpan(1, followList.Length));

                return NewMulticastDelegate(newResultList, newResultCount);
            }

            int resultCount = invocationList.Length + followList.Length;
            Wrapper[]? resultList = (Wrapper[])_helperObject!;
            if (resultList.Length < resultCount)
            {
                resultList = null;
            }
            else
            {
                Span<Wrapper> newInvocations = resultList.AsSpan(invocationList.Length, followList.Length);
                for (int i = 0; i < followList.Length; i++)
                {
                    if (TrySetSlot(ref newInvocations[i].Value, followList[i].Value!))
                        continue;

                    resultList = null;
                    break;
                }
            }

            if (resultList == null)
            {
                resultList = new Wrapper[BitOperations.RoundUpToPowerOf2((uint)resultCount)];
                invocationList.CopyTo(resultList);
                followList.CopyTo(resultList.AsSpan(invocationList.Length));
            }
            return NewMulticastDelegate(resultList, resultCount, true);
        }

        private static Wrapper[] DeleteFromInvocationList(ReadOnlySpan<Wrapper> invocationList, int deleteIndex, int deleteCount)
        {
            Wrapper[] newInvocationList = new Wrapper[BitOperations.RoundUpToPowerOf2((uint)(invocationList.Length - deleteCount))];

            invocationList.Slice(0, deleteIndex).CopyTo(newInvocationList);
            invocationList.Slice(deleteIndex + deleteCount).CopyTo(newInvocationList.AsSpan(deleteIndex));

            return newInvocationList;
        }

        // This method currently looks backward on the invocation list
        //    for an element that has Delegate based equality with value.  (Doesn't
        //    look at the invocation list.)  If this is found we remove it from
        //    this list and return a new delegate.  If its not found a copy of the
        //    current list is returned.
        protected Delegate? RemoveImpl(Delegate? d)
        {
            // There is a special case were we are removing using a delegate as
            //    the value we need to check for this case
            if (d is null)
                return this;

            bool isMulticast = TryGetInvocations(out ReadOnlySpan<Wrapper> invocationList);

            if (!d.TryGetInvocations(out ReadOnlySpan<Wrapper> otherInvocations))
            {
                // they are both not real Multicast
                if (!isMulticast)
                    return Equals(d) ? null : this;

                int index = invocationList.LastIndexOf(new Wrapper(d));
                if (index < 0)
                    return this;

                // Special case - only one value left, either at the beginning or the end
                if (invocationList.Length == 2)
                    return invocationList[1 - index].Value;

                Wrapper[] list = DeleteFromInvocationList(invocationList, index, 1);
                return NewMulticastDelegate(list, invocationList.Length - 1, true);
            }

            if (!isMulticast)
                return this;

            int i = invocationList.LastIndexOf(otherInvocations);
            if (i < 0)
                return this;

            int newCount = invocationList.Length - otherInvocations.Length;
            switch (newCount)
            {
                case 0:
                    // Special case - no values left
                    return null;
                case 1:
                    // Special case - only one value left, either at the beginning or the end
                    return invocationList[i == 0 ? ^1 : 0].Value;
                default:
                    Wrapper[] list = DeleteFromInvocationList(invocationList, i, otherInvocations.Length);
                    return NewMulticastDelegate(list, newCount, true);
            }
        }

        // Equals returns true IIF the delegate is not null and has the
        // same target, method and invocation list as this object.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (!RuntimeHelpers.TypeEquivalent(this, obj))
                return false;

            // Since this is a Delegate, and we know the types are the same, obj should also be a Delegate
            Debug.Assert(obj is Delegate, "Shouldn't have failed here since we already checked the types are the same!");
            return EqualsCore(Unsafe.As<Delegate>(obj));
        }
#endif
    }
}
