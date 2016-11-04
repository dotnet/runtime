// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace System.Threading.Tasks.Tests
{
    public class TaskFactory_FromAsyncTests
    {
        // Exercise the FromAsync() methods in Task and Task<TResult>.
        [Fact]
        public static void RunAPMFactoryTests()
        {
            FakeAsyncClass fac = new FakeAsyncClass();
            Task t = null;
            Task<string> f = null;
            string check;
            object stateObject = new object();

            // Exercise void overload that takes IAsyncResult instead of StartMethod
            t = Task.Factory.FromAsync(fac.StartWrite("", 0, 0, null, null), delegate (IAsyncResult iar) { });
            t.Wait();
            check = fac.ToString();
            Assert.Equal(0, check.Length);

            //CreationOption overload
            t = Task.Factory.FromAsync(fac.StartWrite("", 0, 0, null, null), delegate (IAsyncResult iar) { }, TaskCreationOptions.None);
            t.Wait();
            check = fac.ToString();
            Assert.Equal(0, check.Length);

            // Exercise 0-arg void option
            t = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, stateObject);
            t.Wait();
            Assert.Equal(0, check.Length);
            Assert.Same(stateObject, ((IAsyncResult)t).AsyncState);

            // Exercise 1-arg void option
            Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "1234", stateObject).Wait();
            check = fac.ToString();
            Assert.Equal("1234", check);
            Assert.Same(stateObject, ((IAsyncResult)t).AsyncState);

            // Exercise 2-arg void option
            Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "aaaabcdef",
                4, stateObject).Wait();
            check = fac.ToString();
            Assert.Equal("1234aaaa", check);
            Assert.Same(stateObject, ((IAsyncResult)t).AsyncState);

            // Exercise 3-arg void option
            Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "abcdzzzz",
                4,
                4,
                stateObject).Wait();
            check = fac.ToString();
            Assert.Equal("1234aaaazzzz", check);
            Assert.Same(stateObject, ((IAsyncResult)t).AsyncState);

            // Read side, exercises getting return values from EndMethod
            char[] carray = new char[100];

            // Exercise 3-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                4, // maxchars
                carray,
                0,
                stateObject);
            string s = f.Result;
            Assert.Equal("1234", s);
            Assert.Equal('1', carray[0]);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // Exercise 2-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                4,
                carray,
                stateObject);
            s = f.Result;
            Assert.Equal("aaaa", s);
            Assert.Equal('a', carray[0]);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // Exercise 1-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                1,
                stateObject);
            s = f.Result;
            Assert.Equal("z", s);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // Exercise 0-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                stateObject);
            s = f.Result;
            Assert.Equal("zzz", s);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            //
            // Do all of the read tests again, except with Task.Factory.FromAsync<string>(), instead of Task<string>.Factory.FromAsync().
            //
            fac.EndWrite(fac.StartWrite("12345678aaaaAAAAzzzz", null, null));

            // Exercise 3-arg value option
            f = Task.Factory.FromAsync<int, char[], int, string>(
                //f = Task.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                4, // maxchars
                carray,
                0,
                stateObject);

            s = f.Result;
            Assert.Equal("1234", s);
            Assert.Equal('1', carray[0]);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // one more with the creationOptions overload
            f = Task.Factory.FromAsync<int, char[], int, string>(
              //f = Task.Factory.FromAsync(
              fac.StartRead,
              fac.EndRead,
              4, // maxchars
              carray,
              0,
              stateObject,
              TaskCreationOptions.None);

            s = f.Result;
            Assert.Equal("5678", s);
            Assert.Equal('5', carray[0]);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // Exercise 2-arg value option
            f = Task.Factory.FromAsync<int, char[], string>(
                fac.StartRead,
                fac.EndRead,
                4,
                carray,
                stateObject);
            s = f.Result;
            Assert.Equal("aaaa", s);
            Assert.Equal('a', carray[0]);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            //one more with the creation option overload
            f = Task.Factory.FromAsync<int, char[], string>(
               fac.StartRead,
               fac.EndRead,
               4,
               carray,
               stateObject,
               TaskCreationOptions.None);
            s = f.Result;
            Assert.Equal("AAAA", s);
            Assert.Equal('A', carray[0]);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // Exercise 1-arg value option
            f = Task.Factory.FromAsync<int, string>(
                fac.StartRead,
                fac.EndRead,
                1,
                stateObject);
            s = f.Result;
            Assert.Equal("z", s);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // one more with creation option overload
            f = Task.Factory.FromAsync<int, string>(
             fac.StartRead,
             fac.EndRead,
             1,
             stateObject,
             TaskCreationOptions.None);
            s = f.Result;
            Assert.Equal("z", s);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // Exercise 0-arg value option
            f = Task.Factory.FromAsync<string>(
                fac.StartRead,
                fac.EndRead,
                stateObject);
            s = f.Result;
            Assert.Equal("zz", s);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            //one more with Creation options overload
            f = Task.Factory.FromAsync<string>(
               fac.StartRead,
               fac.EndRead,
               stateObject,
               TaskCreationOptions.None);
            s = f.Result;
            Assert.Equal(string.Empty, s);
            Assert.Same(stateObject, ((IAsyncResult)f).AsyncState);

            // Inject a few more characters into the buffer
            fac.EndWrite(fac.StartWrite("0123456789", null, null));


            // Exercise value overload that accepts an IAsyncResult instead of a beginMethod.
            f = Task<string>.Factory.FromAsync(
                fac.StartRead(4, null, null),
                fac.EndRead);
            s = f.Result;
            Assert.Equal("0123", s);

            f = Task.Factory.FromAsync<string>(
                fac.StartRead(4, null, null),
                fac.EndRead);
            s = f.Result;
            Assert.Equal("4567", s);

            // Test Exception handling from beginMethod
            Assert.ThrowsAsync<NullReferenceException>(() =>
               t = Task.Factory.FromAsync(
                   fac.StartWrite,
                   fac.EndWrite,
                   (string)null,  // will cause null.Length to be dereferenced
                   null));


            // Test Exception handling from asynchronous logic
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                10,
                carray,
                200, // offset past end of array
                null);

            Assert.Throws<AggregateException>(() =>
               check = f.Result);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
               Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, null, TaskCreationOptions.LongRunning));

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
              Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, null, TaskCreationOptions.PreferFairness));

            // Empty the buffer, then inject a few more characters into the buffer
            fac.ResetStateTo("0123456789");

            Task asyncTask = null;

            //
            // Now check that the endMethod throwing an OCE correctly results in a canceled task.
            //

            // Test IAsyncResult overload that returns Task
            asyncTask = Task.Factory.FromAsync(
                fac.StartWrite("abc", null, null),
                delegate (IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); });

            AggregateException ae = Assert.Throws<AggregateException>(() =>
            {
                asyncTask.Wait();
            });
            Assert.Equal(typeof(TaskCanceledException), ae.InnerException.GetType());
            Assert.Equal(TaskStatus.Canceled, asyncTask.Status);

            // Test beginMethod overload that returns Task
            asyncTask = Task.Factory.FromAsync(
                fac.StartWrite,
                delegate (IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); },
                "abc",
                null);

            ae = Assert.Throws<AggregateException>(() =>
            {
                asyncTask.Wait();
            });
            Assert.Equal(typeof(TaskCanceledException), ae.InnerException.GetType());
            Assert.Equal(TaskStatus.Canceled, asyncTask.Status);

            // Test IAsyncResult overload that returns Task<string>
            Task<string> asyncFuture = null;
            asyncFuture = Task<string>.Factory.FromAsync(
                fac.StartRead(3, null, null),
                delegate (IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); });

            ae = Assert.Throws<AggregateException>(() =>
            {
                asyncTask.Wait();
            });
            Assert.Equal(typeof(TaskCanceledException), ae.InnerException.GetType());
            Assert.Equal(TaskStatus.Canceled, asyncTask.Status);

            // Test beginMethod overload that returns Task<string>
            asyncFuture = null;
            asyncFuture = Task<string>.Factory.FromAsync(
                fac.StartRead,
                delegate (IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); },
                3, null);

            ae = Assert.Throws<AggregateException>(() =>
            {
                asyncFuture.Wait();
            });
            Assert.Equal(typeof(TaskCanceledException), ae.InnerException.GetType());
            Assert.Equal(TaskStatus.Canceled, asyncFuture.Status);

            //
            // Make sure that tasks aren't left hanging if StartXYZ() throws an exception
            //
            Task foo = Task.Factory.StartNew(delegate
            {
                // Every one of these should throw an exception from StartWrite/StartRead.  Test to
                // see that foo is allowed to complete (i.e., no dangling attached tasks from FromAsync()
                // calls.
                Task foo1 = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, (string)null, null, TaskCreationOptions.AttachedToParent);
                Task foo2 = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, (string)null, 4, null, TaskCreationOptions.AttachedToParent);
                Task foo3 = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, (string)null, 4, 4, null, TaskCreationOptions.AttachedToParent);
                Task<string> foo4 = Task<string>.Factory.FromAsync(fac.StartRead, fac.EndRead, -1, null, TaskCreationOptions.AttachedToParent);
                Task<string> foo5 = Task<string>.Factory.FromAsync(fac.StartRead, fac.EndRead, -1, (char[])null, null, TaskCreationOptions.AttachedToParent);
                Task<string> foo6 = Task<string>.Factory.FromAsync(fac.StartRead, fac.EndRead, -1, (char[])null, 200, null, TaskCreationOptions.AttachedToParent);
            });

            Debug.WriteLine("RunAPMFactoryTests: Waiting on task w/ faulted FromAsync() calls.  If we hang, there is a problem");

            Assert.Throws<AggregateException>(() =>
            {
                foo.Wait();
            });
        }

        // This class is used in testing APM Factory tests.
        private class FakeAsyncClass
        {
            private List<char> _list = new List<char>();

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                lock (_list)
                {
                    for (int i = 0; i < _list.Count; i++)
                        sb.Append(_list[i]);
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
                if (s == null)
                    throw new ArgumentNullException(nameof(s));

                Task t = Task.Factory.StartNew(delegate
                {
                    //Task.Delay(100).Wait();
                    try
                    {
                        lock (_list)
                        {
                            for (int i = 0; i < length; i++)
                                _list.Add(s[i + offset]);
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
                if (mar.IsFaulted)
                    throw (mar.Exception);
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
                if (maxBytes == -1)
                    throw new ArgumentException("Value was not valid", nameof(maxBytes));

                Task t = Task.Factory.StartNew(delegate
                {
                    //Thread.Sleep(100);
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
                if (mar.IsFaulted)
                    throw (mar.Exception);
                return (string)mar.AsyncState;
            }

            public void ResetStateTo(string s)
            {
                _list.Clear();
                for (int i = 0; i < s.Length; i++)
                    _list.Add(s[i]);
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
                if (_callback != null)
                    _callback(this);
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
                if (_exception != null)
                    throw (_exception);
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

        [ActiveIssue("https://github.com/dotnet/coreclr/issues/7892")] // BinaryCompatibility reverting FromAsync to .NET 4 behavior, causing invokesCallback=false to fail
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FromAsync_CompletedSynchronouslyIAsyncResult_CompletesSynchronously(bool invokesCallback)
        {
            Task t = Task.Factory.FromAsync((callback, state) =>
            {
                var ar = new SynchronouslyCompletedAsyncResult { AsyncState = state };
                if (invokesCallback) callback(ar);
                return ar;
            }, iar => { }, null);
            Assert.Equal(TaskStatus.RanToCompletion, t.Status);
        }

        private sealed class SynchronouslyCompletedAsyncResult : IAsyncResult
        {
            public object AsyncState { get; internal set; }
            public bool CompletedSynchronously => true;
            public bool IsCompleted => true;
            public WaitHandle AsyncWaitHandle { get { throw new NotImplementedException(); } }
        }
    }
}
