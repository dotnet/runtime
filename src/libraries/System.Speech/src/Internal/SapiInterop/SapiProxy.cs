// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Speech.Internal.SapiInterop
{
    internal abstract class SapiProxy : IDisposable
    {
        #region Constructors

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Internal Methods

        internal abstract object Invoke(ObjectDelegate pfn);
        internal abstract void Invoke2(VoidDelegate pfn);

        #endregion

        #region Internal Properties

        internal ISpRecognizer Recognizer
        {
            get
            {
                return _recognizer;
            }
        }

        internal ISpRecognizer2 Recognizer2 =>
            _recognizer2 ??= (ISpRecognizer2)_recognizer;

        internal ISpeechRecognizer SapiSpeechRecognizer =>
            _speechRecognizer ??= (ISpeechRecognizer)_recognizer;

        #endregion

        #region Protected Fields

        protected ISpeechRecognizer _speechRecognizer;
        protected ISpRecognizer2 _recognizer2;
        protected ISpRecognizer _recognizer;

        #endregion

        #region Protected Fields

        internal class PassThrough : SapiProxy, IDisposable
        {
            #region Constructors

            internal PassThrough(ISpRecognizer recognizer)
            {
                _recognizer = recognizer;
            }

            ~PassThrough()
            {
                Dispose(false);
            }
            public override void Dispose()
            {
                try
                {
                    Dispose(true);
                }
                finally
                {
                    base.Dispose();
                }
            }

            #endregion

            #region Internal Methods

            internal override object Invoke(ObjectDelegate pfn)
            {
                return pfn.Invoke();
            }

            internal override void Invoke2(VoidDelegate pfn)
            {
                pfn.Invoke();
            }

            #endregion

            #region Private Methods

            private void Dispose(bool disposing)
            {
                _recognizer2 = null;
                _speechRecognizer = null;
                Marshal.ReleaseComObject(_recognizer);
            }

            #endregion
        }

#pragma warning disable 56500 // Remove all the catch all statements warnings used by the interop layer

        internal class MTAThread : SapiProxy, IDisposable
        {
            #region Constructors

            internal MTAThread(SapiRecognizer.RecognizerType type)
            {
                _mta = new Thread(new ThreadStart(SapiMTAThread));
                if (!_mta.TrySetApartmentState(ApartmentState.MTA))
                {
                    throw new InvalidOperationException();
                }
                _mta.IsBackground = true;
                _mta.Start();

                if (type == SapiRecognizer.RecognizerType.InProc)
                {
                    Invoke2(delegate { _recognizer = (ISpRecognizer)new SpInprocRecognizer(); });
                }
                else
                {
                    Invoke2(delegate { _recognizer = (ISpRecognizer)new SpSharedRecognizer(); });
                }
            }

            ~MTAThread()
            {
                Dispose(false);
            }

            public override void Dispose()
            {
                try
                {
                    Dispose(true);
                }
                finally
                {
                    base.Dispose();
                }
            }

            #endregion

            #region Internal Methods

            internal override object Invoke(ObjectDelegate pfn)
            {
                lock (this)
                {
                    _doit = pfn;
                    _process.Set();
                    _done.WaitOne();
                    if (_exception == null)
                    {
                        return _result;
                    }
                    else
                    {
                        ExceptionDispatchInfo.Throw(_exception);
                        return null;
                    }
                }
            }

            internal override void Invoke2(VoidDelegate pfn)
            {
                lock (this)
                {
                    _doit2 = pfn;
                    _process.Set();
                    _done.WaitOne();
                    if (_exception != null)
                    {
                        ExceptionDispatchInfo.Throw(_exception);
                    }
                }
            }

            #endregion

            #region Private Methods

            private void Dispose(bool disposing)
            {
                lock (this)
                {
                    _recognizer2 = null;
                    _speechRecognizer = null;
                    Invoke2(delegate { Marshal.ReleaseComObject(_recognizer); });
                    ((IDisposable)_process).Dispose();
                    ((IDisposable)_done).Dispose();
                }
                base.Dispose();
            }

            private void SapiMTAThread()
            {
                while (true)
                {
                    try
                    {
                        _process.WaitOne();
                        _exception = null;
                        if (_doit != null)
                        {
                            _result = _doit.Invoke();
                            _doit = null;
                        }
                        else
                        {
                            _doit2.Invoke();
                            _doit2 = null;
                        }
                    }
                    catch (Exception e)
                    {
                        _exception = e;
                    }
                    try
                    {
                        _done.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }

            #endregion

            #region Private Fields

            private Thread _mta;
            private AutoResetEvent _process = new(false);
            private AutoResetEvent _done = new(false);
            private ObjectDelegate _doit;
            private VoidDelegate _doit2;
            private object _result;
            private Exception _exception;

            #endregion
        }

        internal delegate object ObjectDelegate();
        internal delegate void VoidDelegate();
    }

    #endregion
}
