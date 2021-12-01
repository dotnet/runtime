// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Recognition;

namespace System.Speech.Internal.SapiInterop
{
    internal class SapiRecognizer : IDisposable
    {
        #region Constructors

        internal SapiRecognizer(RecognizerType type)
        {
            ISpRecognizer recognizer;
            try
            {
                if (type == RecognizerType.InProc)
                {
                    recognizer = (ISpRecognizer)new SpInprocRecognizer();
                }
                else
                {
                    recognizer = (ISpRecognizer)new SpSharedRecognizer();
                }
                _isSap53 = recognizer is ISpRecognizer2;
            }
            catch (COMException e)
            {
                throw RecognizerBase.ExceptionFromSapiCreateRecognizerError(e);
            }

            // Back out if the recognizer we have SAPI 5.1
            if (!IsSapi53 && System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA)
            {
                // must be recreated on a different thread
                Marshal.ReleaseComObject(recognizer);
                _proxy = new SapiProxy.MTAThread(type);
            }
            else
            {
                _proxy = new SapiProxy.PassThrough(recognizer);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _proxy.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Internal Methods

        // ISpProperties Methods
        internal void SetPropertyNum(string name, int value)
        {
            _proxy.Invoke2(delegate { SetProperty(_proxy.Recognizer, name, value); });
        }

        internal int GetPropertyNum(string name)
        {
            return (int)_proxy.Invoke(delegate { return GetProperty(_proxy.Recognizer, name, true); });
        }
        internal void SetPropertyString(string name, string value)
        {
            _proxy.Invoke2(delegate { SetProperty(_proxy.Recognizer, name, value); });
        }

        internal string GetPropertyString(string name)
        {
            return (string)_proxy.Invoke(delegate { return GetProperty(_proxy.Recognizer, name, false); });
        }

        // ISpRecognizer Methods
        internal void SetRecognizer(ISpObjectToken recognizer)
        {
            try
            {
                _proxy.Invoke2(delegate { _proxy.Recognizer.SetRecognizer(recognizer); });
            }
            catch (InvalidCastException)
            {
                // The Interop layer maps the SAPI error that an interface cannot by QI to an Invalid cast exception
                // Map the InvalidCastException
                throw new PlatformNotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI));
            }
        }

        internal RecognizerInfo GetRecognizerInfo()
        {
            ISpObjectToken sapiObjectToken;
            return (RecognizerInfo)_proxy.Invoke(delegate
          {
              RecognizerInfo recognizerInfo;
              _proxy.Recognizer.GetRecognizer(out sapiObjectToken);

              IntPtr sapiTokenId;
              try
              {
                  sapiObjectToken.GetId(out sapiTokenId);
                  string tokenId = Marshal.PtrToStringUni(sapiTokenId);
                  recognizerInfo = RecognizerInfo.Create(ObjectToken.Open(null, tokenId, false));
                  if (recognizerInfo == null)
                  {
                      throw new InvalidOperationException(SR.Get(SRID.RecognizerNotFound));
                  }
                  Marshal.FreeCoTaskMem(sapiTokenId);
              }
              finally
              {
                  Marshal.ReleaseComObject(sapiObjectToken);
              }

              return recognizerInfo;
          });
        }

        internal void SetInput(object input, bool allowFormatChanges)
        {
            _proxy.Invoke2(delegate { _proxy.Recognizer.SetInput(input, allowFormatChanges); });
        }

        internal SapiRecoContext CreateRecoContext()
        {
            ISpRecoContext context;
            return (SapiRecoContext)_proxy.Invoke(delegate { _proxy.Recognizer.CreateRecoContext(out context); return new SapiRecoContext(context, _proxy); });
        }

        internal SPRECOSTATE GetRecoState()
        {
            SPRECOSTATE state;
            return (SPRECOSTATE)_proxy.Invoke(delegate { _proxy.Recognizer.GetRecoState(out state); return state; });
        }

        internal void SetRecoState(SPRECOSTATE state)
        {
            _proxy.Invoke2(delegate { _proxy.Recognizer.SetRecoState(state); });
        }

        internal SPRECOGNIZERSTATUS GetStatus()
        {
            SPRECOGNIZERSTATUS status;
            return (SPRECOGNIZERSTATUS)_proxy.Invoke(delegate { _proxy.Recognizer.GetStatus(out status); return status; });
        }

        internal IntPtr GetFormat(SPSTREAMFORMATTYPE WaveFormatType)
        {
            return (IntPtr)_proxy.Invoke(delegate
          {
              Guid formatId;
              IntPtr ppCoMemWFEX;
              _proxy.Recognizer.GetFormat(WaveFormatType, out formatId, out ppCoMemWFEX); return ppCoMemWFEX;
          });
        }

        internal SAPIErrorCodes EmulateRecognition(string phrase)
        {
            object displayAttributes = " "; // Passing a null object here doesn't work because EmulateRecognition doesn't handle VT_EMPTY
            return (SAPIErrorCodes)_proxy.Invoke(delegate { return _proxy.SapiSpeechRecognizer.EmulateRecognition(phrase, ref displayAttributes, 0); });
        }

        internal SAPIErrorCodes EmulateRecognition(ISpPhrase iSpPhrase, uint dwCompareFlags)
        {
            return (SAPIErrorCodes)_proxy.Invoke(delegate { return _proxy.Recognizer2.EmulateRecognitionEx(iSpPhrase, dwCompareFlags); });
        }

        #endregion

        #region Internal Properties

        internal bool IsSapi53
        {
            get
            {
                return _isSap53;
            }
        }

        #endregion

        #region Internal Types

        internal enum RecognizerType
        {
            InProc,
            Shared
        }

        #endregion

        #region Private Methods

        private static void SetProperty(ISpRecognizer sapiRecognizer, string name, object value)
        {
            SAPIErrorCodes errorCode;

            if (value is int)
            {
                errorCode = (SAPIErrorCodes)sapiRecognizer.SetPropertyNum(name, (int)value);
            }
            else
            {
                errorCode = (SAPIErrorCodes)sapiRecognizer.SetPropertyString(name, (string)value);
            }

            if (errorCode == SAPIErrorCodes.S_FALSE)
            {
                throw new KeyNotFoundException(SR.Get(SRID.RecognizerSettingNotSupported));
            }
            else if (errorCode < SAPIErrorCodes.S_OK)
            {
                throw RecognizerBase.ExceptionFromSapiCreateRecognizerError(new COMException(SR.Get(SRID.RecognizerSettingUpdateError), (int)errorCode));
            }
        }

        private static object GetProperty(ISpRecognizer sapiRecognizer, string name, bool integer)
        {
            SAPIErrorCodes errorCode;
            object result = null;

            if (integer)
            {
                int value;
                errorCode = (SAPIErrorCodes)sapiRecognizer.GetPropertyNum(name, out value);
                result = value;
            }
            else
            {
                string value;
                errorCode = (SAPIErrorCodes)sapiRecognizer.GetPropertyString(name, out value);
                result = value;
            }

            if (errorCode == SAPIErrorCodes.S_FALSE)
            {
                throw new KeyNotFoundException(SR.Get(SRID.RecognizerSettingNotSupported));
            }
            else if (errorCode < SAPIErrorCodes.S_OK)
            {
                throw RecognizerBase.ExceptionFromSapiCreateRecognizerError(new COMException(SR.Get(SRID.RecognizerSettingUpdateError), (int)errorCode));
            }
            return result;
        }

        #endregion

        #region Private Fields

        private SapiProxy _proxy;
        private bool _disposed;
        private bool _isSap53;

        #endregion
    }
}
