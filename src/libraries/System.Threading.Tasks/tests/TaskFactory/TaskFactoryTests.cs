// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Text;

namespace System.Threading.Tasks.Tests
{
    public class TaskFactoryTests
    {
        #region Test Methods

        // Exercise functionality of TaskFactory and TaskFactory<TResult>
        [Fact]
        public static void RunTaskFactoryTests()
        {
            TaskScheduler tm = TaskScheduler.Default;
            TaskCreationOptions tco = TaskCreationOptions.LongRunning;
            TaskFactory tf;
            TaskFactory<int> tfi;

            tf = new TaskFactory();
            ExerciseTaskFactory(tf, TaskScheduler.Current, TaskCreationOptions.None, CancellationToken.None, TaskContinuationOptions.None);

            CancellationTokenSource cancellationSrc = new CancellationTokenSource();
            tf = new TaskFactory(cancellationSrc.Token);
            var task = tf.StartNew(() => { });
            task.Wait();

            // Exercising TF(scheduler)
            tf = new TaskFactory(tm);
            ExerciseTaskFactory(tf, tm, TaskCreationOptions.None, CancellationToken.None, TaskContinuationOptions.None);

            //Exercising TF(TCrO, TCoO)
            tf = new TaskFactory(tco, TaskContinuationOptions.None);
            ExerciseTaskFactory(tf, TaskScheduler.Current, tco, CancellationToken.None, TaskContinuationOptions.None);

            // Exercising TF(scheduler, TCrO, TCoO)"
            tf = new TaskFactory(CancellationToken.None, tco, TaskContinuationOptions.None, tm);
            ExerciseTaskFactory(tf, tm, tco, CancellationToken.None, TaskContinuationOptions.None);

            //TaskFactory<TResult> tests

            // Exercising TF<int>()
            tfi = new TaskFactory<int>();
            ExerciseTaskFactoryInt(tfi, TaskScheduler.Current, TaskCreationOptions.None, CancellationToken.None, TaskContinuationOptions.None);

            //Test constructor that accepts cancellationToken

            // Exercising TF<int>(cancellationToken) with a noncancelled token 
            cancellationSrc = new CancellationTokenSource();
            tfi = new TaskFactory<int>(cancellationSrc.Token);
            task = tfi.StartNew(() => 0);
            task.Wait();

            // Exercising TF<int>(scheduler)
            tfi = new TaskFactory<int>(tm);
            ExerciseTaskFactoryInt(tfi, tm, TaskCreationOptions.None, CancellationToken.None, TaskContinuationOptions.None);

            // Exercising TF<int>(TCrO, TCoO)
            tfi = new TaskFactory<int>(tco, TaskContinuationOptions.None);
            ExerciseTaskFactoryInt(tfi, TaskScheduler.Current, tco, CancellationToken.None, TaskContinuationOptions.None);

            // Exercising TF<int>(scheduler, TCrO, TCoO)
            tfi = new TaskFactory<int>(CancellationToken.None, tco, TaskContinuationOptions.None, tm);
            ExerciseTaskFactoryInt(tfi, tm, tco, CancellationToken.None, TaskContinuationOptions.None);
        }

        // Exercise functionality of TaskFactory and TaskFactory<TResult>
        [Fact]
        public static void RunTaskFactoryTests_Cancellation_Negative()
        {
            CancellationTokenSource cancellationSrc = new CancellationTokenSource();

            //Test constructor that accepts cancellationToken
            cancellationSrc.Cancel();
            TaskFactory tf = new TaskFactory(cancellationSrc.Token);
            var cancelledTask = tf.StartNew(() => { });
            EnsureTaskCanceledExceptionThrown(() => cancelledTask.Wait());

            // Exercising TF<int>(cancellationToken) with a cancelled token
            cancellationSrc.Cancel();
            TaskFactory<int> tfi = new TaskFactory<int>(cancellationSrc.Token);
            cancelledTask = tfi.StartNew(() => 0);
            EnsureTaskCanceledExceptionThrown(() => cancelledTask.Wait());
        }

        [Fact]
        public static void RunTaskFactoryExceptionTests()
        {
            TaskFactory tf = new TaskFactory();

            // Checking top-level TF exception handling.
            Assert.Throws<ArgumentOutOfRangeException>(
               () => tf = new TaskFactory((TaskCreationOptions)0x40000000, TaskContinuationOptions.None));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tf = new TaskFactory((TaskCreationOptions)0x100, TaskContinuationOptions.None));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tf = new TaskFactory(TaskCreationOptions.None, (TaskContinuationOptions)0x40000000));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tf = new TaskFactory(TaskCreationOptions.None, TaskContinuationOptions.NotOnFaulted));

            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync(null, (obj) => { }, TaskCreationOptions.None); });

            // testing exceptions in null endMethods

            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync(new myAsyncResult((obj) => { }, null), null, TaskCreationOptions.None); });

            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<int>(new myAsyncResult((obj) => { }, null), null, TaskCreationOptions.None); });

            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<int>(new myAsyncResult((obj) => { }, null), null, TaskCreationOptions.None, null); });

            TaskFactory<int> tfi = new TaskFactory<int>();

            // Checking top-level TF<int> exception handling.
            Assert.Throws<ArgumentOutOfRangeException>(
               () => tfi = new TaskFactory<int>((TaskCreationOptions)0x40000000, TaskContinuationOptions.None));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tfi = new TaskFactory<int>((TaskCreationOptions)0x100, TaskContinuationOptions.None));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tfi = new TaskFactory<int>(TaskCreationOptions.None, (TaskContinuationOptions)0x40000000));

            Assert.Throws<ArgumentOutOfRangeException>(
               () => tfi = new TaskFactory<int>(TaskCreationOptions.None, TaskContinuationOptions.NotOnFaulted));
        }

        [Fact]
        public static void RunTaskFactoryFromAsyncExceptionTests()
        {
            // Checking TF special FromAsync exception handling."
            FakeAsyncClass fac = new FakeAsyncClass();
            TaskFactory tf;

            tf = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tf.FromAsync(fac.StartWrite, fac.EndWrite, null /* state */); });

            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", null /* state */); });

            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", 2, null /* state */); });

            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tf.FromAsync(fac.StartWrite, fac.EndWrite, "abc", 0, 2, null /* state */); });

            // testing exceptions in null endMethods or begin method
            //0 parameter
            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<string>(fac.StartWrite, null, (object)null, TaskCreationOptions.None); });
            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<string>(null, fac.EndRead, (object)null, TaskCreationOptions.None); });

            //1 parameter
            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<string, int>(fac.StartWrite, null, "arg1", (object)null, TaskCreationOptions.None); });
            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<string, string>(null, fac.EndRead, "arg1", (object)null, TaskCreationOptions.None); });

            //2 parameters
            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<string, int, int>(fac.StartWrite, null, "arg1", 1, (object)null, TaskCreationOptions.None); });
            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<string, string, string>(null, fac.EndRead, "arg1", "arg2", (object)null, TaskCreationOptions.None); });

            //3 parameters
            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<string, int, int, int>(fac.StartWrite, null, "arg1", 1, 2, (object)null, TaskCreationOptions.None); });
            Assert.Throws<ArgumentNullException>(
               () => { tf.FromAsync<string, string, string, string>(null, fac.EndRead, "arg1", "arg2", "arg3", (object)null, TaskCreationOptions.None); });

            // Checking TF<string> special FromAsync exception handling.
            TaskFactory<string> tfs = new TaskFactory<string>(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            char[] charbuf = new char[128];

            // Test that we throw on bad default task options
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tfs.FromAsync(fac.StartRead, fac.EndRead, null); });
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tfs.FromAsync(fac.StartRead, fac.EndRead, 64, null); });
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tfs.FromAsync(fac.StartRead, fac.EndRead, 64, charbuf, null); });
            Assert.Throws<ArgumentOutOfRangeException>(
               () => { tfs.FromAsync(fac.StartRead, fac.EndRead, 64, charbuf, 0, null); });

            // Test that we throw on null endMethod
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartRead, null, null); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartRead, null, 64, null); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartRead, null, 64, charbuf, null); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartRead, null, 64, charbuf, 0, null); });

            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(null, (obj) => "", TaskCreationOptions.None); });

            //test null begin or end methods with various overloads
            //0 parameter
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(fac.StartWrite, null, null, TaskCreationOptions.None); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync(null, fac.EndRead, null, TaskCreationOptions.None); });

            //1 parameter
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync<string>(fac.StartWrite, null, "arg1", null, TaskCreationOptions.None); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync<string>(null, fac.EndRead, "arg1", null, TaskCreationOptions.None); });

            //2 parameters
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync<string, int>(fac.StartWrite, null, "arg1", 2, null, TaskCreationOptions.None); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync<string, int>(null, fac.EndRead, "arg1", 2, null, TaskCreationOptions.None); });

            //3 parameters
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync<string, int, int>(fac.StartWrite, null, "arg1", 2, 3, null, TaskCreationOptions.None); });
            Assert.Throws<ArgumentNullException>(
               () => { tfs.FromAsync<string, int, int>(null, fac.EndRead, "arg1", 2, 3, null, TaskCreationOptions.None); });
        }

        #endregion

        #region Helper Methods

        // Utility method for RunTaskFactoryTests().
        private static void ExerciseTaskFactory(TaskFactory tf, TaskScheduler tmDefault, TaskCreationOptions tcoDefault, CancellationToken tokenDefault, TaskContinuationOptions continuationDefault)
        {
            TaskScheduler myTM = TaskScheduler.Default;
            TaskCreationOptions myTCO = TaskCreationOptions.LongRunning;
            TaskScheduler tmObserved = null;
            Task t;
            Task<int> f;

            //
            // Helper delegates to make the code below a lot shorter
            //

            Action init = delegate { tmObserved = null; };

            Action void_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
            };
            Action<object> voidState_delegate = delegate (object o)
            {
                tmObserved = TaskScheduler.Current;
            };
            Func<int> int_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };
            Func<object, int> intState_delegate = delegate (object o)
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };

            //check Factory properties
            Assert.Equal(tf.CreationOptions, tcoDefault);
            if (tf.Scheduler != null)
            {
                Assert.Equal(tmDefault, tf.Scheduler);
            }
            Assert.Equal(tokenDefault, tf.CancellationToken);
            Assert.Equal(continuationDefault, tf.ContinuationOptions);


            //
            // StartNew(action)
            //
            init();
            t = tf.StartNew(void_delegate);
            t.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(t.CreationOptions, tcoDefault);

            //
            // StartNew(action, TCO)
            //
            init();
            t = tf.StartNew(void_delegate, myTCO);
            t.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(t.CreationOptions, myTCO);

            //
            // StartNew(action, CT, TCO, scheduler)
            //
            init();
            t = tf.StartNew(void_delegate, CancellationToken.None, myTCO, myTM);
            t.Wait();
            Assert.Equal(tmObserved, myTM);
            Assert.Equal(t.CreationOptions, myTCO);

            //
            // StartNew(action<object>, object)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100);
            t.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(t.CreationOptions, tcoDefault);

            //
            // StartNew(action<object>, object, TCO)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100, myTCO);
            t.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(t.CreationOptions, myTCO);

            //
            // StartNew(action<object>, object, CT, TCO, scheduler)
            //
            init();
            t = tf.StartNew(voidState_delegate, 100, CancellationToken.None, myTCO, myTM);
            t.Wait();
            Assert.Equal(tmObserved, myTM);
            Assert.Equal(t.CreationOptions, myTCO);

            //
            // StartNew(func)
            //
            init();
            f = tf.StartNew(int_delegate);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, tcoDefault);

            //
            // StartNew(func, token)
            //
            init();
            f = tf.StartNew(int_delegate, tokenDefault);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, tcoDefault);

            //
            // StartNew(func, options)
            //
            init();
            f = tf.StartNew(int_delegate, myTCO);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, myTCO);

            //
            // StartNew(func, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(int_delegate, CancellationToken.None, myTCO, myTM);
            f.Wait();
            Assert.Equal(tmObserved, myTM);
            Assert.Equal(f.CreationOptions, myTCO);

            //
            // StartNew(func<object>, object)
            //
            init();
            f = tf.StartNew(intState_delegate, 100);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, tcoDefault);

            //
            // StartNew(func<object>, object, token)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, tokenDefault);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, tcoDefault);

            //
            // StartNew(func<object>, object, options)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, myTCO);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, myTCO);

            //
            // StartNew(func<object>, object, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, CancellationToken.None, myTCO, myTM);
            f.Wait();
            Assert.Equal(tmObserved, myTM);
            Assert.Equal(f.CreationOptions, myTCO);
        }

        // Utility method for RunTaskFactoryTests().
        private static void ExerciseTaskFactoryInt(TaskFactory<int> tf, TaskScheduler tmDefault, TaskCreationOptions tcoDefault, CancellationToken tokenDefault, TaskContinuationOptions continuationDefault)
        {
            TaskScheduler myTM = TaskScheduler.Default;
            TaskCreationOptions myTCO = TaskCreationOptions.LongRunning;
            TaskScheduler tmObserved = null;
            Task<int> f;

            // Helper delegates to make the code shorter.

            Action init = delegate { tmObserved = null; };

            Func<int> int_delegate = delegate
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };
            Func<object, int> intState_delegate = delegate (object o)
            {
                tmObserved = TaskScheduler.Current;
                return 10;
            };

            //check Factory properties
            Assert.Equal(tf.CreationOptions, tcoDefault);
            if (tf.Scheduler != null)
            {
                Assert.Equal(tmDefault, tf.Scheduler);
            }
            Assert.Equal(tokenDefault, tf.CancellationToken);
            Assert.Equal(continuationDefault, tf.ContinuationOptions);

            //
            // StartNew(func)
            //
            init();
            f = tf.StartNew(int_delegate);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, tcoDefault);

            //
            // StartNew(func, options)
            //
            init();
            f = tf.StartNew(int_delegate, myTCO);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, myTCO);

            //
            // StartNew(func, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(int_delegate, CancellationToken.None, myTCO, myTM);
            f.Wait();
            Assert.Equal(tmObserved, myTM);
            Assert.Equal(f.CreationOptions, myTCO);

            //
            // StartNew(func<object>, object)
            //
            init();
            f = tf.StartNew(intState_delegate, 100);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, tcoDefault);

            //
            // StartNew(func<object>, object, token)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, tokenDefault);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, tcoDefault);

            //
            // StartNew(func<object>, object, options)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, myTCO);
            f.Wait();
            Assert.Equal(tmObserved, tmDefault);
            Assert.Equal(f.CreationOptions, myTCO);

            //
            // StartNew(func<object>, object, CT, options, scheduler)
            //
            init();
            f = tf.StartNew(intState_delegate, 100, CancellationToken.None, myTCO, myTM);
            f.Wait();
            Assert.Equal(tmObserved, myTM);
            Assert.Equal(f.CreationOptions, myTCO);
        }

        // Ensures that the specified action throws a AggregateException wrapping a TaskCanceledException
        private static void EnsureTaskCanceledExceptionThrown(Action action)
        {
            AggregateException ae = Assert.Throws<AggregateException>(action);

            Assert.Equal(typeof(TaskCanceledException), ae.InnerException.GetType());
        }

        // This class is used in testing Factory tests.
        private class FakeAsyncClass
        {
            private List<char> _list = new List<char>();

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                lock (_list)
                {
                    for (int i = 0; i < _list.Count; i++) sb.Append(_list[i]);
                }
                return sb.ToString();
            }

            // Silly use of Write, but I wanted to test no-argument StartXXX handling.
            public IAsyncResult StartWrite(AsyncCallback cb, object o)
            {
                return StartWrite("", 0, 0, cb, o);
            }

            public IAsyncResult StartWrite(string s, AsyncCallback cb, object o)
            {
                return StartWrite(s, 0, s.Length, cb, o);
            }

            public IAsyncResult StartWrite(string s, int length, AsyncCallback cb, object o)
            {
                return StartWrite(s, 0, length, cb, o);
            }

            public IAsyncResult StartWrite(string s, int offset, int length, AsyncCallback cb, object o)
            {
                myAsyncResult mar = new myAsyncResult(cb, o);

                // Allow for exception throwing to test our handling of that.
                if (s == null) throw new ArgumentNullException(nameof(s));

                Task t = Task.Factory.StartNew(delegate
                {
                    try
                    {
                        lock (_list)
                        {
                            for (int i = 0; i < length; i++) _list.Add(s[i + offset]);
                        }
                        mar.Signal();
                    }
                    catch (Exception e) { mar.Signal(e); }
                });


                return mar;
            }

            public void EndWrite(IAsyncResult iar)
            {
                myAsyncResult mar = iar as myAsyncResult;
                mar.Wait();
                if (mar.IsFaulted) throw (mar.Exception);
            }

            public IAsyncResult StartRead(AsyncCallback cb, object o)
            {
                return StartRead(128 /*=maxbytes*/, null, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, AsyncCallback cb, object o)
            {
                return StartRead(maxBytes, null, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, char[] buf, AsyncCallback cb, object o)
            {
                return StartRead(maxBytes, buf, 0, cb, o);
            }

            public IAsyncResult StartRead(int maxBytes, char[] buf, int offset, AsyncCallback cb, object o)
            {
                myAsyncResult mar = new myAsyncResult(cb, o);

                // Allow for exception throwing to test our handling of that.
                if (maxBytes == -1) throw new ArgumentException("Value was not valid", nameof(maxBytes));

                Task t = Task.Factory.StartNew(delegate
                {
                    StringBuilder sb = new StringBuilder();
                    int bytesRead = 0;
                    try
                    {
                        lock (_list)
                        {
                            while ((_list.Count > 0) && (bytesRead < maxBytes))
                            {
                                sb.Append(_list[0]);
                                if (buf != null) { buf[offset] = _list[0]; offset++; }
                                _list.RemoveAt(0);
                                bytesRead++;
                            }
                        }

                        mar.SignalState(sb.ToString());
                    }
                    catch (Exception e) { mar.Signal(e); }
                });

                return mar;
            }

            public string EndRead(IAsyncResult iar)
            {
                myAsyncResult mar = iar as myAsyncResult;
                if (mar.IsFaulted) throw (mar.Exception);
                return (string)mar.AsyncState;
            }

            public void ResetStateTo(string s)
            {
                _list.Clear();
                for (int i = 0; i < s.Length; i++) _list.Add(s[i]);
            }
        }

        // This is an internal class used for a concrete IAsyncResult in the APM Factory tests.
        private class myAsyncResult : IAsyncResult
        {
            private volatile int _isCompleted;
            private ManualResetEvent _asyncWaitHandle;
            private AsyncCallback _callback;
            private object _asyncState;
            private Exception _exception;

            public myAsyncResult(AsyncCallback cb, object o)
            {
                _isCompleted = 0;
                _asyncWaitHandle = new ManualResetEvent(false);
                _callback = cb;
                _asyncState = o;
                _exception = null;
            }

            public bool IsCompleted
            {
                get { return (_isCompleted == 1); }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _asyncWaitHandle; }
            }

            public object AsyncState
            {
                get { return _asyncState; }
            }

            public void Signal()
            {
                _isCompleted = 1;
                _asyncWaitHandle.Set();
                if (_callback != null) _callback(this);
            }

            public void Signal(Exception e)
            {
                _exception = e;
                Signal();
            }

            public void SignalState(object o)
            {
                _asyncState = o;
                Signal();
            }

            public void Wait()
            {
                _asyncWaitHandle.WaitOne();
                if (_exception != null) throw (_exception);
            }

            public bool IsFaulted
            {
                get { return ((_isCompleted == 1) && (_exception != null)); }
            }

            public Exception Exception
            {
                get { return _exception; }
            }
        }

        #endregion
    }
}
