// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            if (check.Length != 0)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Write1 -- expected empty fac."));
            }

            //CreationOption overload
            t = Task.Factory.FromAsync(fac.StartWrite("", 0, 0, null, null), delegate (IAsyncResult iar) { }, TaskCreationOptions.None);
            t.Wait();
            check = fac.ToString();
            if (check.Length != 0)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Write2 -- expected empty fac."));
            }

            // Exercise 0-arg void option
            t = Task.Factory.FromAsync(fac.StartWrite, fac.EndWrite, stateObject);
            t.Wait();
            if (check.Length != 0)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Write3 -- expected empty fac."));
            }
            else if (((IAsyncResult)t).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Write3 -- state object not stored correctly."));
            }

            // Exercise 1-arg void option
            Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "1234", stateObject).Wait();
            check = fac.ToString();
            if (!check.Equals("1234"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Write4.  Expected fac \"1234\" after wait, got {0}.", check));
            }
            else if (((IAsyncResult)t).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Write4 -- state object not stored correctly."));
            }

            // Exercise 2-arg void option
            Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "aaaabcdef",
                4, stateObject).Wait();
            check = fac.ToString();
            if (!check.Equals("1234aaaa"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Write5.  Expected fac \"1234aaaa\" after wait, got {0}.", check));
            }
            else if (((IAsyncResult)t).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Write5 -- state object not stored correctly."));
            }

            // Exercise 3-arg void option
            Task.Factory.FromAsync(
                fac.StartWrite,
                fac.EndWrite,
                "abcdzzzz",
                4,
                4,
                stateObject).Wait();
            check = fac.ToString();
            if (!check.Equals("1234aaaazzzz"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Write6.  Expected fac \"1234aaaazzzz\" after wait, got {0}.", check));
            }
            else if (((IAsyncResult)t).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Write6 -- state object not stored correctly."));
            }

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
            if (!s.Equals("1234"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read1.  Expected Result = \"1234\", got {0}.", s));
            }
            else if (carray[0] != '1')
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read1.  Expected carray[0] = '1', got {0}.", carray[0]));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read1 -- state object not stored correctly."));
            }

            // Exercise 2-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                4,
                carray,
                stateObject);
            s = f.Result;
            if (!s.Equals("aaaa"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read2.  Expected Result = \"aaaa\", got {0}.", s));
            }
            else if (carray[0] != 'a')
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read2.  Expected carray[0] = 'a', got {0}.", carray[0]));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read2 -- state object not stored correctly."));
            }

            // Exercise 1-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                1,
                stateObject);
            s = f.Result;
            if (!s.Equals("z"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read3.  Expected Result = \"z\", got {0}.", s));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read3 -- state object not stored correctly."));
            }

            // Exercise 0-arg value option
            f = Task<string>.Factory.FromAsync(
                fac.StartRead,
                fac.EndRead,
                stateObject);
            s = f.Result;
            if (!s.Equals("zzz"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read4.  Expected Result = \"zzz\", got {0}.", s));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read4 -- state object not stored correctly."));
            }

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
            if (!s.Equals("1234"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read1a.  Expected Result = \"1234\", got {0}.", s));
            }
            else if (carray[0] != '1')
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read1a.  Expected carray[0] = '1', got {0}.", carray[0]));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read1a -- state object not stored correctly."));
            }

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
            if (!s.Equals("5678"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read1b.  Expected Result = \"1234\", got {0}.", s));
            }
            else if (carray[0] != '5')
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read1b.  Expected carray[0] = '1', got {0}.", carray[0]));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read1b -- state object not stored correctly."));
            }


            // Exercise 2-arg value option
            f = Task.Factory.FromAsync<int, char[], string>(
                fac.StartRead,
                fac.EndRead,
                4,
                carray,
                stateObject);
            s = f.Result;
            if (!s.Equals("aaaa"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read2a.  Expected Result = \"aaaa\", got {0}.", s));
            }
            else if (carray[0] != 'a')
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read2a.  Expected carray[0] = 'a', got {0}.", carray[0]));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read2a -- state object not stored correctly."));
            }

            //one more with the creation option overload
            f = Task.Factory.FromAsync<int, char[], string>(
               fac.StartRead,
               fac.EndRead,
               4,
               carray,
               stateObject,
               TaskCreationOptions.None);
            s = f.Result;
            if (!s.Equals("AAAA"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read2b.  Expected Result = \"aaaa\", got {0}.", s));
            }
            else if (carray[0] != 'A')
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read2b.  Expected carray[0] = 'a', got {0}.", carray[0]));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read2b -- state object not stored correctly."));
            }

            // Exercise 1-arg value option
            f = Task.Factory.FromAsync<int, string>(
                fac.StartRead,
                fac.EndRead,
                1,
                stateObject);
            s = f.Result;
            if (!s.Equals("z"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read3a.  Expected Result = \"z\", got {0}.", s));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read3a -- state object not stored correctly."));
            }

            // one more with creation option overload
            f = Task.Factory.FromAsync<int, string>(
             fac.StartRead,
             fac.EndRead,
             1,
             stateObject,
             TaskCreationOptions.None);
            s = f.Result;
            if (!s.Equals("z"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read3b.  Expected Result = \"z\", got {0}.", s));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read3b -- state object not stored correctly."));
            }

            // Exercise 0-arg value option
            f = Task.Factory.FromAsync<string>(
                fac.StartRead,
                fac.EndRead,
                stateObject);
            s = f.Result;
            if (!s.Equals("zz"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read4a.  Expected Result = \"zz\", got {0}.", s));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read4a -- state object not stored correctly."));
            }

            //one more with Creation options overload
            f = Task.Factory.FromAsync<string>(
               fac.StartRead,
               fac.EndRead,
               stateObject,
               TaskCreationOptions.None);
            s = f.Result;
            if (!s.Equals(""))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read4b.  Expected Result = \"\", got {0}.", s));
            }
            else if (((IAsyncResult)f).AsyncState != stateObject)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED on Read4b -- state object not stored correctly."));
            }

            // Inject a few more characters into the buffer
            fac.EndWrite(fac.StartWrite("0123456789", null, null));


            // Exercise value overload that accepts an IAsyncResult instead of a beginMethod.
            f = Task<string>.Factory.FromAsync(
                fac.StartRead(4, null, null),
                fac.EndRead);
            s = f.Result;
            if (!s.Equals("0123"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read5.  Expected Result = \"0123\", got {0}.", s));
            }

            f = Task.Factory.FromAsync<string>(
                fac.StartRead(4, null, null),
                fac.EndRead);
            s = f.Result;
            if (!s.Equals("4567"))
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED @ Read5a.  Expected Result = \"4567\", got {0}.", s));
            }

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

            // 
            // Test that parent cancellation flows correctly through FromAsync()
            //

            // Allow some time for whatever happened above to sort itself out.
            //Thread.Sleep(200);

            // Empty the buffer, then inject a few more characters into the buffer
            fac.ResetStateTo("0123456789");

            Task asyncTask = null;

            // Now check to see that the cancellation behaved like we thought it would -- even though the tasks were canceled,
            // the operations still took place.
            //
            // I'm commenting this one out because it has some timing problems -- if some things above get delayed,
            // then the final chars in the buffer might not read like this.
            //
            //s = Task<string>.Factory.FromAsync(fac.StartRead(200, null, null), fac.EndRead).Result;
            //if (!s.Equals("89abcdef"))
            //{
            //    Assert.True(false, string.Format("    > FAILED.  Unexpected result after cancellations: Expected \"89abcdef\", got \"{0}\"", s));
            //    passed = false;
            //}

            //
            // Now check that the endMethod throwing an OCE correctly results in a canceled task.
            //

            // Test IAsyncResult overload that returns Task
            asyncTask = null;
            asyncTask = Task.Factory.FromAsync(
                fac.StartWrite("abc", null, null),
                delegate (IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); });
            try
            {
                asyncTask.Wait();
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED! Expected exception on FAS(iar,endMethod) throwing OCE"));
            }
            catch (Exception) { }
            if (asyncTask.Status != TaskStatus.Canceled)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED! Expected Canceled status on FAS(iar,endMethod) OCE, got {0}", asyncTask.Status));
            }

            // Test beginMethod overload that returns Task
            asyncTask = null;
            asyncTask = Task.Factory.FromAsync(
                fac.StartWrite,
                delegate (IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); },
                "abc",
                null);
            try
            {
                asyncTask.Wait();
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED! Expected exception on FAS(beginMethod,endMethod) throwing OCE"));
            }
            catch (Exception) { }
            if (asyncTask.Status != TaskStatus.Canceled)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED! Expected Canceled status on FAS(beginMethod,endMethod) OCE, got {0}", asyncTask.Status));
            }

            // Test IAsyncResult overload that returns Task<string>
            Task<string> asyncFuture = null;
            asyncFuture = Task<string>.Factory.FromAsync(
                fac.StartRead(3, null, null),
                delegate (IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); });
            try
            {
                asyncFuture.Wait();
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED! Expected exception on FAS<string>(iar,endMethod) throwing OCE"));
            }
            catch (Exception) { }
            if (asyncFuture.Status != TaskStatus.Canceled)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED! Expected Canceled status on FAS<string>(iar,endMethod) OCE, got {0}", asyncFuture.Status));
            }

            // Test beginMethod overload that returns Task<string>
            asyncFuture = null;
            asyncFuture = Task<string>.Factory.FromAsync(
                fac.StartRead,
                delegate (IAsyncResult iar) { throw new OperationCanceledException("FromAsync"); },
                3, null);
            try
            {
                asyncFuture.Wait();
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED! Expected exception on FAS<string>(beginMethod,endMethod) throwing OCE"));
            }
            catch (Exception) { }
            if (asyncFuture.Status != TaskStatus.Canceled)
            {
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED! Expected Canceled status on FAS<string>(beginMethod,endMethod) OCE, got {0}", asyncFuture.Status));
            }


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
            try
            {
                foo.Wait();
                Assert.True(false, string.Format("RunAPMFactoryTests:    > FAILED!  Expected an exception."));
            }
            catch (Exception) { }
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
                    throw new ArgumentNullException("s");

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
                    throw new ArgumentException("maxBytes");

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
    }
}
