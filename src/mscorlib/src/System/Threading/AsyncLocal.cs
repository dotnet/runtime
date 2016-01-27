// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security;

namespace System.Threading
{
    //
    // AsyncLocal<T> represents "ambient" data that is local to a given asynchronous control flow, such as an
    // async method.  For example, say you want to associate a culture with a given async flow:
    //
    // static AsyncLocal<Culture> s_currentCulture = new AsyncLocal<Culture>();
    //
    // static async Task SomeOperationAsync(Culture culture)
    // {
    //    s_currentCulture.Value = culture;
    //
    //    await FooAsync();
    // }
    //
    // static async Task FooAsync()
    // {
    //    PrintStringWithCulture(s_currentCulture.Value);
    // }
    //
    // AsyncLocal<T> also provides optional notifications when the value associated with the current thread
    // changes, either because it was explicitly changed by setting the Value property, or implicitly changed
    // when the thread encountered an "await" or other context transition.  For example, we might want our
    // current culture to be communicated to the OS as well:
    //
    // static AsyncLocal<Culture> s_currentCulture = new AsyncLocal<Culture>(
    //   args =>
    //   {
    //      NativeMethods.SetThreadCulture(args.CurrentValue.LCID);
    //   });
    //
    public sealed class AsyncLocal<T> : IAsyncLocal
    {
        [SecurityCritical] // critical because this action will terminate the process if it throws.
        private readonly Action<AsyncLocalValueChangedArgs<T>> m_valueChangedHandler;

        //
        // Constructs an AsyncLocal<T> that does not receive change notifications.
        //
        public AsyncLocal() 
        {
        }

        //
        // Constructs an AsyncLocal<T> with a delegate that is called whenever the current value changes
        // on any thread.
        //
        [SecurityCritical]
        public AsyncLocal(Action<AsyncLocalValueChangedArgs<T>> valueChangedHandler) 
        {
            m_valueChangedHandler = valueChangedHandler;
        }

        public T Value
        {
            [SecuritySafeCritical]
            get 
            { 
                object obj = ExecutionContext.GetLocalValue(this);
                return (obj == null) ? default(T) : (T)obj;
            }
            [SecuritySafeCritical]
            set 
            {
                ExecutionContext.SetLocalValue(this, value, m_valueChangedHandler != null); 
            }
        }

        [SecurityCritical]
        void IAsyncLocal.OnValueChanged(object previousValueObj, object currentValueObj, bool contextChanged)
        {
            Contract.Assert(m_valueChangedHandler != null);
            T previousValue = previousValueObj == null ? default(T) : (T)previousValueObj;
            T currentValue = currentValueObj == null ? default(T) : (T)currentValueObj;
            m_valueChangedHandler(new AsyncLocalValueChangedArgs<T>(previousValue, currentValue, contextChanged));
        }
    }

    //
    // Interface to allow non-generic code in ExecutionContext to call into the generic AsyncLocal<T> type.
    //
    internal interface IAsyncLocal
    {
        [SecurityCritical]
        void OnValueChanged(object previousValue, object currentValue, bool contextChanged);
    }

    public struct AsyncLocalValueChangedArgs<T>
    {
        public T PreviousValue { get; private set; }
        public T CurrentValue { get; private set; }
        
        //
        // If the value changed because we changed to a different ExecutionContext, this is true.  If it changed
        // because someone set the Value property, this is false.
        //
        public bool ThreadContextChanged { get; private set; }

        internal AsyncLocalValueChangedArgs(T previousValue, T currentValue, bool contextChanged)
            : this()
        {
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            ThreadContextChanged = contextChanged;
        }
    }
}
