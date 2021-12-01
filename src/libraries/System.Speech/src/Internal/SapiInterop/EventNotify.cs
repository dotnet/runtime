// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Speech.AudioFormat;
using System.Threading;

namespace System.Speech.Internal.SapiInterop
{
    internal class SpNotifySink : ISpNotifySink
    {
        public SpNotifySink(EventNotify eventNotify)
        {
            _eventNotifyReference = new WeakReference(eventNotify);
        }

        void ISpNotifySink.Notify()
        {
            EventNotify eventNotify = (EventNotify)_eventNotifyReference.Target;
            if (eventNotify != null)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(eventNotify.SendNotification));
            }
        }

        private WeakReference _eventNotifyReference;
    }
    /// Dispatches events from ISpEventSource to DispatchEventDelegate on a thread
    /// compatible with the application model of the thread that created this object.
    internal class EventNotify
    {
        #region Constructors

        internal EventNotify(ISpEventSource sapiEventSource, IAsyncDispatch dispatcher, bool additionalSapiFeatures)
        {
            // Remember event source
            _sapiEventSourceReference = new WeakReference(sapiEventSource);

            _dispatcher = dispatcher;
            _additionalSapiFeatures = additionalSapiFeatures;

            // Start listening to events from sapiEventSource.
            _notifySink = new SpNotifySink(this);
            sapiEventSource.SetNotifySink(_notifySink);
        }

        #endregion Constructors

        #region Internal Methods

        // Finalizer is not required since ISpEventSource and AsyncOperation both implement appropriate finalizers.
        internal void Dispose()
        {
            lock (this)
            {
                // Since we are explicitly calling Dispose(), sapiEventSource (RCW) will normally be alive.
                // If Dispose() is called from a finalizer this may not be the case so check for null.
                if (_sapiEventSourceReference != null)
                {
                    ISpEventSource sapiEventSource = (ISpEventSource)_sapiEventSourceReference.Target;
                    if (sapiEventSource != null)
                    {
                        // Stop listening to events from sapiEventSource.
                        sapiEventSource.SetNotifySink(null);
                        _notifySink = null;
                    }
                }
                _sapiEventSourceReference = null;
            }
        }

        internal void SendNotification(object ignored)
        {
            lock (this)
            {
                // Call dispatchEventDelegate for each SAPI event currently queued.
                if (_sapiEventSourceReference != null)
                {
                    ISpEventSource sapiEventSource = (ISpEventSource)_sapiEventSourceReference.Target;
                    if (sapiEventSource != null)
                    {
                        List<SpeechEvent> speechEvents = new();
                        SpeechEvent speechEvent;
                        while (null != (speechEvent = SpeechEvent.TryCreateSpeechEvent(sapiEventSource, _additionalSapiFeatures, _audioFormat)))
                        {
                            speechEvents.Add(speechEvent);
                        }
                        _dispatcher.Post(speechEvents.ToArray());
                    }
                }
            }
        }

        #endregion Methods

        #region Internal Properties

        internal SpeechAudioFormatInfo AudioFormat
        {
            set
            {
                _audioFormat = value;
            }
        }

        #endregion Methods

        #region Private Methods

        #endregion

        #region Private Fields

        private IAsyncDispatch _dispatcher;
        private WeakReference _sapiEventSourceReference;
        private bool _additionalSapiFeatures;
        private SpeechAudioFormatInfo _audioFormat;
        private ISpNotifySink _notifySink;
        #endregion Private Fields
    }
}
