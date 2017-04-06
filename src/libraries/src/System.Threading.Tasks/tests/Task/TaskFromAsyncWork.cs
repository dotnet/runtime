// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// AsyncWork.cs
//
// Helper class that is used to test the FromAsync method. These classes hold the APM patterns 
// and is used by the TaskFromAsyncTest.cs file
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace System.Threading.Tasks.Tests
{
    #region AsyncWork (base)

    /// <summary>
    /// The abstract that defines the work done by the Async method
    /// </summary>
    public abstract class AsyncWork
    {
        /// <summary>
        /// Defines the amount of time the thread should sleep (to simulate workload)
        /// </summary>
        private const int DEFAULT_TIME = 15;
        private List<object> _inputs;


        public AsyncWork()
        {
            _inputs = new List<object>();
        }

        protected void AddInput(object o)
        {
            _inputs.Add(o);
        }

        protected void InvokeAction(bool throwing)
        {
            //
            // simulate some dummy workload
            //
            var task = Task.Delay(DEFAULT_TIME);
            task.Wait();

            if (throwing) //simulates error condition during the execution of user delegate
            {
                throw new TPLTestException();
            }
        }

        protected ReadOnlyCollection<object> InvokeFunc(bool throwing)
        {
            //
            // simulate some dummy workload
            //
            var task = Task.Delay(DEFAULT_TIME);
            task.Wait();

            if (throwing)
            {
                throw new TPLTestException();
            }

            return Inputs;
        }

        protected void CheckState(object o)
        {
            ObservedState = o;

            ObservedTaskScheduler = TaskScheduler.Current;
        }

        public ReadOnlyCollection<object> Inputs
        {
            get
            {
                return new ReadOnlyCollection<object>(_inputs);
            }
        }

        public object ObservedState
        {
            get;
            private set;
        }

        public object ObservedTaskScheduler
        {
            get;
            private set;
        }
    }

    #endregion

    #region AsyncAction

    /// <summary>
    /// Extends the base class to implement that action form of APM
    /// </summary>
    public class AsyncAction : AsyncWork
    {
        private Action _action;

        // a general action to take-in inputs upfront rather than delayed until BeginInvoke
        // for testing the overload taking IAsyncResult
        public AsyncAction(object[] inputs, bool throwing)
            : base()
        {
            _action = () =>
            {
                foreach (object o in inputs)
                {
                    AddInput(o);
                }

                InvokeAction(throwing);
            };
        }

        public AsyncAction(bool throwing)
            : base()
        {
            _action = () =>
            {
                InvokeAction(throwing);
            };
        }

        #region APM

        public IAsyncResult BeginInvoke(AsyncCallback cb, object state)
        {
            Task task = Task.Factory.StartNew(_ => _action(), state, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(_ => cb(task));
            return task;
        }

        public void EndInvoke(IAsyncResult iar)
        {
            CheckState(iar.AsyncState);
            ((Task)iar).GetAwaiter().GetResult();
        }

        #endregion
    }

    /// <summary>
    /// Extends the base class to implement that action form of APM with one parameter
    /// </summary>
    public class AsyncAction<T> : AsyncWork
    {
        public delegate void Action<TArg>(TArg obj);

        private Action<T> _action;

        public AsyncAction(bool throwing)
            : base()
        {
            _action = (o) =>
            {
                AddInput(o);

                InvokeAction(throwing);
            };
        }

        #region APM

        public IAsyncResult BeginInvoke(T t, AsyncCallback cb, object state)
        {
            Task task = Task.Factory.StartNew(_ => _action(t), state, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(_ => cb(task));
            return task;
        }

        public void EndInvoke(IAsyncResult iar)
        {
            CheckState(iar.AsyncState);
            ((Task)iar).GetAwaiter().GetResult();
        }

        #endregion
    }

    /// <summary>
    /// Extends the base class to implement that action form of APM with two parameters
    /// </summary>
    public class AsyncAction<T1, T2> : AsyncWork
    {
        private Action<T1, T2> _action;

        public AsyncAction(bool throwing)
            : base()
        {
            _action = (o1, o2) =>
            {
                AddInput(o1);
                AddInput(o2);

                InvokeAction(throwing);
            };
        }

        #region APM

        public IAsyncResult BeginInvoke(T1 t1, T2 t2, AsyncCallback cb, object state)
        {
            Task task = Task.Factory.StartNew(_ => _action(t1, t2), state, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(_ => cb(task));
            return task;
        }

        public void EndInvoke(IAsyncResult iar)
        {
            CheckState(iar.AsyncState);
            ((Task)iar).GetAwaiter().GetResult();
        }

        #endregion
    }

    /// <summary>
    /// Extends the base class to implement that action form of APM with three parameters
    /// </summary>
    public class AsyncAction<T1, T2, T3> : AsyncWork
    {
        private Action<T1, T2, T3> _action;

        public AsyncAction(bool throwing)
            : base()
        {
            _action = (o1, o2, o3) =>
            {
                AddInput(o1);
                AddInput(o2);
                AddInput(o3);

                InvokeAction(throwing);
            };
        }

        #region APM  

        public IAsyncResult BeginInvoke(T1 t1, T2 t2, T3 t3, AsyncCallback cb, object state)
        {
            Task task = Task.Factory.StartNew(_ => _action(t1, t2, t3), state, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(_ => cb(task));
            return task;
        }

        public void EndInvoke(IAsyncResult iar)
        {
            CheckState(iar.AsyncState);
            ((Task)iar).GetAwaiter().GetResult();
        }

        #endregion
    }

    #endregion

    #region AsyncFunc

    /// <summary>
    /// Extends the base class to implement that function form of APM 
    /// </summary>
    public class AsyncFunc : AsyncWork
    {
        private Func<ReadOnlyCollection<object>> _func;

        // a general func to take-in inputs upfront rather than delayed until BeginInvoke
        // for testing the overload taking IAsyncResult
        public AsyncFunc(object[] inputs, bool throwing)
            : base()
        {
            _func = () =>
            {
                foreach (object o in inputs)
                {
                    AddInput(o);
                }

                return InvokeFunc(throwing);
            };
        }

        public AsyncFunc(bool throwing)
            : base()
        {
            _func = () =>
            {
                return InvokeFunc(throwing);
            };
        }

        #region APM

        public IAsyncResult BeginInvoke(AsyncCallback cb, object state)
        {
            Task<ReadOnlyCollection<object>> task = Task.Factory.StartNew(_ => _func(), state, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(_ => cb(task));
            return task;
        }

        public ReadOnlyCollection<object> EndInvoke(IAsyncResult iar)
        {
            CheckState(iar.AsyncState);
            return ((Task<ReadOnlyCollection<object>>)iar).GetAwaiter().GetResult();
        }

        #endregion
    }

    /// <summary>
    /// Extends the base class to implement that function form of APM with one parameter
    /// </summary>
    public class AsyncFunc<T> : AsyncWork
    {
        private Func<T, ReadOnlyCollection<object>> _func;

        public AsyncFunc(bool throwing)
            : base()
        {
            _func = (o) =>
            {
                AddInput(o);

                return InvokeFunc(throwing);
            };
        }

        #region APM

        public IAsyncResult BeginInvoke(T t, AsyncCallback cb, object state)
        {
            Task<ReadOnlyCollection<object>> task = Task.Factory.StartNew(_ => _func(t), state, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(_ => cb(task));
            return task;
        }

        public ReadOnlyCollection<object> EndInvoke(IAsyncResult iar)
        {
            CheckState(iar.AsyncState);
            return ((Task<ReadOnlyCollection<object>>)iar).GetAwaiter().GetResult();
        }

        #endregion
    }

    /// <summary>
    /// Extends the base class to implement that function form of APM with two parameters
    /// </summary>
    public class AsyncFunc<T1, T2> : AsyncWork
    {
        private Func<T1, T2, ReadOnlyCollection<object>> _func;

        public AsyncFunc(bool throwing)
            : base()
        {
            _func = (o1, o2) =>
            {
                AddInput(o1);
                AddInput(o2);

                return InvokeFunc(throwing);
            };
        }

        #region APM

        public IAsyncResult BeginInvoke(T1 t1, T2 t2, AsyncCallback cb, object state)
        {
            Task<ReadOnlyCollection<object>> task = Task.Factory.StartNew(_ => _func(t1, t2), state, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(_ => cb(task));
            return task;
        }

        public ReadOnlyCollection<object> EndInvoke(IAsyncResult iar)
        {
            CheckState(iar.AsyncState);
            return ((Task<ReadOnlyCollection<object>>)iar).GetAwaiter().GetResult();
        }

        #endregion
    }

    /// <summary>
    /// Extends the base class to implement that function form of APM with three parameters
    /// </summary>
    public class AsyncFunc<T1, T2, T3> : AsyncWork
    {
        private Func<T1, T2, T3, ReadOnlyCollection<object>> _func;

        public AsyncFunc(bool throwing)
            : base()
        {
            _func = (o1, o2, o3) =>
            {
                AddInput(o1);
                AddInput(o2);
                AddInput(o3);

                return InvokeFunc(throwing);
            };
        }

        #region APM

        public IAsyncResult BeginInvoke(T1 t1, T2 t2, T3 t3, AsyncCallback cb, object state)
        {
            Task<ReadOnlyCollection<object>> task = Task.Factory.StartNew(_ => _func(t1, t2, t3), state, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(_ => cb(task));
            return task;
        }

        public ReadOnlyCollection<object> EndInvoke(IAsyncResult iar)
        {
            CheckState(iar.AsyncState);
            return ((Task<ReadOnlyCollection<object>>)iar).GetAwaiter().GetResult();
        }

        #endregion
    }

    #endregion
}


