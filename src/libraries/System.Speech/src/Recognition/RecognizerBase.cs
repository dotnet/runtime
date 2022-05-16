// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Internal.SapiInterop;
using System.Threading;

namespace System.Speech.Recognition
{
    internal class RecognizerBase : IRecognizerInternal, IDisposable,
ISpGrammarResourceLoader
    {
        #region Constructors

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RecognizerBase()
        {
            Dispose(false);
        }

        #endregion

        #region Internal Methods

        #region Methods to Load and Unload grammars:

        // Synchronous:
        internal void LoadGrammar(Grammar grammar)
        {
            try
            {
                ValidateGrammar(grammar, GrammarState.Unloaded);

                // Stream and SrgsDocument Grammars get reset on Unload and can't be loaded again. Url Grammars can be reloaded.
                if (!_supportsSapi53)
                {
                    CheckGrammarOptionsOnSapi51(grammar);
                }

                // Create sapi grammar
                // Make the sapi grammar and the id
                ulong grammarId;
                SapiGrammar sapiGrammar = CreateNewSapiGrammar(out grammarId);

                // Load the data into SAPI:
                try
                {
                    LoadSapiGrammar(grammar, sapiGrammar, grammar.Enabled, grammar.Weight, grammar.Priority);
                }
                catch
                {
                    // Release the SAPI object on error.
                    sapiGrammar.Dispose();

                    // Set the State to Unloaded.
                    grammar.State = GrammarState.Unloaded;
                    grammar.InternalData = null;

                    // re-throw exception
                    throw;
                }

                // Create the InternalGrammarData object:
                grammar.InternalData = new InternalGrammarData(grammarId, sapiGrammar, grammar.Enabled, grammar.Weight, grammar.Priority);

                // Add to collection:
                lock (SapiRecognizer) // Lock to prevent anyone enumerating _grammars from failing
                {
                    _grammars.Add(grammar);
                }

                grammar.Recognizer = this;
                grammar.State = GrammarState.Loaded;

                // Note on failure in LoadGrammar() the state remains at New and the Grammar is not added to the collection.
                // This is in contrast to an asynchronous load where the state is set to LoadFailed and the Grammar is added.
            }
            catch (Exception e)
            {
                _loadException = e;
                throw;
            }
        }

        // Asynchronous:
        internal void LoadGrammarAsync(Grammar grammar)
        {
            // Stream and SrgsDocument Grammars get reset on Unload and can't be loaded again. Url Grammars can be reloaded.
            if (!_supportsSapi53)
            {
                CheckGrammarOptionsOnSapi51(grammar);
            }
            ValidateGrammar(grammar, GrammarState.Unloaded);

            // Various methods like SetGrammarState get simpler if there's a SAPI grammar attached to every Grammar.
            // So create sapi grammar and attach to the Internal data before starting the load.
            ulong grammarId;
            SapiGrammar sapiGrammar = CreateNewSapiGrammar(out grammarId);

            // Make the container for the sapiGrammar and cached property values.
            grammar.InternalData = new InternalGrammarData(grammarId, sapiGrammar, grammar.Enabled, grammar.Weight, grammar.Priority);

            // Add to collection:
            lock (SapiRecognizer) // Lock to prevent anyone enumerating _grammars from failing
            {
                _grammars.Add(grammar);
            }

            grammar.Recognizer = this;
            grammar.State = GrammarState.Loading;

            // Increment the OperationLock to indicate we are loading a grammar asynchronously.
            _waitForGrammarsToLoad.StartOperation();

            // Do the actual load on a thread pool callback.
            if (!ThreadPool.QueueUserWorkItem(new WaitCallback(LoadGrammarAsyncCallback), grammar))
            {
                throw new OperationCanceledException(SR.Get(SRID.OperationAborted));
            }
        }

        // Unload grammars:
        internal void UnloadGrammar(Grammar grammar)
        {
            // Currently we have no good way of deleting grammars that are still being loaded.
            ValidateGrammar(grammar, GrammarState.Loaded, GrammarState.LoadFailed);

            // Delete SAPI grammar
            InternalGrammarData grammarData = grammar.InternalData;
            // Both in the Loaded and LoadFailed state the sapi grammar should still exist.
            if (grammarData != null)
            {
                Debug.Assert(grammarData._sapiGrammar != null);
                grammarData._sapiGrammar.Dispose();
            }

            // Remove from collection
            lock (SapiRecognizer) // Lock to prevent anyone enumerating _grammars from failing
            {
                _grammars.Remove(grammar);
            }

            // Mark grammar as dead
            grammar.State = GrammarState.Unloaded;
            grammar.InternalData = null;
        }
        internal void UnloadAllGrammars()
        {
            // Use a new collection as otherwise can't delete from current enumeration.
            List<Grammar> snapshotGrammars;
            lock (SapiRecognizer)
            {
                snapshotGrammars = new List<Grammar>(_grammars);
            }

            // If there is any grammar being loaded asynchronously, wait for the operation to finish first
            _waitForGrammarsToLoad.WaitForOperationsToFinish();

            foreach (Grammar grammar in snapshotGrammars)
            {
                UnloadGrammar(grammar);
            }

            // At the moment there's no way to delete all RecoGrammars in SAPI without individually releasing each one.
            // If there was such a mechanism it might be faster than looping through every Grammar.
        }

        #endregion

        #region IRecognizerInternal implementation

        void IRecognizerInternal.SetGrammarState(Grammar grammar, bool enabled)
        {
            Debug.Assert(grammar != null);
            Debug.Assert(grammar.Recognizer == this);

            // Note: In all states where Grammar is attached to Recognizer {Loading, Loaded, LoadFailed)
            // then the sapiGrammar will be non-null.

            InternalGrammarData grammarData = grammar.InternalData;
            Debug.Assert(grammarData != null && grammarData._sapiGrammar != null);

            // Take the lock so things like the changing of the grammar state to Loaded, or the completion of the load
            // and call to SetSapiGrammarProperties cannot be happening on the background thread.
            lock (_grammarDataLock)
            {
                // If the grammar is actually loaded then update its state in sapi.
                if (grammar.Loaded)
                {
                    grammarData._sapiGrammar.SetGrammarState(enabled ? SPGRAMMARSTATE.SPGS_ENABLED : SPGRAMMARSTATE.SPGS_DISABLED);
                }

                // Otherwise just update the local copy so it gets set correctly when Loaded.
                grammarData._grammarEnabled = enabled;
            }

            // Note - after disabling a Grammar no pending results will be fired on the Grammar because the event handler throws the events away.
        }

        void IRecognizerInternal.SetGrammarWeight(Grammar grammar, float weight)
        {
            Debug.Assert(grammar != null);
            Debug.Assert(grammar.Recognizer == this);

            if (!_supportsSapi53)
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI2, "Weight"));
            }

            InternalGrammarData grammarData = grammar.InternalData;
            Debug.Assert(grammarData != null && grammarData._sapiGrammar != null);

            lock (_grammarDataLock)
            {
                if (grammar.Loaded)
                {
                    if (grammar.IsDictation(grammar.Uri))
                    {
                        grammarData._sapiGrammar.SetDictationWeight(weight);
                    }
                    else
                    {
                        grammarData._sapiGrammar.SetRuleWeight(grammar.RuleName, 0, weight);
                    }
                }
                grammarData._grammarWeight = weight;
            }
        }

        void IRecognizerInternal.SetGrammarPriority(Grammar grammar, int priority)
        {
            Debug.Assert(grammar != null);
            Debug.Assert(grammar.Recognizer == this);

            if (!_supportsSapi53)
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI2, "Priority"));
            }

            InternalGrammarData grammarData = grammar.InternalData;
            Debug.Assert(grammarData != null && grammarData._sapiGrammar != null);

            lock (_grammarDataLock)
            {
                if (grammar.Loaded)
                {
                    if (grammar.IsDictation(grammar.Uri))
                    {
                        // This is not supported in SAPI currently.
                        // but not necessarily always.
                        throw new NotSupportedException(SR.Get(SRID.CannotSetPriorityOnDictation));
                    }
                    else
                    {
                        grammarData._sapiGrammar.SetRulePriority(grammar.RuleName, 0, priority);
                    }
                }
                grammarData._grammarPriority = priority;
            }
        }

        // This method is used to get the Grammar object back from the id returned in the sapi recognition events.
        Grammar IRecognizerInternal.GetGrammarFromId(ulong id)
        {
            lock (SapiRecognizer) // Lock to prevent enumerating _grammars from failing if list is modified on main thread
            {
                foreach (Grammar grammar in _grammars)
                {
                    InternalGrammarData grammarData = grammar.InternalData;
                    if (grammarData._grammarId == id)
                    {
                        Debug.Assert(grammar.State == GrammarState.Loaded && grammar.Recognizer == this);
                        return grammar;
                    }
                }
            }

            return null; // The grammar has already been unloaded
        }

        void IRecognizerInternal.SetDictationContext(Grammar grammar, string precedingText, string subsequentText)
        {
            if (precedingText == null) { precedingText = string.Empty; }
            if (subsequentText == null) { subsequentText = string.Empty; }

            SPTEXTSELECTIONINFO selectionInfo = new(0, 0, (uint)precedingText.Length, 0);
            string textString = precedingText + subsequentText + "\0\0";

            SapiGrammar sapiGrammar = grammar.InternalData._sapiGrammar;
            sapiGrammar.SetWordSequenceData(textString, selectionInfo);
        }

        #endregion
        internal RecognitionResult EmulateRecognize(string inputText)
        {
            Helpers.ThrowIfEmptyOrNull(inputText, nameof(inputText));

            return InternalEmulateRecognize(inputText, SpeechEmulationCompareFlags.SECFDefault, false, null);
        }
        internal void EmulateRecognizeAsync(string inputText)
        {
            Helpers.ThrowIfEmptyOrNull(inputText, nameof(inputText));

            InternalEmulateRecognizeAsync(inputText, SpeechEmulationCompareFlags.SECFDefault, false, null);
        }
        internal RecognitionResult EmulateRecognize(string inputText, CompareOptions compareOptions)
        {
            Helpers.ThrowIfEmptyOrNull(inputText, nameof(inputText));

            bool defaultCasing = compareOptions == CompareOptions.IgnoreCase || compareOptions == CompareOptions.OrdinalIgnoreCase;

            // In Sapi 5.1 the only option is case-sensitive search with extendedWordFormat checking.
            // We still let you use the default EmulateRecognize although the behavior is slightly different.
            // Disable additional flags even with SAPI 5.3 until final EmulateRecognition design completed.
            if (!_supportsSapi53 && !defaultCasing)
            {
                // Disable async grammar loading on SAPI 5.1 because of threading model issues.
                // Note that even if there are no threading issues, baseUri is not supported with SAPI 5.1.
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPICompareOption));
            }

            return InternalEmulateRecognize(inputText, ConvertCompareOptions(compareOptions), !defaultCasing, null);
        }
        internal void EmulateRecognizeAsync(string inputText, CompareOptions compareOptions)
        {
            Helpers.ThrowIfEmptyOrNull(inputText, nameof(inputText));

            bool defaultCasing = compareOptions == CompareOptions.IgnoreCase || compareOptions == CompareOptions.OrdinalIgnoreCase;

            // In Sapi 5.1 the only option is case-sensitive search with extendedWordFormat checking.
            // We still let you use the default EmulateRecognize although the behavior is slightly different.
            // Disable additional flags even with SAPI 5.3 until final EmulateRecognition design completed.
            if (!_supportsSapi53 && !defaultCasing)
            {
                // Disable async grammar loading on SAPI 5.1 because of threading model issues.
                // Note that even if there are no threading issues, baseUri is not supported with SAPI 5.1.
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPICompareOption));
            }

            InternalEmulateRecognizeAsync(inputText, ConvertCompareOptions(compareOptions), !defaultCasing, null);
        }
        internal RecognitionResult EmulateRecognize(RecognizedWordUnit[] wordUnits, CompareOptions compareOptions)
        {
            // In Sapi 5.1 the only option is case-sensitive search with extendedWordFormat checking.
            // We still let you use the default EmulateRecognize although the behavior is slightly different.
            // Disable additional flags even with SAPI 5.3 until final EmulateRecognition design completed.
            if (!_supportsSapi53)
            {
                // Disable async grammar loading on SAPI 5.1 because of threading model issues.
                // Note that even if there are no threading issues, baseUri is not supported with SAPI 5.1.
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI));
            }
            Helpers.ThrowIfNull(wordUnits, nameof(wordUnits));

            foreach (RecognizedWordUnit wordUnit in wordUnits)
            {
                if (wordUnit == null)
                {
                    throw new ArgumentException(SR.Get(SRID.ArrayOfNullIllegal), nameof(wordUnits));
                }
            }

            return InternalEmulateRecognize(null, ConvertCompareOptions(compareOptions), true, wordUnits);
        }
        internal void EmulateRecognizeAsync(RecognizedWordUnit[] wordUnits, CompareOptions compareOptions)
        {
            // In Sapi 5.1 the only option is case-sensitive search with extendedWordFormat checking.
            // We still let you use the default EmulateRecognize although the behavior is slightly different.
            // Disable additional flags even with SAPI 5.3 until final EmulateRecognition design completed.
            if (!_supportsSapi53)
            {
                // Disable async grammar loading on SAPI 5.1 because of threading model issues.
                // Note that even if there are no threading issues, baseUri is not supported with SAPI 5.1.
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI));
            }
            Helpers.ThrowIfNull(wordUnits, nameof(wordUnits));

            foreach (RecognizedWordUnit wordUnit in wordUnits)
            {
                if (wordUnit == null)
                {
                    throw new ArgumentException(SR.Get(SRID.ArrayOfNullIllegal), nameof(wordUnits));
                }
            }

            InternalEmulateRecognizeAsync(null, ConvertCompareOptions(compareOptions), true, wordUnits);
        }

        // Methods to pause the recognizer to do atomic updates:
        internal void RequestRecognizerUpdate()
        {
            RequestRecognizerUpdate(null);
        }
        internal void RequestRecognizerUpdate(object userToken)
        {
            uint bookmarkId = AddBookmarkItem(userToken);

            // This fires the bookmark as soon as possible so we set the time as zero and don't set the SPBO_AHEAD flag.
            SapiContext.Bookmark(SPBOOKMARKOPTIONS.SPBO_PAUSE, 0, new IntPtr(bookmarkId));
        }
        internal void RequestRecognizerUpdate(object userToken, TimeSpan audioPositionAheadToRaiseUpdate)
        {
            if (audioPositionAheadToRaiseUpdate < TimeSpan.Zero)
            {
                throw new NotSupportedException(SR.Get(SRID.NegativeTimesNotSupported));
            }
            if (!_supportsSapi53)
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI));
            }

            uint bookmarkId = AddBookmarkItem(userToken);

            // This always fires the bookmark ahead of the current position.
            // So calling this with zero will wait until the recognizer catches up with the current audio position before firing.
            SapiContext.Bookmark(SPBOOKMARKOPTIONS.SPBO_PAUSE | SPBOOKMARKOPTIONS.SPBO_AHEAD | SPBOOKMARKOPTIONS.SPBO_TIME_UNITS,
                (ulong)audioPositionAheadToRaiseUpdate.Ticks, new IntPtr(bookmarkId));
        }

        internal void Initialize(SapiRecognizer recognizer, bool inproc)
        {
            // Create RecoContext:
            _sapiRecognizer = recognizer;
            _inproc = inproc;

            _recoThunk = new RecognizerBaseThunk(this);

            try
            {
                _sapiContext = _sapiRecognizer.CreateRecoContext();
            }
            catch (COMException e)
            {
                // SAPI 5.1 can throw this error when no recognizer
                if (!_supportsSapi53 && (SAPIErrorCodes)e.ErrorCode == SAPIErrorCodes.SPERR_NOT_FOUND)
                {
                    throw new PlatformNotSupportedException(SR.Get(SRID.RecognitionNotSupported));
                }
                throw ExceptionFromSapiCreateRecognizerError(e);
            }

            // See if SAPI 5.3 features are supported.
            _supportsSapi53 = recognizer.IsSapi53;

            if (_supportsSapi53)
            {
                _sapiContext.SetGrammarOptions(SPGRAMMAROPTIONS.SPGO_ALL);
            }

            try
            {
                ISpPhoneticAlphabetSelection alphabetSelection = _sapiContext as ISpPhoneticAlphabetSelection;
                if (alphabetSelection != null)
                {
                    alphabetSelection.SetAlphabetToUPS(true);
                }
                else
                {
                    Trace.TraceInformation("SAPI does not implement phonetic alphabet selection.");
                }
            }
            catch (COMException)
            {
                Trace.TraceError("Cannot force SAPI to set the alphabet to UPS");
            }

            _sapiContext.SetAudioOptions(SPAUDIOOPTIONS.SPAO_RETAIN_AUDIO, IntPtr.Zero, IntPtr.Zero);

            // Enable alternates with default max.
            MaxAlternates = 10;

            ResetBookmarkTable();

            // Set basic SR event interests that are routed to the end user.
            // Hypothesis and AudioLevelChange events are raised frequently and are less commonly used.
            // So their interests will be registered individually.
            _eventInterest = (1ul << (int)SPEVENTENUM.SPEI_RESERVED1) |
                (1ul << (int)SPEVENTENUM.SPEI_RESERVED2) |
                (1ul << (int)SPEVENTENUM.SPEI_START_SR_STREAM) |
                (1ul << (int)SPEVENTENUM.SPEI_PHRASE_START) |
                (1ul << (int)SPEVENTENUM.SPEI_FALSE_RECOGNITION) |
                (1ul << (int)SPEVENTENUM.SPEI_RECOGNITION) |
                (1ul << (int)SPEVENTENUM.SPEI_RECO_OTHER_CONTEXT) |
                (1ul << (int)SPEVENTENUM.SPEI_END_SR_STREAM) |
                (1ul << (int)SPEVENTENUM.SPEI_SR_BOOKMARK);
            _sapiContext.SetInterest(_eventInterest, _eventInterest);

            _asyncWorker = new AsyncSerializedWorker(new WaitCallback(DispatchEvents), null);

            _asyncWorkerUI = new AsyncSerializedWorker(null, SynchronizationContext.Current);
            _asyncWorkerUI.WorkItemPending += new WaitCallback(SignalHandlerThread);

            _eventNotify = _sapiContext.CreateEventNotify(_asyncWorker, _supportsSapi53);

            _grammars = new List<Grammar>();
            _readOnlyGrammars = new ReadOnlyCollection<Grammar>(_grammars);
            UpdateAudioFormat(null);
            InitialSilenceTimeout = TimeSpan.FromSeconds(30);
        }

        internal void RecognizeAsync(RecognizeMode mode)
        {
            lock (SapiRecognizer) // Lock to protect _isRecognizing and _haveInputSource
            {
                if (_isRecognizing)
                {
                    throw new InvalidOperationException(SR.Get(SRID.RecognizerAlreadyRecognizing));
                }
                if (!_haveInputSource)
                {
                    throw new InvalidOperationException(SR.Get(SRID.RecognizerNoInputSource));
                }

                _isRecognizing = true;

                // The call to RecognizeAsync may happen before the event for the start stream arrives so remove the assert.
                //Debug.Assert (_detectingInitialSilenceTimeout == false);
                Debug.Assert(_detectingBabbleTimeout == false);
                Debug.Assert(_initialSilenceTimeoutReached == false);
                Debug.Assert(_babbleTimeoutReached == false);
                Debug.Assert(_isRecognizeCancelled == false);
                Debug.Assert(_lastResult == null);
                Debug.Assert(_lastException == null);
            } // Not recognizing so no events firing - can unlock now

            _recognizeMode = mode; // This is always Multiple for SpeechRecognizer. If Automatic stop after each recognition.

            if (_supportsSapi53)
            {
                // On another thread - wait for grammar loading to complete and start the recognizer.
                if (!ThreadPool.QueueUserWorkItem(new WaitCallback(RecognizeAsyncWaitForGrammarsToLoad)))
                {
                    throw new OperationCanceledException(SR.Get(SRID.OperationAborted));
                }
            }
            else
            {
                // Don't support async grammar loading and can't call this on another thread because of threading model issues.
                // So just start and throw if there's a problem starting the audio.
                try
                {
                    SapiRecognizer.SetRecoState(SPRECOSTATE.SPRST_ACTIVE_ALWAYS);
                    Debug.WriteLine("Grammar loads completed, recognition started.");
                }
                catch (COMException comException)
                {
                    Debug.WriteLine("Problem starting recognition - sapi exception.");
                    throw ExceptionFromSapiStreamError((SAPIErrorCodes)comException.ErrorCode);
                }
                catch
                {
                    Debug.WriteLine("Problem starting recognition - unknown exception.");
                    throw;
                }
            }
        }

        internal RecognitionResult Recognize(TimeSpan initialSilenceTimeout)
        {
            //let InitialSilenceTimeout property below do validation on the TimeSpan parameter

            RecognitionResult result = null;
            bool completed = false;
            bool hasPendingTask = false;
            bool canceled = false;

            EventHandler<RecognizeCompletedEventArgs> eventHandler = delegate (object sender, RecognizeCompletedEventArgs eventArgs)
            {
                result = eventArgs.Result;
                completed = true;
            };

            TimeSpan oldInitialSilenceTimeout = _initialSilenceTimeout;
            this.InitialSilenceTimeout = initialSilenceTimeout;

            RecognizeCompletedSync += eventHandler;

            // InitialSilenceTimeout bookmark should keep this function from waiting forever, but also have a timeout
            // here in case something's wrong with the audio and the bookmark never gets hit.
            TimeSpan eventTimeout = TimeSpan.FromTicks(Math.Max(initialSilenceTimeout.Ticks, _defaultTimeout.Ticks));

            try
            {
                _asyncWorkerUI.AsyncMode = false;
                RecognizeAsync(RecognizeMode.Single);
                while (!completed && !_disposed)
                {
                    if (!canceled)
                    {
                        hasPendingTask = _handlerWaitHandle.WaitOne(eventTimeout, false);
                        if (!hasPendingTask)
                        {
                            EndRecognitionWithTimeout();
                            canceled = true;
                        }
                    }
                    else
                    {
                        // We have canceled the recognition, so now we only wait to process remaining events
                        //  until SPEI_END_SR_STREAM event arrives.
                        hasPendingTask = _handlerWaitHandle.WaitOne(eventTimeout, false);
                    }

                    if (hasPendingTask)
                    {
                        _asyncWorkerUI.ConsumeQueue();
                    }
                }
            }
            finally
            {
                RecognizeCompletedSync -= eventHandler;
                _initialSilenceTimeout = oldInitialSilenceTimeout;
                _asyncWorkerUI.AsyncMode = true;
            }

            return result;
        }

        internal void RecognizeAsyncCancel()
        {
            bool doCancel = false;

            lock (SapiRecognizer) // Lock to protect _isRecognizing and _isRecognizeCancelled
            {
                if (_isRecognizing)
                {
                    if (!_isEmulateRecognition)
                    {
                        doCancel = true;
                        _isRecognizeCancelled = true; // Set this flag so the RecognizeCompleted event shows the operation was cancelled.
                    }
                    else
                    {
                        // Reset all the recognition flags if an emulate recognition is in progress
                        _isRecognizing = _isEmulateRecognition = false;
                    }
                }
            }

            if (doCancel)
            {
                // Don't hold the lock while we do this.
                try
                {
                    SapiRecognizer.SetRecoState(SPRECOSTATE.SPRST_INACTIVE_WITH_PURGE);
                }
                catch (COMException e)
                {
                    throw ExceptionFromSapiCreateRecognizerError(e);
                }
            }
        }

        internal void RecognizeAsyncStop()
        {
            bool doCancel = false;

            lock (SapiRecognizer) // Lock to protect _isRecognizing and _isRecognizeCancelled
            {
                if (_isRecognizing)
                {
                    doCancel = true;
                    _isRecognizeCancelled = true; // Still set the flag as this is a kind of cancel.
                }
            }

            if (doCancel)
            {
                // Don't hold the lock while we do this.
                try
                {
                    SapiRecognizer.SetRecoState(SPRECOSTATE.SPRST_INACTIVE);
                }
                catch (COMException e)
                {
                    throw ExceptionFromSapiCreateRecognizerError(e);
                }
            }
        }

        // Controls whether the recognizer is paused after each recognition.
        // This is always true for the SpeechRecognitionEngine and is customizable {default false} for the SpeechRecognizer.
        internal bool PauseRecognizerOnRecognition
        {
            // No need to lock anything as this value is non-touched in the event handling code and we are only enumerating _grammars on main thread.
            get { return _pauseRecognizerOnRecognition; }
            set
            {
                if (value != _pauseRecognizerOnRecognition)
                {
                    _pauseRecognizerOnRecognition = value;

                    lock (SapiRecognizer)
                    {
                        foreach (Grammar grammar in _grammars)
                        {
                            SapiGrammar sapiGrammar = grammar.InternalData._sapiGrammar;
                            ActivateRule(sapiGrammar, grammar.Uri, grammar.RuleName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the current input for the recognizer to a file
        /// </summary>
        internal void SetInput(string path)
        {
            Stream inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            SetInput(inputStream, null);

            // Keep track of the local stream
            _inputStream = inputStream;
        }

        /// <summary>
        /// Set the current input for the recognizer to a file
        /// </summary>
        internal void SetInput(Stream stream, SpeechAudioFormatInfo audioFormat)
        {
            lock (SapiRecognizer) // Lock to protect _isRecognizing and _haveInputSource
            {
                if (_isRecognizing)
                {
                    throw new InvalidOperationException(SR.Get(SRID.RecognizerAlreadyRecognizing));
                }

                try
                {
                    // Detach the input stream from the recognizer
                    if (stream == null)
                    {
                        SapiRecognizer.SetInput(null, false);
                        _haveInputSource = false;
                    }
                    else
                    {
                        SapiRecognizer.SetInput(new SpAudioStreamWrapper(stream, audioFormat), false);
                        _haveInputSource = true;
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionFromSapiCreateRecognizerError(e);
                }

                CloseCachedInputStream();
                UpdateAudioFormat(audioFormat);
            }
        }

        /// <summary>
        /// Reset the recognizer input stream to the default audio device
        /// </summary>
        internal void SetInputToDefaultAudioDevice()
        {
            lock (SapiRecognizer) // Lock to protect _isRecognizing and _haveInputSource
            {
                if (_isRecognizing)
                {
                    throw new InvalidOperationException(SR.Get(SRID.RecognizerAlreadyRecognizing));
                }

                ISpObjectTokenCategory category = (ISpObjectTokenCategory)new SpObjectTokenCategory();
                try
                {
                    category.SetId(SAPICategories.AudioIn, false);

                    string tokenId;
                    category.GetDefaultTokenId(out tokenId);

                    ISpObjectToken token = (ISpObjectToken)new SpObjectToken();
                    try
                    {
                        token.SetId(null, tokenId, false);
                        SapiRecognizer.SetInput(token, true);
                    }
                    catch (COMException e)
                    {
                        throw ExceptionFromSapiCreateRecognizerError(e);
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(token);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionFromSapiCreateRecognizerError(e);
                }
                finally
                {
                    Marshal.ReleaseComObject(category);
                }

                UpdateAudioFormat(null);
                _haveInputSource = true; // On success
            }
        }

        internal int QueryRecognizerSettingAsInt(string settingName)
        {
            Helpers.ThrowIfEmptyOrNull(settingName, nameof(settingName));

            // See if property is an int.
            return SapiRecognizer.GetPropertyNum(settingName);
        }

        internal object QueryRecognizerSetting(string settingName)
        {
            Helpers.ThrowIfEmptyOrNull(settingName, nameof(settingName));

            // See if property is an int.
            try
            {
                return SapiRecognizer.GetPropertyNum(settingName);
            }
            catch (Exception e)
            {
                if (e is COMException || e is InvalidOperationException || e is KeyNotFoundException)
                {
                    return SapiRecognizer.GetPropertyString(settingName);
                }
                throw;
            }
        }

        internal void UpdateRecognizerSetting(string settingName, string updatedValue)
        {
            Helpers.ThrowIfEmptyOrNull(settingName, nameof(settingName));

            SapiRecognizer.SetPropertyString(settingName, updatedValue);
        }

        internal void UpdateRecognizerSetting(string settingName, int updatedValue)
        {
            Helpers.ThrowIfEmptyOrNull(settingName, nameof(settingName));

            SapiRecognizer.SetPropertyNum(settingName, updatedValue);
        }

        internal static Exception ExceptionFromSapiCreateRecognizerError(COMException e)
        {
            return ExceptionFromSapiCreateRecognizerError((SAPIErrorCodes)e.ErrorCode);
        }

        internal static Exception ExceptionFromSapiCreateRecognizerError(SAPIErrorCodes errorCode)
        {
            SRID srid = SapiConstants.SapiErrorCode2SRID(errorCode);
            switch (errorCode)
            {
                case SAPIErrorCodes.CLASS_E_CLASSNOTAVAILABLE:
                case SAPIErrorCodes.REGDB_E_CLASSNOTREG:
                    {
                        OperatingSystem OS = Environment.OSVersion;
                        if (IntPtr.Size == 8 && // 64-bit system
                            OS.Platform == PlatformID.Win32NT && // On Windows NT or above
                            OS.Version.Major == 5) // Windows 2000 / XP / Server 2003
                        {
                            return new NotSupportedException(SR.Get(SRID.RecognitionNotSupportedOn64bit));
                        }
                        else
                        {
                            return new PlatformNotSupportedException(SR.Get(SRID.RecognitionNotSupported));
                        }
                    }

                case SAPIErrorCodes.SPERR_SHARED_ENGINE_DISABLED:
                case SAPIErrorCodes.SPERR_RECOGNIZER_NOT_FOUND:
                    return new PlatformNotSupportedException(SR.Get(srid));

                default:
                    Exception exReturn = null;
                    if (srid >= 0)
                    {
                        exReturn = new InvalidOperationException(SR.Get(srid));
                    }
                    else
                    {
                        try
                        {
                            Marshal.ThrowExceptionForHR((int)errorCode);
                        }
                        catch (Exception ex)
                        {
                            exReturn = ex;
                        }
                    }
                    return exReturn;
            }
        }

        #endregion

        #region Internal Properties

        // Note on locking implementation:
        //
        // In general operations are not locked on the RecognizerBase - there's no single lock that makes everything thread safe.
        // This is the normal .NET design pattern.
        //
        // However, because there is processing of sapi events, going on different threads that the app does not control,
        // we need to protect certain members.
        //
        // This is generally done with "lock (SapiRecognizer)" - the choice of SapiRecognizer is arbitrary  - any object could have been used.
        // Anything that's touched both by sapi event code and by public methods need this lock.
        // {For sanity this includes bool like _isRecognizing even though setting these is atomic}.
        // Similarly when enumerating the Grammars collection we need to ensure no other thread can be adding or removing items.
        //
        // Some other well encapsulated fields also lock themselves e.g. the bookmark table.
        //
        // In addition, the EventNotify class holds a lock to prevent events being fired more that one at a time.
        // It is required that Dispose also takes this lock.

        internal TimeSpan InitialSilenceTimeout
        {
            // lock to protect _initialSilenceTimeout and _isRecognizing
            get { lock (SapiRecognizer) { return _initialSilenceTimeout; } }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.NegativeTimesNotSupported));
                }

                lock (SapiRecognizer)
                {
                    if (_isRecognizing)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.RecognizerAlreadyRecognizing));
                    }
                    _initialSilenceTimeout = value;
                }
            }
        }

        internal TimeSpan BabbleTimeout
        {
            // lock to protect _babbleTimeout and _isRecognizing
            get { lock (SapiRecognizer) { return _babbleTimeout; } }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.NegativeTimesNotSupported));
                }

                lock (SapiRecognizer)
                {
                    if (_isRecognizing)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.RecognizerAlreadyRecognizing));
                    }
                    _babbleTimeout = value;
                }
            }
        }

        internal RecognizerState State
        {
            get
            {
                try
                {
                    SPRECOSTATE sapiState;
                    sapiState = SapiRecognizer.GetRecoState(); // This does not wait for engine sync point so should be fast.
                    if (sapiState == SPRECOSTATE.SPRST_ACTIVE || sapiState == SPRECOSTATE.SPRST_ACTIVE_ALWAYS)
                    {
                        return RecognizerState.Listening;
                    }
                    else
                    {
                        return RecognizerState.Stopped;
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionFromSapiCreateRecognizerError(e);
                }
            }
        }

        internal bool Enabled
        {
            get
            {
                lock (SapiRecognizer) // Lock to protect _enabled
                {
                    return _enabled;
                }
            }
            set
            {
                lock (SapiRecognizer) // Lock to protect _enabled
                {
                    if (value != _enabled)
                    {
                        try
                        {
                            SapiContext.SetContextState(value ? SPCONTEXTSTATE.SPCS_ENABLED : SPCONTEXTSTATE.SPCS_DISABLED);
                            _enabled = value;
                        }
                        catch (COMException e)
                        {
                            throw ExceptionFromSapiCreateRecognizerError(e);
                        }
                    }
                }
            }
        }

        // Gives access to the collection of grammars that are currently active. Read-only.
        internal ReadOnlyCollection<Grammar> Grammars
        {
            get { return _readOnlyGrammars; }
        }

        // Gives access to the set of attributes exposed by this recognizer.
        internal RecognizerInfo RecognizerInfo
        {
            get
            {
                if (_recognizerInfo == null)
                {
                    try
                    {
                        _recognizerInfo = SapiRecognizer.GetRecognizerInfo();
                    }
                    catch (COMException e)
                    {
                        throw ExceptionFromSapiCreateRecognizerError(e);
                    }
                }

                return _recognizerInfo;
            }
        }

        // Data on the audio stream the recognizer is processing
        internal AudioState AudioState
        {
            get
            {
                if (!_haveInputSource)
                {
                    // If we don't have an audio source return an empty status.
                    return AudioState.Stopped;
                }
                return _audioState;
            }
            set
            {
                _audioState = value;
            }
        }

        internal int AudioLevel
        {
            get
            {
                // If we don't have an audio source return 0
                int level = 0;
                if (_haveInputSource)
                {
                    SPRECOGNIZERSTATUS recoStatus;

                    try
                    {
                        // These calls do not wait for engine sync point so should be fast.
                        recoStatus = SapiRecognizer.GetStatus();

                        lock (SapiRecognizer) // Lock to protect _audioStatus.
                        {
                            if (_supportsSapi53)
                            {
                                level = (int)recoStatus.AudioStatus.dwAudioLevel;
                            }
                            else
                            {
                                level = 0; // This is not implemented in SAPI 5.1 so will always be zero.
                            }
                        }
                    }
                    catch (COMException e)
                    {
                        throw ExceptionFromSapiCreateRecognizerError(e);
                    }
                }

                return level;
            }
        }

        internal TimeSpan AudioPosition
        {
            get
            {
                if (!_haveInputSource)
                {
                    // If we don't have an audio source return an empty status.
                    return TimeSpan.Zero;
                }

                SPRECOGNIZERSTATUS recoStatus;

                try
                {
                    // These calls do not wait for engine sync point so should be fast.
                    recoStatus = SapiRecognizer.GetStatus();

                    lock (SapiRecognizer) // Lock to protect _audioStatus.
                    {
                        SpeechAudioFormatInfo audioFormat = AudioFormat;
                        return audioFormat.AverageBytesPerSecond > 0 ? new TimeSpan((long)((recoStatus.AudioStatus.CurDevicePos * TimeSpan.TicksPerSecond) / (ulong)audioFormat.AverageBytesPerSecond)) : TimeSpan.Zero;
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionFromSapiCreateRecognizerError(e);
                }
            }
        }

        internal TimeSpan RecognizerAudioPosition
        {
            get
            {
                if (!_haveInputSource)
                {
                    // If we don't have an audio source return an empty status.
                    return TimeSpan.Zero;
                }

                SPRECOGNIZERSTATUS recoStatus;

                try
                {
                    // These calls do not wait for engine sync point so should be fast.
                    recoStatus = SapiRecognizer.GetStatus();

                    lock (SapiRecognizer) // Lock to protect _audioStatus.
                    {
                        // RecognizerPosition and AudioPosition get reset to zero at the start of each stream so can be used directly.
                        return new TimeSpan((long)recoStatus.ullRecognitionStreamTime);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionFromSapiCreateRecognizerError(e);
                }
            }
        }
        internal SpeechAudioFormatInfo AudioFormat
        {
            get
            {
                lock (SapiRecognizer) // Lock to protect _audioFormat and _haveInputSource
                {
                    if (!_haveInputSource)
                    {
                        // If we don't have an audio source trying to return data about the audio doesn't make sense.
                        return null;
                    }

                    if (_audioFormat == null)
                    {
                        _audioFormat = GetSapiAudioFormat();
                    }
                }
                return _audioFormat;
            }
        }
        internal int MaxAlternates
        {
            get { return _maxAlternates; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.MaxAlternatesInvalid));
                }
                if (value != _maxAlternates)
                {
                    SapiContext.SetMaxAlternates((uint)value);
                    _maxAlternates = value; // On success
                }
            }
        }

        #endregion

        #region Internal Events

        // Internal event used to hook up the SpeechRecognitionEngine RecognizeCompleted event.
        internal event EventHandler<RecognizeCompletedEventArgs> RecognizeCompleted;

        // Fired when the RecognizeAsync process completes.
        internal event EventHandler<EmulateRecognizeCompletedEventArgs> EmulateRecognizeCompleted;

        // Internal event used to hook up the SpeechRecognizer StateChanged event.
        internal event EventHandler<StateChangedEventArgs> StateChanged;
        internal event EventHandler<LoadGrammarCompletedEventArgs> LoadGrammarCompleted;

        // The event fired when speech is detected. Used for barge-in.
        internal event EventHandler<SpeechDetectedEventArgs> SpeechDetected;

        // The event fired on a recognition.
        internal event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        // The event fired on a no recognition
        internal event EventHandler<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejected;

#pragma warning disable 6504
        // Occurs when a spoken phrase is partially recognized.
        internal event EventHandler<SpeechHypothesizedEventArgs> SpeechHypothesized
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                if (_speechHypothesizedDelegate == null)
                {
                    AddEventInterest(1ul << (int)SPEVENTENUM.SPEI_HYPOTHESIS);
                }
                _speechHypothesizedDelegate += value;
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                _speechHypothesizedDelegate -= value;
                if (_speechHypothesizedDelegate == null)
                {
                    RemoveEventInterest(1ul << (int)SPEVENTENUM.SPEI_HYPOTHESIS);
                }
            }
        }
        internal event EventHandler<AudioSignalProblemOccurredEventArgs> AudioSignalProblemOccurred
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                if (_audioSignalProblemOccurredDelegate == null)
                {
                    AddEventInterest(1ul << (int)SPEVENTENUM.SPEI_INTERFERENCE);
                }
                _audioSignalProblemOccurredDelegate += value;
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                _audioSignalProblemOccurredDelegate -= value;
                if (_audioSignalProblemOccurredDelegate == null)
                {
                    RemoveEventInterest(1ul << (int)SPEVENTENUM.SPEI_INTERFERENCE);
                }
            }
        }
        internal event EventHandler<AudioLevelUpdatedEventArgs> AudioLevelUpdated
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                if (_audioLevelUpdatedDelegate == null)
                {
                    AddEventInterest(1ul << (int)SPEVENTENUM.SPEI_SR_AUDIO_LEVEL);
                }
                _audioLevelUpdatedDelegate += value;
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                _audioLevelUpdatedDelegate -= value;
                if (_audioLevelUpdatedDelegate == null)
                {
                    RemoveEventInterest(1ul << (int)SPEVENTENUM.SPEI_SR_AUDIO_LEVEL);
                }
            }
        }
        internal event EventHandler<AudioStateChangedEventArgs> AudioStateChanged
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                _audioStateChangedDelegate += value;
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                _audioStateChangedDelegate -= value;
            }
        }

#pragma warning restore 6504
        internal event EventHandler<RecognizerUpdateReachedEventArgs> RecognizerUpdateReached;

        #endregion

        #region Protected Methods
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Lock to wait for event dispatching to finish
                    lock (_thisObjectLock)
                    {
                        // Make sure no pending posts are sent, no events are dispatched as we are disposing

                        if (_asyncWorkerUI != null)
                        {
                            _asyncWorkerUI.Enabled = false;
                            _asyncWorkerUI.Purge();
                            _asyncWorker.Enabled = false;
                            _asyncWorker.Purge();
                        }

                        // Dispose unmanaged resources in event notification and detach from ISpEventSource.
                        // Release grammar resources.
                        if (_sapiContext != null)
                        {
                            _sapiContext.DisposeEventNotify(_eventNotify);
                            _handlerWaitHandle.Close();
                            UnloadAllGrammars();
                            _waitForGrammarsToLoad.Dispose();
                        }
                        CloseCachedInputStream();

                        // Release SAPI recognizer/recoContext interfaces.
                        // We do not need to release additional references copy onto the same RCW.
                        if (_sapiContext != null)
                        {
                            _sapiContext.Dispose();
                            _sapiContext = null;
                        }
                        if (_sapiRecognizer != null)
                        {
                            _sapiRecognizer.Dispose();
                            _sapiRecognizer = null;
                        }

                        if (_recognizerInfo != null)
                        {
                            _recognizerInfo.Dispose();
                            _recognizerInfo = null;
                        }

                        _disposed = true;
                    }
                }
            }
        }

        #endregion

        #region Private Properties

        // Properties to get access to the underlying SAPI objects and to throw if disposed.

        private SapiRecoContext SapiContext
        {
            // Also this method is not public.
#pragma warning disable 6503
            get { if (_disposed) { throw new ObjectDisposedException("RecognizerBase"); } return _sapiContext; }
#pragma warning restore 6503
        }

        private SapiRecognizer SapiRecognizer
        {
#pragma warning disable 6503
            get { if (_disposed) { throw new ObjectDisposedException("RecognizerBase"); } return _sapiRecognizer; }
#pragma warning restore 6503
        }

        #endregion

        #region Private Methods

        // Method called from LoadGrammar and LoadGrammarAsync to load the data from a Grammar into sapiGrammar.
        // Grammar is unchanged by this method.
        private void LoadSapiGrammar(Grammar grammar, SapiGrammar sapiGrammar, bool enabled, float weight, int priority)
        {
            Uri baseUri = grammar.BaseUri;

            if (_supportsSapi53 && baseUri == null && grammar.Uri != null)
            {
                // If the base Uri has not been set any other way, then set the base Uri for this file
                string uri = grammar.Uri.OriginalString;
                int posSlash = uri.LastIndexOfAny(new char[] { '\\', '/' });
                if (posSlash >= 0)
                {
                    baseUri = new Uri(uri.Substring(0, posSlash + 1), UriKind.RelativeOrAbsolute);
                }
            }

            // For dictation grammar, pass the Uri to SAPI.
            // For anything else, load it locally to figure out if it is a
            // strongly typed grammar.
            if (grammar.IsDictation(grammar.Uri))
            {
                // If uri load
                LoadSapiDictationGrammar(sapiGrammar, grammar.Uri, grammar.RuleName, enabled, weight, priority);
                return;
            }
            LoadSapiGrammarFromCfg(sapiGrammar, grammar, baseUri, enabled, weight, priority);
        }

        // Actually load the uri into the sapiGrammar. This does not touch the Grammar object or InternalGrammarData.
        // This must be called on a new SapiGrammar that does not already have a grammar loaded {for SetSapiGrammarProperties}.
        private void LoadSapiDictationGrammar(SapiGrammar sapiGrammar, Uri uri, string ruleName, bool enabled, float weight, int priority)
        {
            try
            {
                if (Grammar.IsDictationGrammar(uri))
                {
                    // Note: checking whether the grammar is a dictation grammar is somewhat messy.
                    // This is done because SAPI has different methods to load and activate dictation as it does CFGs.
                    // Other options here include:
                    //  - Modify SAPI so LoadCmdFromFile works with dictation Uris.
                    //  - Modify the engine and use a regular grammar with a special ruleref to dictation.
                    //  - Call back to the Grammar and let it manage the loading activation.
                    string topicName = string.IsNullOrEmpty(uri.Fragment) ? null : uri.Fragment.Substring(1);
                    sapiGrammar.LoadDictation(topicName, SPLOADOPTIONS.SPLO_STATIC);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
            }
            catch (COMException e)
            {
                switch ((SAPIErrorCodes)e.ErrorCode)
                {
                    case SAPIErrorCodes.SPERR_NOT_FOUND:
                        {
                            throw new ArgumentException(SR.Get(SRID.DictationTopicNotFound, uri), e);
                        }

                    default:
                        {
                            ThrowIfSapiErrorCode((SAPIErrorCodes)e.ErrorCode);
                            throw;
                        }
                }
            }

            SetSapiGrammarProperties(sapiGrammar, uri, ruleName, enabled, weight, priority);
        }

        #region Resource loader implementation

        /// <summary>
        /// Called to load a grammar and all of its dependent rule refs.
        ///
        /// Returns the CFG data for a given file and builds a tree of rule ref dependencies.
        /// </summary>
        int ISpGrammarResourceLoader.LoadResource(string bstrResourceUri, bool fAlwaysReload, out IStream pStream, ref string pbstrMIMEType, ref short pfModified, ref string pbstrRedirectUrl)
        {
            try
            {
                // Look for the OnInitParameters
                int posGreaterThan = bstrResourceUri.IndexOf('>');
                string onInitParameters = null;
                if (posGreaterThan > 0)
                {
                    onInitParameters = bstrResourceUri.Substring(posGreaterThan + 1);
                    bstrResourceUri = bstrResourceUri.Substring(0, posGreaterThan);
                }

                // Hack to get the parent and children grammar.
                string ruleName = pbstrMIMEType;

                // The parent is the first
                string[] ids = pbstrRedirectUrl.Split(new char[] { ' ' }, StringSplitOptions.None);
                System.Diagnostics.Debug.Assert(ids.Length == 2);

                uint parentGrammarId = uint.Parse(ids[0], CultureInfo.InvariantCulture);
                uint grammarId = uint.Parse(ids[1], CultureInfo.InvariantCulture);

                // Create the grammar for that resources.
                Uri redirectedUri;
                Grammar grammar = Grammar.Create(bstrResourceUri, ruleName, onInitParameters, out redirectedUri);

                // If http:// then set the redirect Uri
                if (redirectedUri != null)
                {
                    pbstrRedirectUrl = redirectedUri.ToString();
                }

                // Could fail for SRGS
                if (grammar == null)
                {
                    throw new FormatException(SR.Get(SRID.SapiErrorRuleNotFound2, ruleName, bstrResourceUri));
                }

                // Save the SAPI grammar id for that grammar
                grammar.SapiGrammarId = grammarId;

                // Find the grammar this ruleref belongs to and add it to the appropriate grammar
                Grammar parent = _topLevel.Find(parentGrammarId);
                if (parent == null)
                {
                    _topLevel.AddRuleRef(grammar, grammarId);
                }
                else
                {
                    parent.AddRuleRef(grammar, grammarId);
                }

                // Must return and IStream to enable SAPI to retrieve the data
                MemoryStream stream = new(grammar.CfgData);
                SpStreamWrapper spStream = new(stream);
                pStream = spStream;
                pfModified = 0;

                return 0;
            }
            catch (Exception e)
            {
                // Something failed.
                // Save the exception and return an error to SAPI.
                pStream = null;
                _loadException = e;
                return (int)SAPIErrorCodes.SPERR_INVALID_IMPORT;
            }
        }

        /// <summary>
        /// Unused
        /// </summary>
        string ISpGrammarResourceLoader.GetLocalCopy(Uri resourcePath, out string mimeType, out Uri redirectUrl)
        {
            redirectUrl = null;
            mimeType = null;
            return null;
        }

        /// <summary>
        /// Unused
        /// </summary>
        void ISpGrammarResourceLoader.ReleaseLocalCopy(string path)
        {
        }

        #endregion

        // Actually load the stream into the sapiGrammar. This does not touch the Grammar object or InternalGrammarData.
        // This must be called on a new SapiGrammar that does not already have a grammar loaded {for SetSapiGrammarProperties}.
        private void LoadSapiGrammarFromCfg(SapiGrammar sapiGrammar, Grammar grammar, Uri baseUri, bool enabled, float weight, int priority)
        {
            byte[] data = grammar.CfgData;

            // Pin the array:
            GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr dataPtr = gcHandle.AddrOfPinnedObject();

            // Load the data into SAPI:
            try
            {
                if (_supportsSapi53)
                {
                    _loadException = null;
                    _topLevel = grammar;

                    if (_inproc)
                    {
                        // Use the resource loader for Sapi 5.3 and above
                        // The rulerefs will be resolved locally.

                        sapiGrammar.SetGrammarLoader(_recoThunk);
                    }
                    sapiGrammar.LoadCmdFromMemory2(dataPtr, SPLOADOPTIONS.SPLO_STATIC, null, baseUri == null ? null : baseUri.ToString());
                }
                else
                {
                    sapiGrammar.LoadCmdFromMemory(dataPtr, SPLOADOPTIONS.SPLO_STATIC);
                }
            }
            catch (COMException e)
            {
                switch ((SAPIErrorCodes)e.ErrorCode)
                {
                    case SAPIErrorCodes.SPERR_UNSUPPORTED_FORMAT:
                        {
                            throw new FormatException(SR.Get(SRID.RecognizerInvalidBinaryGrammar), e);
                        }
                    case SAPIErrorCodes.SPERR_INVALID_IMPORT:
                        {
                            throw new FormatException(SR.Get(SRID.SapiErrorInvalidImport), e);
                        }
                    case SAPIErrorCodes.SPERR_TOO_MANY_GRAMMARS:
                        {
                            throw new NotSupportedException(SR.Get(SRID.SapiErrorTooManyGrammars), e);
                        }
                    case SAPIErrorCodes.SPERR_NOT_FOUND:
                        {
                            throw new FileNotFoundException(SR.Get(SRID.ReferencedGrammarNotFound), e);
                        }

                    case ((SAPIErrorCodes)(-1)):
                        if (_loadException != null)
                        {
                            ExceptionDispatchInfo.Throw(_loadException);
                        }
                        ThrowIfSapiErrorCode((SAPIErrorCodes)e.ErrorCode);
                        break;

                    default:
                        ThrowIfSapiErrorCode((SAPIErrorCodes)e.ErrorCode);
                        break;
                }
                throw;
            }
            catch (ArgumentException e)
            {
                throw new FormatException(SR.Get(SRID.RecognizerInvalidBinaryGrammar), e);
            }
            finally
            {
                gcHandle.Free();
            }

            SetSapiGrammarProperties(sapiGrammar, null, grammar.RuleName, enabled, weight, priority);
        }

        // Update a new SAPI grammar with relevant enabled, weight and priority and activate the desired rule.
        // SetRuleState on the rule is always set to active - theSetGrammarState API is used to enable or disable the grammar.
        // This needs to be a new grammar only because it only bothers to update the values of they are different to the default.
        private void SetSapiGrammarProperties(SapiGrammar sapiGrammar, Uri uri, string ruleName, bool enabled, float weight, int priority)
        {
            if (!enabled)
            {
                // SetGrammarState is ENABLED by default so only call if changed.
                sapiGrammar.SetGrammarState(SPGRAMMARSTATE.SPGS_DISABLED);
            }

            if (_supportsSapi53)
            {
                if (priority != 0)
                {
                    if (Grammar.IsDictationGrammar(uri))
                    {
                        throw new NotSupportedException(SR.Get(SRID.CannotSetPriorityOnDictation));
                    }
                    else
                    {
                        sapiGrammar.SetRulePriority(ruleName, 0, priority);
                    }
                }
                if (!weight.Equals(1.0f))
                {
                    if (Grammar.IsDictationGrammar(uri))
                    {
                        sapiGrammar.SetDictationWeight(weight);
                    }
                    else
                    {
                        sapiGrammar.SetRuleWeight(ruleName, 0, weight);
                    }
                }
            }
            else if (priority != 0 || !weight.Equals(1.0f))
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI));
            }

            // Always activate the rule
            // Do this after calling SetGrammarState so we don't accidentally enable the Grammar for recognition.
            ActivateRule(sapiGrammar, uri, ruleName);
        }

        // Method called on background thread to do actual grammar loading.
#pragma warning disable 56500 // Transferring exceptions to another thread
        private void LoadGrammarAsyncCallback(object grammarObject)
        {
            Debug.WriteLine("Loading grammar asynchronously.");

            // Note all of the items called on Grammar are simple properties so we don't
            // have any special locking even though this method could be called on different threads.

            Grammar grammar = (Grammar)grammarObject;
            InternalGrammarData grammarData = grammar.InternalData;

            // Right now you can't unload a grammar while it is being loaded, so the state must still be being "Loading"
            Debug.Assert(grammar.State == GrammarState.Loading);
            Debug.Assert(grammar.Recognizer == this);
            Debug.Assert(grammarData != null && grammarData._sapiGrammar != null);

            // Now load the grammar:

            // Keep track of any exceptions which we will store in the completed event args.
            Exception exception = null;
            try
            {
                // Take the lock here so if an app is updating properties on the grammar at this point on the main thread,
                // then the value is pulled and sapi updated atomically.
                // Note: This locks properties like Grammar.Enabled so if they are called while an async Grammar load is
                // in progress then they will block. This is probably okay for System.Speech, and could be avoided
                // by removing the actual call to load the grammar into sapi out of the lock.
                lock (_grammarDataLock)
                {
                    // The sapi grammar has already been created, so load the grammar data into SAPI:
                    LoadSapiGrammar(grammar, grammarData._sapiGrammar,
                        grammarData._grammarEnabled, grammarData._grammarWeight, grammarData._grammarPriority);

                    // Successful load - set the state:
                    grammar.State = GrammarState.Loaded;
                }

                Debug.WriteLine("Finished Loading grammar asynchronously.");
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                if (exception != null)
                {
                    Debug.WriteLine("Failed to load grammar asynchronously.");

                    // Need to do special logic to add grammar to collection but with LoadFailed state.
                    grammar.State = GrammarState.LoadFailed;
                    grammar.LoadException = exception;
                    // Wait until UnloadGrammar to release the sapi grammar object.
                }

                // Always release reader lock so if RecognizeAsync wants to start it can do so
                _waitForGrammarsToLoad.FinishOperation();

                // Always fire completed event
                _asyncWorkerUI.PostOperation(new WaitCallback(LoadGrammarAsyncCompletedCallback), grammarObject);
            }
        }

#pragma warning restore 56500

        // Method called by AsyncOperationManager on appropriate thread when async grammar loading completes.
        private void LoadGrammarAsyncCompletedCallback(object grammarObject)
        {
            Debug.WriteLine("Raising LoadGrammarCompleted event.");

            Grammar grammar = (Grammar)grammarObject;
            EventHandler<LoadGrammarCompletedEventArgs> loadGrammarCompletedHandler = LoadGrammarCompleted;
            if (loadGrammarCompletedHandler != null)
            {
                // When a LoadGrammarAsync completes all we must do is raise the LoadGrammarCompleted event.
                loadGrammarCompletedHandler(this, new LoadGrammarCompletedEventArgs(grammar, grammar.LoadException, false, null));
            }
        }

        // Create a new sapi grammarId and SapiGrammar object.
        // The algorithm starts at '1' and increments.
        // Eventually the numbers wrap around so you'll end up at 0 etc. which is fine.
        // We also check if a value is in use and then skip it.
        private SapiGrammar CreateNewSapiGrammar(out ulong grammarId)
        {
            ulong initialGrammarIdValue = _currentGrammarId;
            // No need to lock as enumerating _grammars on the main thread and only gets altered on the main thread
            do
            {
                _currentGrammarId++;

                bool foundCollision = false;
                lock (SapiRecognizer)
                {
                    foreach (Grammar g in _grammars)
                    {
                        if (_currentGrammarId == g.InternalData._grammarId)
                        {
                            // This can only be hit if _currentGrammarId has wrapped around past 2^64.
                            foundCollision = true;
                            break;
                        }
                    }
                }
                if (!foundCollision)
                {
                    SapiGrammar sapiGrammar = SapiContext.CreateGrammar(_currentGrammarId);
                    grammarId = _currentGrammarId;
                    return sapiGrammar;
                }
            }
            while (_currentGrammarId != initialGrammarIdValue);

            // This is not a realistic scenario because you'd need to have 2^64 grammars loaded to hit this, but it removes at least
            // a theoretical infinite loop.
            throw new InvalidOperationException(SR.Get(SRID.SapiErrorTooManyGrammars));
        }

        // Do some basic parameter validation on a passed in Grammar
        private void ValidateGrammar(Grammar grammar, params GrammarState[] validStates)
        {
            Helpers.ThrowIfNull(grammar, nameof(grammar));

            // Check if grammar is in a valid state for the caller.
            foreach (GrammarState state in validStates)
            {
                if (grammar.State == state)
                {
                    // Grammar is in a valid state, but is this the right Recognizer?
                    if (grammar.State != GrammarState.Unloaded && grammar.Recognizer != this)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.GrammarWrongRecognizer));
                    }

                    // Everything is fine - return.
                    return;
                }
            }

            // Grammar was not in correct state - produce exception.
            switch (grammar.State)
            {
                case GrammarState.Unloaded:
                    throw new InvalidOperationException(SR.Get(SRID.GrammarNotLoaded));
                case GrammarState.Loading:
                    throw new InvalidOperationException(SR.Get(SRID.GrammarLoadingInProgress));
                case GrammarState.LoadFailed:
                    throw new InvalidOperationException(SR.Get(SRID.GrammarLoadFailed));
                case GrammarState.Loaded:
                    throw new InvalidOperationException(SR.Get(SRID.GrammarAlreadyLoaded));
            }
        }

        private RecognitionResult InternalEmulateRecognize(string phrase, SpeechEmulationCompareFlags flag, bool useReco2, RecognizedWordUnit[] wordUnits)
        {
            RecognitionResult result = null;
            bool completed = false;
            EventHandler<EmulateRecognizeCompletedEventArgs> eventHandler = delegate (object sender, EmulateRecognizeCompletedEventArgs eventArgs)
            {
                result = eventArgs.Result;
                completed = true;
            };

            EmulateRecognizeCompletedSync += eventHandler;

            try
            {
                _asyncWorkerUI.AsyncMode = false;
                InternalEmulateRecognizeAsync(phrase, flag, useReco2, wordUnits);
                do
                {
                    _handlerWaitHandle.WaitOne();
                    _asyncWorkerUI.ConsumeQueue();
                } while (!completed && !_disposed);
            }
            finally
            {
                EmulateRecognizeCompletedSync -= eventHandler;
                _asyncWorkerUI.AsyncMode = true;
            }

            return result;
        }

        // Pass the Emulation information to SAPI
        private void InternalEmulateRecognizeAsync(string phrase, SpeechEmulationCompareFlags flag, bool useReco2, RecognizedWordUnit[] wordUnits)
        {
            lock (SapiRecognizer) // Lock to protect _isRecognizing and _haveInputSource
            {
                if (_isRecognizing)
                {
                    throw new InvalidOperationException(SR.Get(SRID.RecognizerAlreadyRecognizing));
                }

                _isRecognizing = true;
                _isEmulateRecognition = true;
            } // Not recognizing so no events firing - can unlock now

            if (useReco2 || _supportsSapi53)
            {
                // Create the structure to pass the recognition engine.
                IntPtr data;
                GCHandle[] memHandles = null;
                ISpPhrase iSpPhrase = null;

                if (wordUnits == null)
                {
                    iSpPhrase = SPPHRASE.CreatePhraseFromText(phrase.Trim(), RecognizerInfo.Culture, out memHandles, out data);
                }
                else
                {
                    iSpPhrase = SPPHRASE.CreatePhraseFromWordUnits(wordUnits, RecognizerInfo.Culture, out memHandles, out data);
                }

                try
                {
                    SAPIErrorCodes hr = SapiRecognizer.EmulateRecognition(iSpPhrase, (uint)(flag));
                    if (hr != SAPIErrorCodes.S_OK)
                    {
                        EmulateRecognizedFailReportError(hr);
                    }
                }
                finally
                {
                    foreach (GCHandle memHandle in memHandles)
                    {
                        memHandle.Free();
                    }
                    Marshal.FreeCoTaskMem(data);
                }
            }
            else
            {
                // Fast case
                SAPIErrorCodes hr = SapiRecognizer.EmulateRecognition(phrase);
                if (hr != SAPIErrorCodes.S_OK)
                {
                    EmulateRecognizedFailReportError(hr);
                }
            }
        }

        private void EmulateRecognizedFailReportError(SAPIErrorCodes hr)
        {
            _lastException = ExceptionFromSapiCreateRecognizerError(hr);

            //
            // Do not fire the recognize completed event if we know that we will receive
            // a recognition event eventually; as doing so will lead to premature completion
            // of the recognition task without raising any recognition events.
            //

            //
            // We do not have recognition event for SP_NO_ACTIVE_RULE (thus should complete immediately),
            // but we have (false) recognition for the other two SP_NO_PARSE_FOUND and S_FALSE.
            //
            if ((int)hr < 0 || hr == SAPIErrorCodes.SP_NO_RULE_ACTIVE)
            {
                FireEmulateRecognizeCompletedEvent(null, _lastException, true);
            }
        }

        // Set the desired rule to either the active or active_with_auto_pause state.
        // This method is used when a grammar is first loaded, and if the PauseRecognizerOnRecognition property is changed.
        private void ActivateRule(SapiGrammar sapiGrammar, Uri uri, string ruleName)
        {
            SPRULESTATE ruleState = _pauseRecognizerOnRecognition ? SPRULESTATE.SPRS_ACTIVE_WITH_AUTO_PAUSE : SPRULESTATE.SPRS_ACTIVE;

            SAPIErrorCodes errorCode;
            if (Grammar.IsDictationGrammar(uri))
            {
                errorCode = sapiGrammar.SetDictationState(ruleState);
            }
            else
            {
                errorCode = sapiGrammar.SetRuleState(ruleName, ruleState);
            }

            if (errorCode == SAPIErrorCodes.SPERR_NOT_TOPLEVEL_RULE || errorCode == SAPIErrorCodes.SP_NO_RULES_TO_ACTIVATE)
            {
                if (uri == null)
                {
                    if (string.IsNullOrEmpty(ruleName))
                    {
                        throw new FormatException(SR.Get(SRID.RecognizerNoRootRuleToActivate));
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.RecognizerRuleNotFoundStream, ruleName), nameof(ruleName));
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(ruleName))
                    {
                        throw new FormatException(SR.Get(SRID.RecognizerNoRootRuleToActivate1, uri));
                    }
                    else
                    {
                        throw new ArgumentException(SR.Get(SRID.RecognizerRuleNotFound, ruleName, uri), nameof(ruleName));
                    }
                }
            }

            // We can proceed if the audio is not found as this call could be for emulation.
            else if (errorCode != SAPIErrorCodes.SPERR_AUDIO_NOT_FOUND && errorCode < 0)
            {
                ThrowIfSapiErrorCode(errorCode);
                throw new COMException(SR.Get(SRID.RecognizerRuleActivationFailed), (int)errorCode);
            }
        }

        // Method called on background thread {from RecognizeAsync} to start recognition process.
#pragma warning disable 56500 // Transferring exceptions to another thread

        private void RecognizeAsyncWaitForGrammarsToLoad(object unused)
        {
            Debug.WriteLine("Waiting for any pending grammar loads to complete.");
            // First we must wait until all pending grammars have loaded.
            // Once we have the lock can release immediately - there's no need to hold on to it
            _waitForGrammarsToLoad.WaitForOperationsToFinish();

            Exception exception = null; // Keep track of any error we need to throw
            bool cancelled = false; // If you call cancel while grammars are loading we don't bother starting recognition.

            lock (SapiRecognizer)
            {
                foreach (Grammar grammar in _grammars)
                {
                    // Note all of the items called on Grammar are simple properties so we don't
                    // have any special locking even though this method could be called on different threads.

                    if (grammar.State == GrammarState.LoadFailed)
                    {
                        // Note: For now there's no special exception for when RecognizeAsync fails because a grammar load failed.
                        // Instead just use whatever grammar exception was fired.
                        Debug.WriteLine("Problem loading grammars.");
                        exception = grammar.LoadException;
                        break;
                    }
                }

                Debug.Assert(_isRecognizing);

                // The app may have called RecognizeAsyncCancel by no so abort at this point and don't bother starting SAPI.
                if (_isRecognizeCancelled)
                {
                    Debug.WriteLine("Recognition cancelled while waiting for grammars to load.");
                    cancelled = true;
                }
            }

            // Now start the recognizer there was no exception and we are not cancelled.
            if (exception == null && !cancelled)
            {
                try
                {
                    if (!_isEmulateRecognition)
                    {
                        SapiRecognizer.SetRecoState(SPRECOSTATE.SPRST_ACTIVE_ALWAYS);
                        Debug.WriteLine("Grammar loads completed, recognition started.");
                    }
                }
                catch (COMException comException)
                {
                    Debug.WriteLine("Problem starting recognition - sapi exception.");
                    exception = ExceptionFromSapiStreamError((SAPIErrorCodes)comException.ErrorCode);
                }
                catch (Exception fatalException)
                {
                    Debug.WriteLine("Problem starting recognition - unknown exception.");
                    exception = fatalException;
                }
            }

            // If either an exception or the cancellation has occurred then we need to throw the RecognizeCompleted right away.
            // Otherwise it will be thrown later when SAPI sends the EndStream event.
            if (exception != null || cancelled)
            {
                RecognizeCompletedEventArgs eventArgs = new(null, false, false, false, TimeSpan.Zero, exception, cancelled, null);
                _asyncWorkerUI.PostOperation(new WaitCallback(RecognizeAsyncWaitForGrammarsToLoadFailed), eventArgs);
            }
        }
#pragma warning restore 56500

        // Method called on app thread model used to fire the RecognizeCompelted event args if recognition stopped prematurely
        private void RecognizeAsyncWaitForGrammarsToLoadFailed(object eventArgs)
        {
            Debug.WriteLine("Firing RecognizeCompleted because recognition didn't start as expected.");
            Debug.Assert(eventArgs != null);

            lock (SapiRecognizer) // Lock to protect _isRecognizing and _isRecognizeCancelled
            {
                // Might have got here because recognition was cancelled so reset flags.
                Debug.Assert(_isRecognizing);
                _isRecognizeCancelled = false;
                _isRecognizing = false;
            }

            // Now raise RecognizeCompleted event.
            EventHandler<RecognizeCompletedEventArgs> recognizeCompletedHandler = RecognizeCompleted;
            if (recognizeCompletedHandler != null)
            {
                recognizeCompletedHandler(this, (RecognizeCompletedEventArgs)eventArgs);
            }
        }

        // This method will be called asynchronously
        private void SignalHandlerThread(object ignored)
        {
            if (_asyncWorkerUI.AsyncMode == false)
            {
                _handlerWaitHandle.Set();
            }
        }

        // Main handler of sapi events. This method will be called asynchronously
        private void DispatchEvents(object eventData)
        {
            lock (_thisObjectLock)
            {
                SpeechEvent speechEvent = eventData as SpeechEvent;
                if (!_disposed && eventData != null)
                {
                    switch (speechEvent.EventId)
                    {
                        case SPEVENTENUM.SPEI_START_SR_STREAM:
                            ProcessStartStreamEvent();
                            break;

                        case SPEVENTENUM.SPEI_PHRASE_START:
                            ProcessPhraseStartEvent(speechEvent);
                            break;

                        case SPEVENTENUM.SPEI_SR_BOOKMARK:
                            ProcessBookmarkEvent(speechEvent);
                            break;

                        case SPEVENTENUM.SPEI_HYPOTHESIS:
                            ProcessHypothesisEvent(speechEvent);
                            break;

                        case SPEVENTENUM.SPEI_FALSE_RECOGNITION:
                        case SPEVENTENUM.SPEI_RECOGNITION:
                            ProcessRecognitionEvent(speechEvent);
                            break;

                        case SPEVENTENUM.SPEI_RECO_OTHER_CONTEXT:
                            ProcessRecoOtherContextEvent();
                            break;

                        case SPEVENTENUM.SPEI_END_SR_STREAM:
                            ProcessEndStreamEvent(speechEvent);
                            break;

                        case SPEVENTENUM.SPEI_INTERFERENCE:
                            ProcessInterferenceEvent((uint)speechEvent.LParam);
                            break;

                        case SPEVENTENUM.SPEI_SR_AUDIO_LEVEL:
                            ProcessAudioLevelEvent((int)speechEvent.WParam);
                            break;
                    }
                }
            }
        }

        private void ProcessStartStreamEvent()
        {
            lock (SapiRecognizer)
            {
                _audioState = AudioState.Silence;
            }

            // Fire events
            FireAudioStateChangedEvent(_audioState);
            FireStateChangedEvent(RecognizerState.Listening);

            // Set the initial silence timeout running.
            // We wait until this event in case there was some error that prevented the recognition from starting.

            TimeSpan initialSilenceTimeout = InitialSilenceTimeout; // This gets the value in a thread-safe manner.

            // Add bookmark at desired InitialSilence Timeout
            if (_recognizeMode == RecognizeMode.Single && initialSilenceTimeout != TimeSpan.Zero)
            {
                if (_supportsSapi53)
                {
                    SapiContext.Bookmark(SPBOOKMARKOPTIONS.SPBO_TIME_UNITS | SPBOOKMARKOPTIONS.SPBO_PAUSE,
                        (ulong)initialSilenceTimeout.Ticks, new IntPtr((int)_initialSilenceBookmarkId));
                }
                else
                {
                    SapiContext.Bookmark(SPBOOKMARKOPTIONS.SPBO_PAUSE,
                        TimeSpanToStreamPosition(initialSilenceTimeout), new IntPtr((int)_initialSilenceBookmarkId));
                }
                _detectingInitialSilenceTimeout = true;
            }
        }

        private void ProcessPhraseStartEvent(SpeechEvent speechEvent)
        {
            // A phrase start event should be followed by a Recognition or False Recognition event
            _isWaitingForRecognition = true;

            lock (SapiRecognizer)
            {
                _audioState = AudioState.Speech;
            }
            FireAudioStateChangedEvent(_audioState);

            // Set the babble timeout running.

            // Cancel any InitialSilenceTimeout detection.
            _detectingInitialSilenceTimeout = false;

            TimeSpan babbleTimeout = BabbleTimeout; // This gets the value in a thread-safe manner.

            // Add bookmark at BabbleTimeout
            if (_recognizeMode == RecognizeMode.Single && babbleTimeout != TimeSpan.Zero)
            {
                // Don't make this a pausing bookmark or it will have to wait for the engine to reach a sync point ...
                if (_supportsSapi53)
                {
                    SapiContext.Bookmark(SPBOOKMARKOPTIONS.SPBO_TIME_UNITS,
                        (ulong)((babbleTimeout + speechEvent.AudioPosition).Ticks), new IntPtr((int)_babbleBookmarkId));
                }
                else
                {
                    SapiContext.Bookmark(SPBOOKMARKOPTIONS.SPBO_NONE,
                        TimeSpanToStreamPosition(babbleTimeout) + speechEvent.AudioStreamOffset, new IntPtr((int)_babbleBookmarkId));
                }
                _detectingBabbleTimeout = true;
            }

            // Fire the SpeechDetected event.
            FireSpeechDetectedEvent(speechEvent.AudioPosition);
        }

        private void ProcessBookmarkEvent(SpeechEvent speechEvent)
        {
            // A bookmark can either be triggered from a timeout,
            // in which case the recognition process is stopped;
            // or from a call to RequestRecognizerUpdate, in
            // which case the RecognizerUpdateReached event is raised.

            uint bookmarkId = (uint)speechEvent.LParam;

            // We always call Resume even on error so have a try - finally block;
            try
            {
                if (bookmarkId == _initialSilenceBookmarkId)
                {
                    if (_detectingInitialSilenceTimeout) // If a phrase start has already happened we still get the bookmark but should ignore it.
                    {
                        EndRecognitionWithTimeout();
                    }
                }
                else if (bookmarkId == _babbleBookmarkId)
                {
                    // If a phrase start has already happened we still get the bookmark but should ignore it.
                    // Similarly don't ever fire both timeouts.
                    if (_detectingBabbleTimeout && !_initialSilenceTimeoutReached)
                    {
                        // Otherwise set the flag and cancel the recognition.
                        _babbleTimeoutReached = true;
                        SapiRecognizer.SetRecoState(SPRECOSTATE.SPRST_INACTIVE_WITH_PURGE);
                    }
                }
                else // Not a timeout so a real request to pause the engine
                {
                    object userToken = GetBookmarkItemAndRemove(bookmarkId);

                    EventHandler<RecognizerUpdateReachedEventArgs> updateHandler = RecognizerUpdateReached;
                    if (updateHandler != null)
                    {
                        updateHandler(this, new RecognizerUpdateReachedEventArgs(userToken, speechEvent.AudioPosition));
                    }
                }
            }
            catch (COMException e)
            {
                throw ExceptionFromSapiCreateRecognizerError(e);
            }
            finally
            {
                // Always want to call Resume otherwise the engine will remain in the pause state.
                // Currently all bookmarks pause but we check anyway for safety.
                if (((SPRECOEVENTFLAGS)speechEvent.WParam & SPRECOEVENTFLAGS.SPREF_AutoPause) != 0)
                {
                    SapiContext.Resume();
                }
            }
        }

        private void ProcessHypothesisEvent(SpeechEvent speechEvent)
        {
            RecognitionResult result = CreateRecognitionResult(speechEvent);

            bool enabled;
            lock (SapiRecognizer) // Lock to protect _grammarEnabled
            {
                enabled = _enabled;
            }

            // If the result corresponds to a real, active grammar (result.Grammar != null),
            // And the Enabled property is set,
            // then proceed and raise the event.
            // Otherwise, the Grammar has been unloaded or deactivated so skip the event.
            if (result.Grammar != null && result.Grammar.Enabled && enabled)
            {
                Debug.Assert(result.Grammar.State == GrammarState.Loaded);

                // Fire the hypothesis event.
                FireSpeechHypothesizedEvent(result);
            }
        }

        private void ProcessRecognitionEvent(SpeechEvent speechEvent)
        {
            // First disable timeouts.
            _detectingInitialSilenceTimeout = false;
            _detectingBabbleTimeout = false;
            bool isRecognizeCancelled = true;
            bool isEmulate = (speechEvent.WParam & (ulong)SPRECOEVENTFLAGS.SPREF_Emulated) != 0;

            try
            {
                RecognitionResult result = CreateRecognitionResult(speechEvent);

                bool enabled;
                lock (SapiRecognizer) // Lock to protect _grammarEnabled, _isRecognizeCancelled, and _audioStatus.
                {
                    _audioState = AudioState.Silence;
                    isRecognizeCancelled = _isRecognizeCancelled;
                    enabled = _enabled;
                }

                FireAudioStateChangedEvent(_audioState);

                // If the result corresponds to a real, active grammar (result.Grammar != null),
                // Or the result corresponds to an event which belongs to no grammar (result.GrammarId == 0),
                // And the Enabled property is set,
                // then proceed and raise the event.
                // Otherwise, the Grammar has been unloaded or deactivated so skip the event.
                // Note: this doesn't absolutely guarantee an event won't be fired after the grammar is unloaded
                // - there's a small window after this check is done and before the event fires where the grammar could get
                // unloaded. To fix this would require more strict locking here.
                if (((result.Grammar != null && result.Grammar.Enabled) ||
                    (speechEvent.EventId == SPEVENTENUM.SPEI_FALSE_RECOGNITION && result.GrammarId == 0)) &&
                    enabled)
                {
                    if (speechEvent.EventId == SPEVENTENUM.SPEI_RECOGNITION)
                    {
                        // Remember the last result so we can fire it again in the RecognitionCompleted event.
                        // Note this is only done for Recognition, not for a rejected Recognition.
                        _lastResult = result;

                        // Fire the recognition on the grammar.
                        SpeechRecognizedEventArgs recognitionEventArgs = new(result);
                        result.Grammar.OnRecognitionInternal(recognitionEventArgs);

                        // Fire the recognition on the recognizer.
                        FireSpeechRecognizedEvent(recognitionEventArgs);
                    }
                    else
                    {
                        // Although we send a result in RecognitionRejected event, we would want a null
                        // result in RecognitionCompleted event.
                        _lastResult = null;

                        // SPEVENTENUM.SPEI_FALSE_RECOGNITION
                        // Fire the event but if SAPI will fire an empty false recognition after each timeout
                        // or when the recognition has been shut off. Don't report these events since then don't contain useful info.
                        if (result.GrammarId != 0 || !(_babbleTimeoutReached || isRecognizeCancelled))
                        {
                            // Fire the rejected recognition on the recognizer.
                            FireSpeechRecognitionRejectedEvent(result);
                        }
                    }
                }
                // else Grammar has already been unloaded or disabled - so don't fire result

            }
            finally // Even if event handler throws we should call this
            {
                if (_recognizeMode == RecognizeMode.Single)
                {
                    // Always stop recognizer after each recognition or false recognition in Automatic mode.
                    // - Same as RecognizeAsyncCancel but don't want to set _isRecognizeCancelled flag;
                    try
                    {
                        SapiRecognizer.SetRecoState(SPRECOSTATE.SPRST_INACTIVE_WITH_PURGE);
                    }
                    catch (COMException e)
                    {
                        throw ExceptionFromSapiCreateRecognizerError(e);
                    }
                }

                if (((SPRECOEVENTFLAGS)speechEvent.WParam & SPRECOEVENTFLAGS.SPREF_AutoPause) != 0)
                {
                    SapiContext.Resume();
                }
            }

            //
            // Set a flag so we will fire recognition completed event when we receive SR_END_STREAM.
            //
            // In the inproc case, we will not be able to do simultaneous recognition, so this is
            // the recognition we are waiting for.
            // In the shared case, we can do emulation during recognition, but we only wait for the
            // emulate result.
            //
            if (_inproc || isEmulate)
            {
                _isWaitingForRecognition = false;
            }
            if (isEmulate && !_inproc)
            {
                // Fire the EmulateRecognizeCompleted event
                FireEmulateRecognizeCompletedEvent(_lastResult, _lastException, isRecognizeCancelled);
            }
        }

        private void ProcessRecoOtherContextEvent()
        {
            if (_isEmulateRecognition && !_inproc)
            {
                // Fire the EmulateRecognizeCompleted event
                FireEmulateRecognizeCompletedEvent(_lastResult, _lastException, false);
            }

            lock (SapiRecognizer)
            {
                _audioState = AudioState.Silence;
            }
            FireAudioStateChangedEvent(_audioState);
        }

        private void ProcessEndStreamEvent(SpeechEvent speechEvent)
        {
            //
            // Emulation on SAPI5.1 can send bogus end stream events before a recognition
            //
            if (!_supportsSapi53 && _isEmulateRecognition && _isWaitingForRecognition)
            {
                return;
            }

            // All queued bookmarks can be removed now.
            // Don't reset with EmulatedResults - because you can Emulate during a recognition {in SpeechRecognizer},
            // this means multiple EndStreamEvents can be fired together which confuses the BookmarkTable clean-up.
            if (((SPENDSRSTREAMFLAGS)speechEvent.WParam & SPENDSRSTREAMFLAGS.SPESF_EMULATED) == 0)
            {
                ResetBookmarkTable();
            }

            // Remember variables we need later.
            bool initialSilenceTimeoutReached = _initialSilenceTimeoutReached;
            bool babbleTimeoutReached = _babbleTimeoutReached;

            RecognitionResult lastResult = _lastResult;
            Exception lastException = _lastException;

            // Reset all variables so you can restart recognition immediately (from within RecognizeCompleted event handler).
            _initialSilenceTimeoutReached = false;
            _babbleTimeoutReached = false;
            _detectingInitialSilenceTimeout = false;
            _detectingBabbleTimeout = false;
            _lastResult = null;
            _lastException = null;

            bool isStreamReleased = false;
            bool isRecognizeCancelled;
            lock (SapiRecognizer) // Lock to protect _isRecognizing, _isRecognizeCancelled, _haveInputSource, _audioFormat, _audioStatus.
            {
                _audioState = AudioState.Stopped;

                if (((SPENDSRSTREAMFLAGS)speechEvent.WParam & SPENDSRSTREAMFLAGS.SPESF_STREAM_RELEASED) == SPENDSRSTREAMFLAGS.SPESF_STREAM_RELEASED)
                {
                    isStreamReleased = true;
                    _haveInputSource = false;
                }

                isRecognizeCancelled = _isRecognizeCancelled;

                _isRecognizeCancelled = false;
                _isRecognizing = false;
            }

            Debug.Assert(!(initialSilenceTimeoutReached && babbleTimeoutReached)); // Both timeouts should not be set
            FireAudioStateChangedEvent(_audioState);

            // Fire the RecognizeCompleted event. (Except in the emulation case)
            if (!_isEmulateRecognition)
            {
                FireRecognizeCompletedEvent(lastResult, initialSilenceTimeoutReached, babbleTimeoutReached, isStreamReleased, speechEvent.AudioPosition, (speechEvent.LParam == 0) ? null : ExceptionFromSapiStreamError((SAPIErrorCodes)speechEvent.LParam), isRecognizeCancelled);
            }
            else
            {
                //
                // followed by a recognition/false recognition event. But it is not the case at this point as we
                // actually receive multiple SR_END_STREAM events for a single emulation, and the first SR_END_STREAM
                // is not proceeded by a recognition event. Until we found the problem in SAPI, this is only a workaround
                //

                // Fire the EmulateRecognizeCompleted event
                // Don't reset with EmulatedResults - because you can Emulate during a recognition {in SpeechRecognizer},
                // this means multiple EndStreamEvents can be fired together which confuses the BookmarkTable clean-up.

                FireEmulateRecognizeCompletedEvent(lastResult, (speechEvent.LParam == 0) ? lastException : ExceptionFromSapiStreamError((SAPIErrorCodes)speechEvent.LParam), isRecognizeCancelled);
            }

            // Fire state changed event
            FireStateChangedEvent(RecognizerState.Stopped);
        }

        private void ProcessInterferenceEvent(uint interference)
        {
            // Don't actually read the value here because we get it in a call to GetStatus later.
            FireSignalProblemOccurredEvent((AudioSignalProblem)interference);
        }

        private void ProcessAudioLevelEvent(int audioLevel)
        {
            // Don't actually read the value here because we get it in a call to GetStatus later.
            FireAudioLevelUpdatedEvent(audioLevel);
        }

        private void EndRecognitionWithTimeout()
        {
            _initialSilenceTimeoutReached = true;

            // Got a timeout so cancel Recognition.
            // - Same as RecognizeAsyncCancel but don't want to set _isRecognizeCancelled flag;

            SapiRecognizer.SetRecoState(SPRECOSTATE.SPRST_INACTIVE_WITH_PURGE);

            // Note we don't directly raise a SpeechRecognitionRejected event in this scenario.
            // However SAPI should always raise a FALSE_RECOGNITION after canceling.
        }

        private RecognitionResult CreateRecognitionResult(SpeechEvent speechEvent)
        {
            // Get the sapi result
            ISpRecoResult sapiResult = (ISpRecoResult)Marshal.GetObjectForIUnknown((IntPtr)speechEvent.LParam);
            RecognitionResult recoResult = null;

            // Get the serialized unmanaged blob and then delete the sapi result
            IntPtr coMemSerializeBlob;
            sapiResult.Serialize(out coMemSerializeBlob);
            byte[] serializedBlob = null;

            try
            {
                // Convert the unmanaged blob to managed and delete the unmanaged memory
                uint sizeOfSerializedBlob = (uint)Marshal.ReadInt32(coMemSerializeBlob);
                serializedBlob = new byte[sizeOfSerializedBlob];
                Marshal.Copy(coMemSerializeBlob, serializedBlob, 0, (int)sizeOfSerializedBlob);
            }
            finally
            {
                Marshal.FreeCoTaskMem(coMemSerializeBlob);
            }
            // Now create a RecognitionResult.
            // For normal recognitions and false recognitions this will have all the information in it.
            // For a false recognition with no phrase the result should still be valid, just empty.
            recoResult = new RecognitionResult(this, sapiResult, serializedBlob, MaxAlternates);

            return recoResult;
        }

        // Reset the AudioFormat property - needed when the format might have changed.
        // Also update the EventNotify so it can calculate event AudioPositions from byte offsets correctly.
        private void UpdateAudioFormat(SpeechAudioFormatInfo audioFormat)
        {
            lock (SapiRecognizer) // Lock to protect _audioFormat
            {
                // This code could be skipped for SAPI 5.3 - just reset _audioFormat and _eventNotify.AudioFormat to null.
                // But for consistency do the same in both scenarios.
                try
                {
                    _audioFormat = GetSapiAudioFormat();
                }
                catch (ArgumentException)
                {
                    _audioFormat = audioFormat;
                }
                _eventNotify.AudioFormat = _audioFormat; // Update EventNotify so subsequent events get correct AudioPosition.
            }
        }

        // Calls through to Sapi to get the current engine audio format.
        private SpeechAudioFormatInfo GetSapiAudioFormat()
        {
            IntPtr waveFormatPtr = IntPtr.Zero;
            SpeechAudioFormatInfo formatInfo = null;
            bool hasWaveFormat = false;
            try
            {
                try
                {
                    // Get the format for that engine
                    waveFormatPtr = SapiRecognizer.GetFormat(SPSTREAMFORMATTYPE.SPWF_SRENGINE);
                    if (waveFormatPtr != IntPtr.Zero)
                    {
                        if ((formatInfo = AudioFormatConverter.ToSpeechAudioFormatInfo(waveFormatPtr)) != null)
                        {
                            hasWaveFormat = true;
                        }
                    }
                }
                catch (COMException)
                {
                }

                // If for some reason the GetFormat fails OR we can't get a wave format, assume 16 Kb, 16 bits, Audio.
                if (!hasWaveFormat)
                {
                    formatInfo = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
                }
            }
            finally
            {
                if (waveFormatPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(waveFormatPtr);
                }
            }
            return formatInfo;
        }

        // Convert a TimeSpan such as initialSilenceTimeout to a byte offset using the
        // current audio format. This should only needed if not using SAPI 5.3.
        private ulong TimeSpanToStreamPosition(TimeSpan time)
        {
            return (ulong)(time.Ticks * AudioFormat.AverageBytesPerSecond) / TimeSpan.TicksPerSecond;
        }

        // Converts COM errors returned by SPEI_END_SR_STREAM or SetRecoState to an appropriate .NET exception.
        private static void ThrowIfSapiErrorCode(SAPIErrorCodes errorCode)
        {
            SRID srid = SapiConstants.SapiErrorCode2SRID(errorCode);
            if ((int)srid >= 0)
            {
                throw new InvalidOperationException(SR.Get(srid));
            }
        }

        // Converts COM errors returned by SPEI_END_SR_STREAM or SetRecoState to an appropriate .NET exception.
        private static Exception ExceptionFromSapiStreamError(SAPIErrorCodes errorCode)
        {
            SRID srid = SapiConstants.SapiErrorCode2SRID(errorCode);
            if ((int)srid >= 0)
            {
                return new InvalidOperationException(SR.Get(srid));
            }
            else
            {
                return new COMException(SR.Get(SRID.AudioDeviceInternalError), (int)errorCode);
            }
        }

        // Convert the .NET CompareOptions into the SAPI SpeechEmulationCompareFlags.
        private static SpeechEmulationCompareFlags ConvertCompareOptions(CompareOptions compareOptions)
        {
            CompareOptions handledOptions = CompareOptions.IgnoreCase | CompareOptions.OrdinalIgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.Ordinal;
            SpeechEmulationCompareFlags flags = 0;
            if ((compareOptions & CompareOptions.IgnoreCase) != 0 || (compareOptions & CompareOptions.OrdinalIgnoreCase) != 0)
            {
                flags |= SpeechEmulationCompareFlags.SECFIgnoreCase;
            }
            if ((compareOptions & CompareOptions.IgnoreKanaType) != 0)
            {
                flags |= SpeechEmulationCompareFlags.SECFIgnoreKanaType;
            }
            if ((compareOptions & CompareOptions.IgnoreWidth) != 0)
            {
                flags |= SpeechEmulationCompareFlags.SECFIgnoreWidth;
            }
            if ((compareOptions & ~handledOptions) != 0)
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPICompareOption));
            }
            return flags;
        }

        // Methods to add and remove SAPI event interests.

        internal void AddEventInterest(ulong interest)
        {
            if ((_eventInterest & interest) != interest)
            {
                _eventInterest |= interest;
                SapiContext.SetInterest(_eventInterest, _eventInterest);
            }
        }

        internal void RemoveEventInterest(ulong interest)
        {
            if ((_eventInterest & interest) != 0)
            {
                _eventInterest &= ~interest;
                SapiContext.SetInterest(_eventInterest, _eventInterest);
            }
        }

        // Bookmark related methods:
        // A dictionary is used to map between userToken objects supplied to the RequestRecognizerUpdate event
        // and the sapi bookmark lparam value.
        // The uint key from the dictionary is stored in the sapi event and used to recover the userToken reference.
        // The following methods encapsulate this functionality.
        // The bookmarks used for the InitialSilenceTimeout and BabbleTimeout are also stored in this table.
        // To prevent the dictionary growing too much, each bookmark event removes itself from the hashtable, the end stream event clears the table.

        private uint AddBookmarkItem(object userToken)
        {
            uint bookmarkId = 0;
            if (userToken != null) // Null item always maps to zero id.
            {
                lock (_bookmarkTable) // Lock to protect _nextBookmarkId and _bookmarkTable
                {
                    bookmarkId = _nextBookmarkId++; // Find the next bookmark id to use.

                    if (_nextBookmarkId == 0)
                    {
                        // As long as there are not 2^32 outstanding bookmarks this will work fine.
                        // There's also a case where the bookmark table doesn't completely reset
                        // during ResetBookmarkTable but this would require 2^32 bookmarks also.
                        throw new InvalidOperationException(SR.Get(SRID.RecognizerUpdateTableTooLarge));
                    }

                    _bookmarkTable[unchecked((int)bookmarkId)] = userToken;
                    Debug.WriteLine("Added bookmark: " + bookmarkId + " " + userToken);
                }
            }
            return bookmarkId;
        }

        private void ResetBookmarkTable()
        {
            lock (_bookmarkTable) // Lock to protect _nextBookmarkId and _bookmarkTable
            {
                // Don't delete every bookmark, because there's an edge case where a bookmark,
                // can be requested just before the EndStream event and be fired just after.
                // So only clear the table up to the max value from the PREVIOUS recognition.

                // There's no way to enumerate through the Dictionary while deleting some keys.
                // So make a copy of the keys first.
                if (_bookmarkTable.Count > 0)
                {
                    int[] keysArray = new int[_bookmarkTable.Count];
                    _bookmarkTable.Keys.CopyTo(keysArray, 0);
                    for (int i = 0; i < keysArray.Length; i++)
                    {
                        if (keysArray[i] <= _prevMaxBookmarkId)
                        {
                            _bookmarkTable.Remove(keysArray[i]);
                        }
                    }
                }

                if (_bookmarkTable.Count == 0)
                {
                    // Now reset the _nextBookmarkId.
                    // Remember that several values are predefined and must not be used, so reset to _intialBookmarkId
                    _nextBookmarkId = _firstUnusedBookmarkId;
                    _prevMaxBookmarkId = _firstUnusedBookmarkId - 1;
                }
                else
                {
                    // If there's still bookmarks in the table that might still fire,
                    // then update _prevMaxBookmarkId. At the end of the next recognition they will be deleted.
                    _prevMaxBookmarkId = _nextBookmarkId - 1;
                }
                //Debug.WriteLine ("Reset bookmarks: count=" + _bookmarkTable.Count + " max=" + _prevMaxBookmarkId + " next=" + _nextBookmarkId);
            }
        }

        private object GetBookmarkItemAndRemove(uint bookmarkId)
        {
            object userToken = null;
            if (bookmarkId != 0) // Zero is a special case where the lookup table is bypassed.
            {
                lock (_bookmarkTable) // Lock to protect _bookmarkTable
                {
                    int id = unchecked((int)bookmarkId);
                    userToken = _bookmarkTable[id];
                    _bookmarkTable.Remove(id);
                    Debug.WriteLine("Fired bookmark: " + bookmarkId + " " + userToken);
                }
            }
            return userToken;
        }

        private void CloseCachedInputStream()
        {
            if (_inputStream != null)
            {
                _inputStream.Close();
                _inputStream = null;
            }
        }

        /// <summary>
        /// Fire audio status changed event
        /// </summary>
        private void FireAudioStateChangedEvent(AudioState audioState)
        {
            EventHandler<AudioStateChangedEventArgs> audioStateChangedHandler = _audioStateChangedDelegate;
            if (audioStateChangedHandler != null)
            {
                _asyncWorkerUI.PostOperation(audioStateChangedHandler, this, new AudioStateChangedEventArgs(audioState));
            }
        }

        /// <summary>
        /// Fire audio status changed event
        /// </summary>
        private void FireSignalProblemOccurredEvent(AudioSignalProblem audioSignalProblem)
        {
            EventHandler<AudioSignalProblemOccurredEventArgs> audioSignalProblemOccurredHandler = _audioSignalProblemOccurredDelegate;
            if (audioSignalProblemOccurredHandler != null)
            {
                TimeSpan recognizerPosition = TimeSpan.Zero;
                TimeSpan audioPosition = TimeSpan.Zero;

                try
                {
                    // These calls do not wait for engine sync point so should be fast.
                    SPRECOGNIZERSTATUS recoStatus;
                    recoStatus = SapiRecognizer.GetStatus();

                    lock (SapiRecognizer) // Lock to protect _audioStatus.
                    {
                        SpeechAudioFormatInfo audioFormat = AudioFormat;
                        audioPosition = audioFormat.AverageBytesPerSecond > 0 ? new TimeSpan((long)((recoStatus.AudioStatus.CurDevicePos * TimeSpan.TicksPerSecond) / (ulong)audioFormat.AverageBytesPerSecond)) : TimeSpan.Zero;
                        recognizerPosition = new TimeSpan((long)recoStatus.ullRecognitionStreamTime);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionFromSapiCreateRecognizerError(e);
                }

                _asyncWorkerUI.PostOperation(audioSignalProblemOccurredHandler, this, new AudioSignalProblemOccurredEventArgs(audioSignalProblem, AudioLevel, audioPosition, recognizerPosition));
            }
        }

        /// <summary>
        /// Fire audio status changed event
        /// </summary>
        private void FireAudioLevelUpdatedEvent(int audioLevel)
        {
            EventHandler<AudioLevelUpdatedEventArgs> audioLevelUpdatedHandler = _audioLevelUpdatedDelegate;
            if (audioLevelUpdatedHandler != null)
            {
                _asyncWorkerUI.PostOperation(audioLevelUpdatedHandler, this, new AudioLevelUpdatedEventArgs(audioLevel));
            }
        }

        private void FireStateChangedEvent(RecognizerState recognizerState)
        {
            // Fire state changed event
            EventHandler<StateChangedEventArgs> stateChangedHandler = StateChanged;
            if (stateChangedHandler != null)
            {
                _asyncWorkerUI.PostOperation(stateChangedHandler, this, new StateChangedEventArgs(recognizerState));
            }
        }
        /// <summary>
        /// Fire the SpeechDetected event.
        /// </summary>
        private void FireSpeechDetectedEvent(TimeSpan audioPosition)
        {
            EventHandler<SpeechDetectedEventArgs> speechDetectedHandler = SpeechDetected;
            if (speechDetectedHandler != null)
            {
                _asyncWorkerUI.PostOperation(speechDetectedHandler, this, new SpeechDetectedEventArgs(audioPosition));
            }
        }

        /// <summary>
        /// Fire the hypothesis event.
        /// </summary>
        private void FireSpeechHypothesizedEvent(RecognitionResult result)
        {
            EventHandler<SpeechHypothesizedEventArgs> speechHypothesizedHandler = _speechHypothesizedDelegate;
            if (speechHypothesizedHandler != null)
            {
                _asyncWorkerUI.PostOperation(speechHypothesizedHandler, this, new SpeechHypothesizedEventArgs(result));
            }
        }

        /// <summary>
        /// Fire the rejected recognition on the recognizer.
        /// </summary>
        private void FireSpeechRecognitionRejectedEvent(RecognitionResult result)
        {
            EventHandler<SpeechRecognitionRejectedEventArgs> recognitionHandler = SpeechRecognitionRejected;
            SpeechRecognitionRejectedEventArgs recognitionEventArgs = new(result);
            if (recognitionHandler != null)
            {
                _asyncWorkerUI.PostOperation(recognitionHandler, this, recognitionEventArgs);
            }
        }

        /// <summary>
        /// Fire the recognition on the grammar.
        /// </summary>
        private void FireSpeechRecognizedEvent(SpeechRecognizedEventArgs recognitionEventArgs)
        {
            EventHandler<SpeechRecognizedEventArgs> recognitionHandler = SpeechRecognized;
            if (recognitionHandler != null)
            {
                _asyncWorkerUI.PostOperation(recognitionHandler, this, recognitionEventArgs);
            }
        }

        /// <summary>
        /// Fire the recognition completed event.
        /// </summary>
        private void FireRecognizeCompletedEvent(RecognitionResult result, bool initialSilenceTimeoutReached, bool babbleTimeoutReached, bool isStreamReleased, TimeSpan audioPosition, Exception exception, bool isRecognizeCancelled)
        {
            // In the synchronous case, fire the private event
            EventHandler<RecognizeCompletedEventArgs> recognizeCompletedHandler = RecognizeCompletedSync;
            if (recognizeCompletedHandler == null)
            {
                // If not in sync mode, fire the public event.
                recognizeCompletedHandler = RecognizeCompleted;
            }

            // Fire the completed event
            if (recognizeCompletedHandler != null)
            {
                _asyncWorkerUI.PostOperation(recognizeCompletedHandler, this, new RecognizeCompletedEventArgs(result, initialSilenceTimeoutReached, babbleTimeoutReached,
                    isStreamReleased, audioPosition, exception, isRecognizeCancelled, null));
            }
        }

        /// <summary>
        /// Fire the emulate completed event.
        /// </summary>
        private void FireEmulateRecognizeCompletedEvent(RecognitionResult result, Exception exception, bool isRecognizeCancelled)
        {
            EventHandler<EmulateRecognizeCompletedEventArgs> emulateRecognizeCompletedHandler;
            lock (SapiRecognizer)
            {
                // In the synchronous case, fire the private event
                emulateRecognizeCompletedHandler = EmulateRecognizeCompletedSync;
                if (emulateRecognizeCompletedHandler == null)
                {
                    // If not in sync mode, fire the public event.
                    emulateRecognizeCompletedHandler = EmulateRecognizeCompleted;
                }
                _lastResult = null;
                _lastException = null;
                _isEmulateRecognition = false;
                _isRecognizing = false;

                _isWaitingForRecognition = false;
            }

            if (emulateRecognizeCompletedHandler != null)
            {
                _asyncWorkerUI.PostOperation(emulateRecognizeCompletedHandler, this, new EmulateRecognizeCompletedEventArgs(result, exception, isRecognizeCancelled, null));
            }
        }

        private static void CheckGrammarOptionsOnSapi51(Grammar grammar)
        {
            SRID messageId = (SRID)(-1);
            if (grammar.BaseUri != null && !grammar.IsSrgsDocument)
            {
                messageId = SRID.NotSupportedWithThisVersionOfSAPIBaseUri;
            }
            else if (grammar.IsStg || grammar.Sapi53Only)
            {
                messageId = SRID.NotSupportedWithThisVersionOfSAPITagFormat;
            }
            if (messageId != (SRID)(-1))
            {
                throw new NotSupportedException(SR.Get(messageId));
            }
        }

        #endregion

        #region Private Fields

        private List<Grammar> _grammars;
        private ReadOnlyCollection<Grammar> _readOnlyGrammars;

        private RecognizerInfo _recognizerInfo;
        private bool _disposed;

        // Internal Id incremented and passed to SAPI each time a grammar is created
        private ulong _currentGrammarId;

        // Associated sapi interfaces
        private SapiRecoContext _sapiContext;
        private SapiRecognizer _sapiRecognizer;
        private bool _supportsSapi53;

        private EventNotify _eventNotify;
        private ulong _eventInterest;

        private EventHandler<AudioSignalProblemOccurredEventArgs> _audioSignalProblemOccurredDelegate;
        private EventHandler<AudioLevelUpdatedEventArgs> _audioLevelUpdatedDelegate;
        private EventHandler<AudioStateChangedEventArgs> _audioStateChangedDelegate;
        private EventHandler<SpeechHypothesizedEventArgs> _speechHypothesizedDelegate;

        private bool _enabled = true; // Used by SpeechRecognizer to globally deactivate grammars.

        private int _maxAlternates;
        internal AudioState _audioState;
        private SpeechAudioFormatInfo _audioFormat;

        private RecognizeMode _recognizeMode = RecognizeMode.Multiple; // Default for SpeechRecognizer, set explicitly on SpeechRecognitionEngine
        private bool _isRecognizeCancelled;
        private bool _isRecognizing;
        private bool _isEmulateRecognition; // The end of stream event is not fire on error for emulate recognition in SAPI 5.1
        private bool _isWaitingForRecognition;

        private RecognitionResult _lastResult; // Temporarily store last result but always set to null once recognition completes
        private Exception _lastException; // Temporarily store last exception but always set to null once recognition completes

        // Means that the recognizer will be paused after each recognition while the SpeechRecognized event is firing.
        // This is always on for the SpeechRecognitionEngine but off by default for the SpeechRecognizer.
        private bool _pauseRecognizerOnRecognition = true;

        private bool _detectingInitialSilenceTimeout;
        private bool _detectingBabbleTimeout;
        private bool _initialSilenceTimeoutReached;
        private bool _babbleTimeoutReached;
        private TimeSpan _initialSilenceTimeout;
        private TimeSpan _babbleTimeout;

        internal bool _haveInputSource; // Tracks if there's an input stream set or not - only used on SpeechRecognitionEngine.
        private Stream _inputStream;    // track the input stream open if it has been opened by this object

        // Dictionary used to map between sapi bookmark ids and RequestRecognizerUpdate userToken values.
        private Dictionary<int, object> _bookmarkTable = new();
        private uint _nextBookmarkId = _firstUnusedBookmarkId;
        private uint _prevMaxBookmarkId = _firstUnusedBookmarkId - 1;

        // Lock used to wait for all pending async grammar loads to complete before starting recognition.
        private OperationLock _waitForGrammarsToLoad = new();
        // Lock used to protect properties on the Grammar {Enabled, Weight etc.} from being changed while an async grammar load is in progress.
        private object _grammarDataLock = new();

        // Preset bookmark values.
        private const uint _nullBookmarkId = 0;
        private const uint _initialSilenceBookmarkId = _nullBookmarkId + 1; // 1
        private const uint _babbleBookmarkId = _initialSilenceBookmarkId + 1; // 2
        private const uint _firstUnusedBookmarkId = _babbleBookmarkId + 1; // 3

        private AsyncSerializedWorker _asyncWorker, _asyncWorkerUI;
        private AutoResetEvent _handlerWaitHandle = new(false);

        private object _thisObjectLock = new();

        private Exception _loadException;
        private Grammar _topLevel;

        private bool _inproc;

        // private event used to hook up the SpeechRecognitionEngine RecognizeCompleted event.
        private event EventHandler<RecognizeCompletedEventArgs> RecognizeCompletedSync;
        private event EventHandler<EmulateRecognizeCompletedEventArgs> EmulateRecognizeCompletedSync;

        private TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        private RecognizerBaseThunk _recoThunk;
        #endregion

        private sealed class RecognizerBaseThunk : ISpGrammarResourceLoader
        {
            internal RecognizerBaseThunk(RecognizerBase recognizer)
            {
                _recognizerRef = new WeakReference(recognizer);
            }

            internal RecognizerBase Recognizer
            {
                get
                {
                    return (RecognizerBase)_recognizerRef.Target;
                }
            }

            /// <summary>
            /// Called to load a grammar and all of its dependent rule refs.
            ///
            /// Returns the CFG data for a given file and builds a tree of rule ref dependencies.
            /// </summary>
            int ISpGrammarResourceLoader.LoadResource(string bstrResourceUri, bool fAlwaysReload, out IStream pStream, ref string pbstrMIMEType, ref short pfModified, ref string pbstrRedirectUrl)
            {
                return ((ISpGrammarResourceLoader)Recognizer).LoadResource(bstrResourceUri, fAlwaysReload, out pStream, ref pbstrMIMEType, ref pfModified, ref pbstrRedirectUrl);
            }

            /// <summary>
            /// Unused
            /// </summary>
            string ISpGrammarResourceLoader.GetLocalCopy(Uri resourcePath, out string mimeType, out Uri redirectUrl)
            {
                return ((ISpGrammarResourceLoader)Recognizer).GetLocalCopy(resourcePath, out mimeType, out redirectUrl);
            }

            /// <summary>
            /// Unused
            /// </summary>
            void ISpGrammarResourceLoader.ReleaseLocalCopy(string path)
            {
                ((ISpGrammarResourceLoader)Recognizer).ReleaseLocalCopy(path);
            }

            private WeakReference _recognizerRef;
        }
    }

    // Internal class used to encapsulate all the additional data the RecognizerBase needs about a Grammar.
    // This is stored in the Grammar.InternalData property.
    internal class InternalGrammarData
    {
        #region Constructors

        // Keep a copy of enabled, weight and priority because there's a race condition between reading the values from the Grammar
        // to initially call SetSapiGrammarProperties and an app setting a property on the Grammar at the same time.
        // Thus these copied values are taken under a lock and used to update sapi.
        // This is to avoid having a lock which spans both the Grammar and Recognizer which would be awkward.
        internal InternalGrammarData(ulong grammarId, SapiGrammar sapiGrammar, bool enabled, float weight, int priority)
        {
            _grammarId = grammarId;
            _sapiGrammar = sapiGrammar;
            _grammarEnabled = enabled;
            _grammarWeight = weight;
            _grammarPriority = priority;
        }

        #endregion

        #region Internal Fields

        internal ulong _grammarId; // Id passed to SAPI's CreateGrammar call and returned in result.
        internal SapiGrammar _sapiGrammar;
        internal bool _grammarEnabled;
        internal float _grammarWeight;
        internal int _grammarPriority;

        #endregion
    }

    // Simple class that keeps track of multiple threads performing an operation, and then allows another thread
    // to wait until all operations have completed. This is similar in concept to a ReaderWriterLock, except
    // in the ReaderWriterLock all Acquire/Releases must be on the same threads, where here StartOperation and FinishOperation
    // can be on different threads.
    // This is used in async grammar loading - all LoadGrammarAsync starts an activity, and then later they finished
    // (on a different thread). WaitForOperationsToFinish is called by RecognizeAsync to wait for all loads to finish
    // before starting recognition.
    internal class OperationLock : IDisposable
    {
        public void Dispose()
        {
            _event.Close();
            GC.SuppressFinalize(this);
        }

        internal void StartOperation()
        {
            lock (_thisObjectLock) // Not a publicly exposed class so okay to lock.
            {
                if (_operationCount == 0)
                {
                    _event.Reset(); // Activities in progress so start blocking the WaitForActivitiesToFinish method.
                }
                _operationCount++;
            }
        }

        internal void FinishOperation()
        {
            lock (_thisObjectLock)
            {
                _operationCount--;
                if (_operationCount == 0)
                {
                    _event.Set(); // No more activities in progress so signal event.
                }
            }
        }

        internal void WaitForOperationsToFinish()
        {
            _event.WaitOne();
        }

        private ManualResetEvent _event = new(true); // In signaled state so initially do not block
        private uint _operationCount;
        private object _thisObjectLock = new();
    }

    #region Interface

    [ComImport, Guid("2D3D3845-39AF-4850-BBF9-40B49780011D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpObjectTokenCategory : ISpDataKey
    {
        // ISpDataKey Methods
        [PreserveSig]
        new int SetData([MarshalAs(UnmanagedType.LPWStr)] string valueName, uint cbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data);
        [PreserveSig]
        new int GetData([MarshalAs(UnmanagedType.LPWStr)] string valueName, ref uint pcbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] byte[] data);
        [PreserveSig]
        new int SetStringValue([MarshalAs(UnmanagedType.LPWStr)] string valueName, [MarshalAs(UnmanagedType.LPWStr)] string value);
        [PreserveSig]
        new void GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszValue);
        [PreserveSig]
        new int SetDWORD([MarshalAs(UnmanagedType.LPWStr)] string valueName, uint dwValue);
        [PreserveSig]
        new int GetDWORD([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, ref uint pdwValue);
        [PreserveSig]
        new int OpenKey([MarshalAs(UnmanagedType.LPWStr)] string pszSubKeyName, out ISpDataKey ppSubKey);
        [PreserveSig]
        new int CreateKey([MarshalAs(UnmanagedType.LPWStr)] string subKey, out ISpDataKey ppSubKey);
        [PreserveSig]
        new int DeleteKey([MarshalAs(UnmanagedType.LPWStr)] string subKey);
        [PreserveSig]
        new int DeleteValue([MarshalAs(UnmanagedType.LPWStr)] string valueName);
        [PreserveSig]
        new int EnumKeys(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszSubKeyName);
        [PreserveSig]
        new int EnumValues(uint Index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszValueName);

        // ISpObjectTokenCategory Methods
        void SetId([MarshalAs(UnmanagedType.LPWStr)] string pszCategoryId, [MarshalAs(UnmanagedType.Bool)] bool fCreateIfNotExist);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemCategoryId);
        void Slot14(); // void GetDataKey(System.Speech.Internal.SPDATAKEYLOCATION spdkl, out ISpDataKey ppDataKey);
        void EnumTokens([MarshalAs(UnmanagedType.LPWStr)] string pzsReqAttribs, [MarshalAs(UnmanagedType.LPWStr)] string pszOptAttribs, out IEnumSpObjectTokens ppEnum);
        void Slot16(); // void SetDefaultTokenId([MarshalAs(UnmanagedType.LPWStr)] string pszTokenId);
        void GetDefaultTokenId([MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemTokenId);
    }

    [ComImport, Guid("06B64F9E-7FDA-11D2-B4F2-00C04F797396"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEnumSpObjectTokens
    {
        void Slot1(); // void Next(UInt32 celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] ISpObjectToken[] pelt, out UInt32 pceltFetched);
        void Slot2(); // void Skip(UInt32 celt);
        void Slot3(); // void Reset();
        void Slot4(); // void Clone(out IEnumSpObjectTokens ppEnum);
        void Item(uint Index, out ISpObjectToken ppToken);
        void GetCount(out uint pCount);
    }

    [ComImport, Guid("EF411752-3736-4CB4-9C8C-8EF4CCB58EFE")]
    internal class SpObjectToken { }

    [ComImport, Guid("A910187F-0C7A-45AC-92CC-59EDAFB77B53")]
    internal class SpObjectTokenCategory { }

    #endregion
}
