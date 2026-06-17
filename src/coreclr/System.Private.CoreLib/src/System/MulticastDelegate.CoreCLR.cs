// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public abstract partial class MulticastDelegate : Delegate
    {
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new SerializationException(SR.Serialization_DelegatesNotSupported);
        }

        // equals returns true IIF the delegate is not null and has the
        //    same target, method and invocation list as this object
        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (!InternalEqualTypes(this, obj))
                return false;

            // Since this is a MulticastDelegate and we know
            // the types are the same, obj should also be a
            // MulticastDelegate
            Debug.Assert(obj is MulticastDelegate, "Shouldn't have failed here since we already checked the types are the same!");
            MulticastDelegate other = Unsafe.AsAssert<MulticastDelegate>(obj);

            if (IsSpecialDelegate)
            {
                // there are 4 kind of delegate kinds that fall into this bucket
                // 1- Multicast (_helperObject is Object[])
                // 2- Wrapper (_helperObject is Delegate)
                // 3- Unmanaged FntPtr (_helperObject == null)
                // 4- Open virtual (_invocationCount == MethodDesc of target, _helperObject == null, LoaderAllocator, or DynamicResolver)
                switch (_helperObject)
                {
                    case object[]:
                        return GetInvocationsUnchecked().SequenceEqual(other.GetInvocationsUnchecked());
                    // this is a wrapper delegate so we need to unwrap and check the inner one
                    case Delegate wrapper:
                        return wrapper.Equals(other);
                    default:
                        if (IsUnmanagedFunctionPtr)
                        {
                            return other.IsUnmanagedFunctionPtr &&
                                   _functionPointer == other._functionPointer &&
                                   _extraFunctionPointerOrData == other._extraFunctionPointerOrData;
                        }

                        // now we know 'this' is not a special one, so we can work out what the other is
                        // this is a wrapper delegate so we need to unwrap and check the inner one
                        return other._helperObject is Delegate ? Equals(other._helperObject) : base.Equals(obj);
                }
            }

            Debug.Assert(InvocationListLogicallyNull);
            // now we know 'this' is not a special one, so we can work out what the other is
            return other._helperObject is Delegate ?
                // this is a wrapper delegate so we need to unwrap and check the inner one
                Equals(other._helperObject) :
                // we can call on the base
                base.Equals(other);
        }

        private static bool TrySetSlot(object?[] a, int index, object o)
        {
            if (a[index] == null && Interlocked.CompareExchange(ref a[index], o, null) == null)
            {
                return true;
            }

            // The slot may be already set because we have added and removed the same method before.
            // Optimize this case, because it's cheaper than copying the array.
            object? previous = a[index];
            if (previous is null)
            {
                return false;
            }

            MulticastDelegate d = (MulticastDelegate)o;
            MulticastDelegate dd = (MulticastDelegate)previous;
            return dd._functionPointer == d._functionPointer &&
                   dd._extraFunctionPointerOrData == d._extraFunctionPointerOrData &&
                   dd._firstParameter == d._firstParameter;
        }

        private unsafe MulticastDelegate NewMulticastDelegate(object[] invocationList, int invocationCount, bool thisIsMultiCastAlready)
        {
            // First, allocate a new multicast delegate just like this one, i.e. same type as the this object
            MulticastDelegate result = Unsafe.AsAssert<MulticastDelegate>(RuntimeTypeHandle.InternalAllocNoChecks(RuntimeHelpers.GetMethodTable(this)));

            // Performance optimization - if this already points to a true multicast delegate,
            // copy _functionPointer and _functionPointerAux fields rather than calling into the EE to get them
            if (thisIsMultiCastAlready)
            {
                result._functionPointer = _functionPointer;
                result._extraFunctionPointerOrData = _extraFunctionPointerOrData;
            }
            else
            {
                result._functionPointer = GetMulticastInvoke();
                result._extraFunctionPointerOrData = GetInvokeMethod();
            }
            result._firstParameter = result;
            result._helperObject = invocationList;
            result._invocationCount = invocationCount;

            return result;
        }

        internal MulticastDelegate NewMulticastDelegate(object[] invocationList, int invocationCount)
        {
            return NewMulticastDelegate(invocationList, invocationCount, false);
        }

        // This method will combine this delegate with the passed delegate
        //    to form a new delegate.
        protected sealed override Delegate CombineImpl(Delegate? follow)
        {
            if (follow is null)
                return this;

            // Verify that the types are the same...
            if (!InternalEqualTypes(this, follow))
                throw new ArgumentException(SR.Arg_DlgtTypeMis);

            MulticastDelegate dFollow = (MulticastDelegate)follow;
            object[]? resultList;
            int followCount = 1;
            object[]? followList = dFollow._helperObject as object[];
            if (followList != null)
                followCount = (int)dFollow._invocationCount;

            int resultCount;
            if (_helperObject is not object[] invocationList)
            {
                resultCount = 1 + followCount;
                resultList = new object[resultCount];
                resultList[0] = this;
                if (followList == null)
                {
                    resultList[1] = dFollow;
                }
                else
                {
                    for (int i = 0; i < followCount; i++)
                        resultList[1 + i] = followList[i];
                }
                return NewMulticastDelegate(resultList, resultCount);
            }

            int invocationCount = (int)_invocationCount;
            resultCount = invocationCount + followCount;
            resultList = null;
            if (resultCount <= invocationList.Length)
            {
                resultList = invocationList;
                if (followList == null)
                {
                    if (!TrySetSlot(resultList, invocationCount, dFollow))
                        resultList = null;
                }
                else
                {
                    for (int i = 0; i < followCount; i++)
                    {
                        if (TrySetSlot(resultList, invocationCount + i, followList[i]))
                        {
                            continue;
                        }

                        resultList = null;
                        break;
                    }
                }
            }

            if (resultList == null)
            {
                int allocCount = invocationList.Length;
                while (allocCount < resultCount)
                    allocCount *= 2;

                resultList = new object[allocCount];

                for (int i = 0; i < invocationCount; i++)
                    resultList[i] = invocationList[i];

                if (followList == null)
                {
                    resultList[invocationCount] = dFollow;
                }
                else
                {
                    for (int i = 0; i < followCount; i++)
                        resultList[invocationCount + i] = followList[i];
                }
            }
            return NewMulticastDelegate(resultList, resultCount, true);
        }

        private object[] DeleteFromInvocationList(object[] invocationList, int invocationCount, int deleteIndex, int deleteCount)
        {
            object[]? thisInvocationList = Unsafe.AsAssert<object[]>(_helperObject);
            Debug.Assert(thisInvocationList is not null);

            int allocCount = thisInvocationList.Length;
            while (allocCount / 2 >= invocationCount - deleteCount)
                allocCount /= 2;

            object[] newInvocationList = new object[allocCount];

            for (int i = 0; i < deleteIndex; i++)
                newInvocationList[i] = invocationList[i];

            for (int i = deleteIndex + deleteCount; i < invocationCount; i++)
                newInvocationList[i - deleteCount] = invocationList[i];

            return newInvocationList;
        }

        private static bool EqualInvocationLists(object[] a, object[] b, int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!a[start + i].Equals(b[i]))
                    return false;
            }
            return true;
        }

        // This method currently looks backward on the invocation list
        //    for an element that has Delegate based equality with value.  (Doesn't
        //    look at the invocation list.)  If this is found we remove it from
        //    this list and return a new delegate.  If its not found a copy of the
        //    current list is returned.
        protected sealed override Delegate? RemoveImpl(Delegate? value)
        {
            // There is a special case were we are removing using a delegate as
            //    the value we need to check for this case
            //
            MulticastDelegate? v = (MulticastDelegate?)value;
            if (v == null)
                return this;

            if (!v.HasSingleTarget)
            {
                if (_helperObject is object[] invocationList)
                {
                    int invocationCount = (int)_invocationCount;
                    int vInvocationCount = (int)v._invocationCount;
                    object[] vInvocationList = (object[])v._helperObject!;
                    for (int i = invocationCount - vInvocationCount; i >= 0; i--)
                    {
                        if (!EqualInvocationLists(invocationList, vInvocationList, i, vInvocationCount))
                        {
                            continue;
                        }

                        if (invocationCount - vInvocationCount == 0)
                        {
                            // Special case - no values left
                            return null;
                        }

                        if (invocationCount - vInvocationCount == 1)
                        {
                            // Special case - only one value left, either at the beginning or the end
                            return (Delegate)invocationList[i != 0 ? 0 : invocationCount - 1];
                        }

                        object[] list = DeleteFromInvocationList(invocationList, invocationCount, i, vInvocationCount);
                        return NewMulticastDelegate(list, invocationCount - vInvocationCount, true);
                    }
                }
            }
            else
            {
                if (_helperObject is object[] invocationList)
                {
                    int invocationCount = (int)_invocationCount;
                    for (int i = invocationCount; --i >= 0;)
                    {
                        if (!value!.Equals(invocationList[i]))
                        {
                            continue;
                        }

                        if (invocationCount == 2)
                        {
                            // Special case - only one value left, either at the beginning or the end
                            return (Delegate)invocationList[1 - i];
                        }

                        object[] list = DeleteFromInvocationList(invocationList, invocationCount, i, 1);
                        return NewMulticastDelegate(list, invocationCount - 1, true);
                    }
                }
                else
                {
                    // they are both not real Multicast
                    if (Equals(value))
                        return null;
                }
            }

            return this;
        }

        // This method returns the Invocation list of this multicast delegate.
        public sealed override Delegate[] GetInvocationList()
        {
            if (_helperObject is not object[])
            {
                return [this];
            }

            // Create an array of delegate copies and copy each element into the array
            ReadOnlySpan<MulticastDelegate> invocationList = GetInvocationsUnchecked();
            Delegate[] del = new Delegate[invocationList.Length];
            for (int i = 0; i < invocationList.Length; i++)
                del[i] = invocationList[i];
            return del;
        }

        internal new bool HasSingleTarget => _helperObject is not object[];

        // Used by delegate invocation list enumerator
        internal MulticastDelegate? TryGetAt(int index)
        {
            if (HasSingleTarget)
            {
                if (index == 0)
                    return this;
            }
            else
            {
                ReadOnlySpan<MulticastDelegate> invocationList = GetInvocationsUnchecked();
                if ((uint)index < (uint)invocationList.Length)
                    return invocationList[index];
            }

            return null;
        }

        public sealed override int GetHashCode()
        {
            if (IsUnmanagedFunctionPtr)
                return HashCode.Combine(_functionPointer, _functionPointer);

            if (IsSpecialDelegate)
            {
                switch (_helperObject)
                {
                    case object[]:
                    {
                        int hash = 0;
                        foreach (MulticastDelegate multicastDelegate in GetInvocationsUnchecked())
                        {
                            hash = hash * 33 + multicastDelegate.GetHashCode();
                        }

                        return hash;
                    }
                    case Delegate wrapper:
                        // this is a wrapper delegate so we need to unwrap and check the inner one
                        return wrapper.GetHashCode();
                }
            }

            return base.GetHashCode();
        }

        internal override object? GetTarget()
        {
            if (IsSpecialDelegate)
            {
                // IsSpecialDelegate we are in one of these cases:
                // - Multicast -> return the target of the last delegate in the list
                // - Wrapper delegate -> return the target of the inner delegate
                // - unmanaged function pointer - return null
                // - virtual open delegate - return null
                if (InvocationListLogicallyNull)
                {
                    // both open virtual and ftn pointer return null for the target
                    return null;
                }

                switch (_helperObject)
                {
                    case object[]:
                        return GetLastInvocationUnchecked().Target;
                    case Delegate receiver:
                        return receiver.Target;
                }
            }
            return base.GetTarget();
        }

        // this should help inlining
        [DoesNotReturn]
        [DebuggerNonUserCode]
        private static void ThrowNullThisInDelegateToInstance() =>
            throw new ArgumentException(SR.Arg_DlgtNullInst);

#pragma warning disable IDE0060
        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorClosed(object target, IntPtr methodPtr)
        {
            if (target == null)
                ThrowNullThisInDelegateToInstance();
            _firstParameter = target;
            _functionPointer = (nuint)methodPtr;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorClosedStatic(object target, IntPtr methodPtr)
        {
            _firstParameter = target;
            _functionPointer = (nuint)methodPtr;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorRTClosed(object target, IntPtr methodPtr)
        {
            if (target == null)
                ThrowNullThisInDelegateToInstance();
            _firstParameter = target;
            _functionPointer = (nuint)AdjustTarget(target, methodPtr);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk)
        {
            _firstParameter = this;
            _functionPointer = (nuint)shuffleThunk;
            _extraFunctionPointerOrData = methodPtr;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk)
        {
            _firstParameter = this;
            _functionPointer = (nuint)shuffleThunk;
            InitializeVirtualCallStub(methodPtr);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorCollectibleClosedStatic(object target, IntPtr methodPtr, IntPtr gchandle)
        {
            _firstParameter = target;
            _functionPointer = (nuint)methodPtr;
            _helperObject = GCHandle.InternalGet(gchandle);
            Debug.Assert(InvocationListLogicallyNull);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorCollectibleOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
        {
            _firstParameter = this;
            _functionPointer = (nuint)shuffleThunk;
            _extraFunctionPointerOrData = methodPtr;
            _helperObject = GCHandle.InternalGet(gchandle);
            Debug.Assert(InvocationListLogicallyNull);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorCollectibleVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
        {
            _firstParameter = this;
            _functionPointer = (nuint)shuffleThunk;
            _helperObject = GCHandle.InternalGet(gchandle);
            Debug.Assert(InvocationListLogicallyNull);
            InitializeVirtualCallStub(methodPtr);
        }
#pragma warning restore IDE0060
    }
}
