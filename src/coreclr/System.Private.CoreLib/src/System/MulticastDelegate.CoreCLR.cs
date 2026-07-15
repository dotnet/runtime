// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
#pragma warning disable CS0660, CS0661 // Defining operators, but not overriding Equals/GetHashCode
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
            return dd._methodPtr == d._methodPtr &&
                   dd._methodPtrAux == d._methodPtrAux &&
                   dd._target == d._target;
        }

        private unsafe MulticastDelegate NewMulticastDelegate(object[] invocationList, int invocationCount, bool thisIsMultiCastAlready = false)
        {
            // First, allocate a new multicast delegate just like this one, i.e. same type as the this object
            MulticastDelegate result = InternalAlloc(RuntimeHelpers.GetMethodTable(this));

            // Performance optimization - if this already points to a true multicast delegate,
            // copy _methodPtr and _methodPtrAux fields rather than calling into the EE to get them
            if (thisIsMultiCastAlready)
            {
                result._methodPtr = _methodPtr;
                result._methodPtrAux = _methodPtrAux;
            }
            else
            {
                result._methodPtr = GetMulticastInvoke();
                result._methodPtrAux = GetInvokeMethod();
            }
            result._target = result;
            result._helperObject = invocationList;
            result._extraData = invocationCount;

            return result;
        }

        // This method will combine this delegate with the passed delegate
        //    to form a new delegate.
        internal new Delegate CombineImpl(Delegate? follow)
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
                followCount = (int)dFollow._extraData;

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

            int invocationCount = (int)_extraData;
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
            Debug.Assert(_helperObject is object[]);
            object[] thisInvocationList = (object[])_helperObject;

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
        internal new Delegate? RemoveImpl(Delegate? value)
        {
            // There is a special case were we are removing using a delegate as
            //    the value we need to check for this case
            //
            MulticastDelegate? v = (MulticastDelegate?)value;
            if (v == null)
                return this;

            if (v.HasSingleTarget)
            {
                if (_helperObject is not object[] invocationList)
                {
                    // they are both not real Multicast
                    if (Equals(v))
                        return null;
                }
                else
                {
                    int invocationCount = (int)_extraData;
                    for (int i = invocationCount; --i >= 0;)
                    {
                        if (!v.Equals(invocationList[i]))
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
            }
            else if (_helperObject is object[] invocationList)
            {
                int invocationCount = (int)_extraData;
                int vInvocationCount = (int)v._extraData;
                object[] vInvocationList = (object[])v._helperObject!;
                for (int i = invocationCount - vInvocationCount; i >= 0; i--)
                {
                    if (!EqualInvocationLists(invocationList, vInvocationList, i, vInvocationCount))
                    {
                        continue;
                    }

                    switch (invocationCount - vInvocationCount)
                    {
                        case 0:
                            // Special case - no values left
                            return null;
                        case 1:
                            // Special case - only one value left, either at the beginning or the end
                            return (Delegate)invocationList[i != 0 ? 0 : invocationCount - 1];
                        default:
                        {
                            object[] list = DeleteFromInvocationList(invocationList, invocationCount, i,
                                vInvocationCount);
                            return NewMulticastDelegate(list, invocationCount - vInvocationCount, true);
                        }
                    }
                }
            }

            return this;
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
            _target = target;
            _methodPtr = methodPtr;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorClosedStatic(object target, IntPtr methodPtr)
        {
            _target = target;
            _methodPtr = methodPtr;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorRTClosed(object target, IntPtr methodPtr)
        {
            if (target == null)
                ThrowNullThisInDelegateToInstance();
            _target = target;
            _methodPtr = AdjustTarget(target, methodPtr);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk)
        {
            _target = this;
            _methodPtr = shuffleThunk;
            _methodPtrAux = methodPtr;
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk)
        {
            _target = this;
            _methodPtr = shuffleThunk;
            InitializeVirtualCallStub(methodPtr);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorCollectibleClosedStatic(object target, IntPtr methodPtr, IntPtr gchandle)
        {
            _target = target;
            _methodPtr = methodPtr;
            _helperObject = GCHandle.InternalGet(gchandle);
            Debug.Assert(HasSingleTarget);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorCollectibleOpened(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
        {
            _target = this;
            _methodPtr = shuffleThunk;
            _methodPtrAux = methodPtr;
            _helperObject = GCHandle.InternalGet(gchandle);
            Debug.Assert(HasSingleTarget);
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        private void CtorCollectibleVirtualDispatch(object target, IntPtr methodPtr, IntPtr shuffleThunk, IntPtr gchandle)
        {
            _target = this;
            _methodPtr = shuffleThunk;
            _helperObject = GCHandle.InternalGet(gchandle);
            Debug.Assert(HasSingleTarget);
            InitializeVirtualCallStub(methodPtr);
        }
#pragma warning restore IDE0060
    }
#pragma warning restore CS0660, CS0661
}
