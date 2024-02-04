// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public abstract class MulticastDelegate : Delegate
    {
        private Delegate[]? delegates;

        [RequiresUnreferencedCode("The target method might be removed")]
        protected MulticastDelegate(object target, string method)
            : base(target, method)
        {
        }

        protected MulticastDelegate([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type target, string method)
            : base(target, method)
        {
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new SerializationException(SR.Serialization_DelegatesNotSupported);
        }

        protected sealed override object? DynamicInvokeImpl(object?[]? args)
        {
            if (delegates == null)
            {
                return base.DynamicInvokeImpl(args);
            }
            else
            {
                object? r;
                int i = 0, len = delegates.Length;
                do
                {
                    r = delegates[i].DynamicInvoke(args);
                } while (++i < len);
                return r;
            }
        }

        // <remarks>
        //   Equals: two multicast delegates are equal if their base is equal
        //   and their invocations list is equal.
        // </remarks>
        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!base.Equals(obj))
                return false;

            if (!(obj is MulticastDelegate d))
                return false;

            if (delegates == null && d.delegates == null)
            {
                return true;
            }
            else if (delegates == null ^ d.delegates == null)
            {
                return false;
            }
            else
            {
                if (delegates!.Length != d.delegates!.Length)
                    return false;

                for (int i = 0; i < delegates.Length; ++i)
                {
                    if (!delegates[i].Equals(d.delegates[i]))
                        return false;
                }

                return true;
            }
        }

        //
        // FIXME: This could use some improvements.
        //
        public sealed override int GetHashCode()
        {
            return base.GetHashCode();
        }

        protected override MethodInfo GetMethodImpl()
        {
            if (delegates != null)
                return delegates[delegates.Length - 1].Method;

            return base.GetMethodImpl();
        }

        // <summary>
        //   Return, in order of invocation, the invocation list
        //   of a MulticastDelegate
        // </summary>
        public sealed override Delegate[] GetInvocationList()
        {
            if (delegates != null)
                return (Delegate[])delegates.Clone();
            else
                return new Delegate[1] { this };
        }

        internal new bool HasSingleTarget
        {
            get { return delegates == null || delegates.Length == 1; }
        }

        // Used by delegate invocation list enumerator
        internal Delegate? TryGetAt(int index)
        {
            if (delegates == null)
            {
                return (index == 0) ? this : null;
            }
            else
            {
                return ((uint)index < (uint)delegates.Length) ? delegates[index] : null;
            }
        }

        // <summary>
        //   Combines this MulticastDelegate with the (Multicast)Delegate `follow'.
        //   This does _not_ combine with Delegates. ECMA states the whole delegate
        //   thing should have better been a simple System.Delegate class.
        //   Compiler generated delegates are always MulticastDelegates.
        // </summary>
        protected sealed override Delegate CombineImpl(Delegate? follow)
        {
            if (follow == null)
                return this;

            MulticastDelegate other = (MulticastDelegate)follow;

            MulticastDelegate ret = AllocDelegateLike_internal(this);

            if (delegates == null && other.delegates == null)
            {
                ret.delegates = new Delegate[2] { this, other };
            }
            else if (delegates == null)
            {
                ret.delegates = new Delegate[1 + other.delegates!.Length];

                ret.delegates[0] = this;
                Array.Copy(other.delegates, 0, ret.delegates, 1, other.delegates.Length);
            }
            else if (other.delegates == null)
            {
                ret.delegates = new Delegate[delegates.Length + 1];

                Array.Copy(delegates, 0, ret.delegates, 0, delegates.Length);
                ret.delegates[ret.delegates.Length - 1] = other;
            }
            else
            {
                ret.delegates = new Delegate[delegates.Length + other.delegates.Length];

                Array.Copy(delegates, 0, ret.delegates, 0, delegates.Length);
                Array.Copy(other.delegates, 0, ret.delegates, delegates.Length, other.delegates.Length);
            }

            return ret;
        }

        /* Based on the Boyer-Moore string search algorithm */
        private static int LastIndexOf(Delegate[] haystack, Delegate[] needle)
        {
            if (haystack.Length < needle.Length)
                return -1;

            if (haystack.Length == needle.Length)
            {
                for (int i = 0; i < haystack.Length; ++i)
                    if (!haystack[i].Equals(needle[i]))
                        return -1;

                return 0;
            }

            for (int i = haystack.Length - needle.Length, j; i >= 0;)
            {
                for (j = 0; needle[j].Equals(haystack[i]); ++i, ++j)
                {
                    if (j == needle.Length - 1)
                        return i - j;
                }

                i -= j + 1;
            }

            return -1;
        }

        protected sealed override Delegate? RemoveImpl(Delegate value)
        {
            if (value == null)
                return this;

            MulticastDelegate other = (MulticastDelegate)value;

            if (delegates == null && other.delegates == null)
            {
                /* if they are not equal and the current one is not
                 * a multicastdelegate then we cannot delete it */
                return this.Equals(other) ? null : this;
            }
            else if (delegates == null)
            {
                foreach (Delegate? d in other.delegates!)
                {
                    if (this.Equals(d))
                        return null;
                }
                return this;
            }
            else if (other.delegates == null)
            {
                int idx = Array.LastIndexOf(delegates, other);
                if (idx == -1)
                    return this;

                if (delegates.Length <= 1)
                {
                    /* delegates.Length should never be equal or
                     * lower than 1, it should be 2 or greater */
                    throw new InvalidOperationException();
                }

                if (delegates.Length == 2)
                    return delegates[idx == 0 ? 1 : 0];

                MulticastDelegate ret = AllocDelegateLike_internal(this);
                ret.delegates = new Delegate[delegates.Length - 1];

                Array.Copy(delegates, ret.delegates, idx);
                Array.Copy(delegates, idx + 1, ret.delegates, idx, delegates.Length - idx - 1);

                return ret;
            }
            else
            {
                /* wild case : remove MulticastDelegate from MulticastDelegate
                 * complexity is O(m + n), with n the number of elements in
                 * this.delegates and m the number of elements in other.delegates */

                if (delegates.Equals(other.delegates))
                    return null;

                /* we need to remove elements from the end to the beginning, as
                 * the addition and removal of delegates behaves like a stack */
                int idx = LastIndexOf(delegates, other.delegates);
                if (idx == -1)
                    return this;

                MulticastDelegate ret = AllocDelegateLike_internal(this);
                ret.delegates = new Delegate[delegates.Length - other.delegates.Length];

                Array.Copy(delegates, ret.delegates, idx);
                Array.Copy(delegates, idx + other.delegates.Length, ret.delegates, idx, delegates.Length - idx - other.delegates.Length);

                return ret;
            }
        }

        public static bool operator ==(MulticastDelegate? d1, MulticastDelegate? d2)
        {
            if (d1 == null)
                return d2 == null;

            return d1.Equals(d2);
        }

        public static bool operator !=(MulticastDelegate? d1, MulticastDelegate? d2)
        {
            if (d1 == null)
                return d2 != null;

            return !d1.Equals(d2);
        }

        internal override object? GetTarget()
        {
            return delegates?.Length > 0 ? delegates[delegates.Length - 1].GetTarget() : base.GetTarget();
        }
    }
}
