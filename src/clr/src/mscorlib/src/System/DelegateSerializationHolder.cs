// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System
{
    [Serializable]
    internal sealed class DelegateSerializationHolder : IObjectReference, ISerializable
    {
        #region Static Members
        internal static DelegateEntry GetDelegateSerializationInfo(
            SerializationInfo info, Type delegateType, Object target, MethodInfo method, int targetIndex)
        {
            // Used for MulticastDelegate

            if (method == null)
                throw new ArgumentNullException(nameof(method));
            Contract.EndContractBlock();

            Type c = delegateType.BaseType;

            if (c == null || (c != typeof(Delegate) && c != typeof(MulticastDelegate)))
                throw new ArgumentException(SR.Arg_MustBeDelegate, "type");

            if (method.DeclaringType == null)
                throw new NotSupportedException(SR.NotSupported_GlobalMethodSerialization);

            DelegateEntry de = new DelegateEntry(delegateType.FullName, delegateType.Module.Assembly.FullName, target,
                method.ReflectedType.Module.Assembly.FullName, method.ReflectedType.FullName, method.Name);

            if (info.MemberCount == 0)
            {
                info.SetType(typeof(DelegateSerializationHolder));
                info.AddValue("Delegate", de, typeof(DelegateEntry));
            }

            // target can be an object so it needs to be added to the info, or else a fixup is needed
            // when deserializing, and the fixup will occur too late. If it is added directly to the
            // info then the rules of deserialization will guarantee that it will be available when
            // needed

            if (target != null)
            {
                String targetName = "target" + targetIndex;
                info.AddValue(targetName, de.target);
                de.target = targetName;
            }

            // Due to a number of additions (delegate signature binding relaxation, delegates with open this or closed over the
            // first parameter and delegates over generic methods) we need to send a deal more information than previously. We can
            // get this by serializing the target MethodInfo. We still need to send the same information as before though (the
            // DelegateEntry above) for backwards compatibility. And we want to send the MethodInfo (which is serialized via an
            // ISerializable holder) as a top-level child of the info for the same reason as the target above -- we wouldn't have an
            // order of deserialization guarantee otherwise.
            String methodInfoName = "method" + targetIndex;
            info.AddValue(methodInfoName, method);

            return de;
        }
        #endregion

        #region Definitions
        [Serializable]
        internal class DelegateEntry
        {
            #region Internal Data Members
            internal String type;
            internal String assembly;
            internal Object target;
            internal String targetTypeAssembly;
            internal String targetTypeName;
            internal String methodName;
            internal DelegateEntry delegateEntry;
            #endregion

            #region Constructor
            internal DelegateEntry(
                String type, String assembly, Object target, String targetTypeAssembly, String targetTypeName, String methodName)
            {
                this.type = type;
                this.assembly = assembly;
                this.target = target;
                this.targetTypeAssembly = targetTypeAssembly;
                this.targetTypeName = targetTypeName;
                this.methodName = methodName;
            }
            #endregion

            #region Internal Members
            internal DelegateEntry Entry
            {
                get { return delegateEntry; }
                set { delegateEntry = value; }
            }
            #endregion
        }

        #endregion

        #region Private Data Members
        private DelegateEntry m_delegateEntry;
        private MethodInfo[] m_methods;
        #endregion

        #region Constructor
        private DelegateSerializationHolder(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();

            bool bNewWire = true;

            try
            {
                m_delegateEntry = (DelegateEntry)info.GetValue("Delegate", typeof(DelegateEntry));
            }
            catch
            {
                // Old wire format
                m_delegateEntry = OldDelegateWireFormat(info, context);
                bNewWire = false;
            }

            if (bNewWire)
            {
                // retrieve the targets
                DelegateEntry deiter = m_delegateEntry;
                int count = 0;
                while (deiter != null)
                {
                    if (deiter.target != null)
                    {
                        string stringTarget = deiter.target as string; //need test to pass older wire format
                        if (stringTarget != null)
                            deiter.target = info.GetValue(stringTarget, typeof(Object));
                    }
                    count++;
                    deiter = deiter.delegateEntry;
                }

                // If the sender is as recent as us they'll have provided MethodInfos for each delegate. Look for these and pack
                // them into an ordered array if present.
                MethodInfo[] methods = new MethodInfo[count];
                int i;
                for (i = 0; i < count; i++)
                {
                    String methodInfoName = "method" + i;
                    methods[i] = (MethodInfo)info.GetValueNoThrow(methodInfoName, typeof(MethodInfo));
                    if (methods[i] == null)
                        break;
                }

                // If we got the info then make the array available for deserialization.
                if (i == count)
                    m_methods = methods;
            }
        }
        #endregion

        #region Private Members
        private void ThrowInsufficientState(string field)
        {
            throw new SerializationException(
                SR.Format(SR.Serialization_InsufficientDeserializationState, field));
        }

        private DelegateEntry OldDelegateWireFormat(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();

            String delegateType = info.GetString("DelegateType");
            String delegateAssembly = info.GetString("DelegateAssembly");
            Object target = info.GetValue("Target", typeof(Object));
            String targetTypeAssembly = info.GetString("TargetTypeAssembly");
            String targetTypeName = info.GetString("TargetTypeName");
            String methodName = info.GetString("MethodName");

            return new DelegateEntry(delegateType, delegateAssembly, target, targetTypeAssembly, targetTypeName, methodName);
        }

        private Delegate GetDelegate(DelegateEntry de, int index)
        {
            Delegate d;

            try
            {
                if (de.methodName == null || de.methodName.Length == 0)
                    ThrowInsufficientState("MethodName");

                if (de.assembly == null || de.assembly.Length == 0)
                    ThrowInsufficientState("DelegateAssembly");

                if (de.targetTypeName == null || de.targetTypeName.Length == 0)
                    ThrowInsufficientState("TargetTypeName");

                // We cannot use Type.GetType directly, because of AppCompat - assembly names starting with '[' would fail to load.
                RuntimeType type = (RuntimeType)Assembly.GetType_Compat(de.assembly, de.type);

                // {de.targetTypeAssembly, de.targetTypeName} do not actually refer to the type of the target, but the reflected
                // type of the method. Those types may be the same when the method is on the target's type or on a type in its
                // inheritance chain, but those types may not be the same when the method is an extension method for the
                // target's type or a type in its inheritance chain.

                // If we received the new style delegate encoding we already have the target MethodInfo in hand.
                if (m_methods != null)
                {
                    // The method info is serialized, so the target type info is redundant. The desktop framework does no
                    // additional verification on the target type info.
                    d = Delegate.CreateDelegateNoSecurityCheck(type, de.target, m_methods[index]);
                }
                else
                {
                    if (de.target != null)
                    {
                        // For consistency with the desktop framework, when the method info is not serialized for a closed
                        // delegate, the method is assumed to be on the target's type or in its inheritance chain. An extension
                        // method would not work on this path for the above reason and also because the delegate binds to
                        // instance methods only. The desktop framework does no additional verification on the target type info.
                        d = Delegate.CreateDelegate(type, de.target, de.methodName);
                    }
                    else
                    {
                        RuntimeType targetType = (RuntimeType)Assembly.GetType_Compat(de.targetTypeAssembly, de.targetTypeName);
                        d = Delegate.CreateDelegate(type, targetType, de.methodName);
                    }
                }
            }
            catch (Exception e)
            {
                if (e is SerializationException)
                    throw e;

                throw new SerializationException(e.Message, e);
            }

            return d;
        }
        #endregion

        #region IObjectReference
        public Object GetRealObject(StreamingContext context)
        {
            int count = 0;
            for (DelegateEntry de = m_delegateEntry; de != null; de = de.Entry)
                count++;

            int maxindex = count - 1;

            if (count == 1)
            {
                return GetDelegate(m_delegateEntry, 0);
            }
            else
            {
                object[] invocationList = new object[count];

                for (DelegateEntry de = m_delegateEntry; de != null; de = de.Entry)
                {
                    // Be careful to match the index we pass to GetDelegate (used to look up extra information for each delegate) to
                    // the order we process the entries: we're actually looking at them in reverse order.
                    --count;
                    invocationList[count] = GetDelegate(de, maxindex - count);
                }
                return ((MulticastDelegate)invocationList[0]).NewMulticastDelegate(invocationList, invocationList.Length);
            }
        }
        #endregion

        #region ISerializable
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException(SR.NotSupported_DelegateSerHolderSerial);
        }
        #endregion
    }
}
