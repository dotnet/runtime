// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Speech.Internal.SapiInterop
{
    internal class SapiRecoContext : IDisposable
    {
        #region Constructors

        // This constructor must be called in the context of the background proxy if any
        internal SapiRecoContext(ISpRecoContext recoContext, SapiProxy proxy)
        {
            _recoContext = recoContext;
            _proxy = proxy;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Called from the client proxy
                _proxy.Invoke2(delegate { Marshal.ReleaseComObject(_recoContext); });
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Internal Methods

        internal void SetInterest(ulong eventInterest, ulong queuedInterest)
        {
            _proxy.Invoke2(delegate { _recoContext.SetInterest(eventInterest, queuedInterest); });
        }

        internal SapiGrammar CreateGrammar(ulong id)
        {
            ISpRecoGrammar sapiGrammar;
            return (SapiGrammar)_proxy.Invoke(delegate { _recoContext.CreateGrammar(id, out sapiGrammar); return new SapiGrammar(sapiGrammar, _proxy); });
        }

        internal void SetMaxAlternates(uint count)
        {
            _proxy.Invoke2(delegate { _recoContext.SetMaxAlternates(count); });
        }

        internal void SetAudioOptions(SPAUDIOOPTIONS options, IntPtr audioFormatId, IntPtr waveFormatEx)
        {
            _proxy.Invoke2(delegate { _recoContext.SetAudioOptions(options, audioFormatId, waveFormatEx); });
        }

        internal void Bookmark(SPBOOKMARKOPTIONS options, ulong position, IntPtr lparam)
        {
            _proxy.Invoke2(delegate { _recoContext.Bookmark(options, position, lparam); });
        }

        internal void Resume()
        {
            _proxy.Invoke2(delegate { _recoContext.Resume(0); });
        }

        internal void SetContextState(SPCONTEXTSTATE state)
        {
            _proxy.Invoke2(delegate { _recoContext.SetContextState(state); });
        }

        internal EventNotify CreateEventNotify(AsyncSerializedWorker asyncWorker, bool supportsSapi53)
        {
            return (EventNotify)_proxy.Invoke(delegate { return new EventNotify(_recoContext, asyncWorker, supportsSapi53); });
        }

        internal void DisposeEventNotify(EventNotify eventNotify)
        {
            _proxy.Invoke2(eventNotify.Dispose);
        }

        internal void SetGrammarOptions(SPGRAMMAROPTIONS options)
        {
            _proxy.Invoke2(delegate { ((ISpRecoContext2)_recoContext).SetGrammarOptions(options); });
        }

        #endregion

        #region Private Fields

        private ISpRecoContext _recoContext;
        private SapiProxy _proxy;
        private bool _disposed;

        #endregion
    }
}
