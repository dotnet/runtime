// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QuerySettings.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel
{
    /// <summary>
    /// This type contains query execution options specified by the user.
    /// QuerySettings are used as follows:
    /// - in the query construction phase, some settings may be uninitialized.
    /// - at the start of the query opening phase, the WithDefaults method
    ///   is used to initialize all uninitialized settings.
    /// - in the rest of the query opening phase, we assume that all settings
    ///   have been initialized.
    /// </summary>
    internal struct QuerySettings
    {
        private TaskScheduler? _taskScheduler;
        private int? _degreeOfParallelism;
        private CancellationState _cancellationState;
        private ParallelExecutionMode? _executionMode;
        private ParallelMergeOptions? _mergeOptions;
        private int _queryId;

        internal CancellationState CancellationState
        {
            get { return _cancellationState; }
            set
            {
                _cancellationState = value;
                Debug.Assert(_cancellationState != null);
            }
        }

        // The task manager on which to execute the query.
        internal TaskScheduler? TaskScheduler
        {
            get { return _taskScheduler; }
            set { _taskScheduler = value; }
        }

        // The number of parallel tasks to utilize.
        internal int? DegreeOfParallelism
        {
            get { return _degreeOfParallelism; }
            set { _degreeOfParallelism = value; }
        }

        // The mode in which to execute this query.
        internal ParallelExecutionMode? ExecutionMode
        {
            get { return _executionMode; }
            set { _executionMode = value; }
        }

        internal ParallelMergeOptions? MergeOptions
        {
            get { return _mergeOptions; }
            set { _mergeOptions = value; }
        }

        internal int QueryId
        {
            get
            {
                return _queryId;
            }
        }

        //-----------------------------------------------------------------------------------
        // Constructs a new settings structure.
        //
        internal QuerySettings(TaskScheduler? taskScheduler, int? degreeOfParallelism,
            CancellationToken externalCancellationToken, ParallelExecutionMode? executionMode,
            ParallelMergeOptions? mergeOptions)
        {
            _taskScheduler = taskScheduler;
            _degreeOfParallelism = degreeOfParallelism;
            _cancellationState = new CancellationState(externalCancellationToken);
            _executionMode = executionMode;
            _mergeOptions = mergeOptions;
            _queryId = -1;

            Debug.Assert(_cancellationState != null);
        }

        //-----------------------------------------------------------------------------------
        // Combines two sets of options.
        //
        internal QuerySettings Merge(QuerySettings settings2)
        {
            if (this.TaskScheduler != null && settings2.TaskScheduler != null)
            {
                throw new InvalidOperationException(SR.ParallelQuery_DuplicateTaskScheduler);
            }

            if (this.DegreeOfParallelism != null && settings2.DegreeOfParallelism != null)
            {
                throw new InvalidOperationException(SR.ParallelQuery_DuplicateDOP);
            }

            if (this.CancellationState.ExternalCancellationToken.CanBeCanceled && settings2.CancellationState.ExternalCancellationToken.CanBeCanceled)
            {
                throw new InvalidOperationException(SR.ParallelQuery_DuplicateWithCancellation);
            }

            if (this.ExecutionMode != null && settings2.ExecutionMode != null)
            {
                throw new InvalidOperationException(SR.ParallelQuery_DuplicateExecutionMode);
            }

            if (this.MergeOptions != null && settings2.MergeOptions != null)
            {
                throw new InvalidOperationException(SR.ParallelQuery_DuplicateMergeOptions);
            }

            TaskScheduler? tm = this.TaskScheduler ?? settings2.TaskScheduler;
            int? dop = this.DegreeOfParallelism.HasValue ? this.DegreeOfParallelism : settings2.DegreeOfParallelism;
            CancellationToken externalCancellationToken = (this.CancellationState.ExternalCancellationToken.CanBeCanceled) ? this.CancellationState.ExternalCancellationToken : settings2.CancellationState.ExternalCancellationToken;
            ParallelExecutionMode? executionMode = this.ExecutionMode.HasValue ? this.ExecutionMode : settings2.ExecutionMode;
            ParallelMergeOptions? mergeOptions = this.MergeOptions.HasValue ? this.MergeOptions : settings2.MergeOptions;

            return new QuerySettings(tm, dop, externalCancellationToken, executionMode, mergeOptions);
        }

        internal QuerySettings WithPerExecutionSettings()
        {
            return WithPerExecutionSettings(new CancellationTokenSource(), new Shared<bool>(false));
        }

        internal QuerySettings WithPerExecutionSettings(CancellationTokenSource topLevelCancellationTokenSource, Shared<bool> topLevelDisposedFlag)
        {
            //Initialize a new QuerySettings structure and copy in the current settings.
            //Note: this has the very important effect of newing a fresh CancellationSettings,
            //      and _not_ copying in the current internalCancellationSource or topLevelDisposedFlag which should not be
            //      propagated to internal query executions. (This affects SelectMany execution)
            //      The fresh toplevel parameters are used instead.
            QuerySettings settings = new QuerySettings(TaskScheduler, DegreeOfParallelism, CancellationState.ExternalCancellationToken, ExecutionMode, MergeOptions);

            Debug.Assert(topLevelCancellationTokenSource != null, "There should always be a top-level cancellation signal specified.");
            settings.CancellationState.InternalCancellationTokenSource = topLevelCancellationTokenSource;

            //Merge internal and external tokens to form the combined token
            settings.CancellationState.MergedCancellationTokenSource =
                   CancellationTokenSource.CreateLinkedTokenSource(settings.CancellationState.InternalCancellationTokenSource.Token, settings.CancellationState.ExternalCancellationToken);

            // and copy in the topLevelDisposedFlag
            settings.CancellationState.TopLevelDisposedFlag = topLevelDisposedFlag;

            Debug.Assert(settings.CancellationState.InternalCancellationTokenSource != null);
            Debug.Assert(settings.CancellationState.MergedCancellationToken.CanBeCanceled);
            Debug.Assert(settings.CancellationState.TopLevelDisposedFlag != null);

            // Finally, assign a query Id to the settings
            settings._queryId = PlinqEtwProvider.NextQueryId();

            return settings;
        }

        //-----------------------------------------------------------------------------------
        // Copies the settings, replacing unspecified settings with defaults.
        //
        internal QuerySettings WithDefaults()
        {
            QuerySettings settings = this;

            settings.TaskScheduler ??= TaskScheduler.Default;
            settings.DegreeOfParallelism ??= Scheduling.GetDefaultDegreeOfParallelism();
            settings.ExecutionMode ??= ParallelExecutionMode.Default;
            settings.MergeOptions ??= ParallelMergeOptions.Default;
            if (settings.MergeOptions == ParallelMergeOptions.Default)
            {
                settings.MergeOptions = ParallelMergeOptions.AutoBuffered;
            }

            Debug.Assert(settings.TaskScheduler != null);
            Debug.Assert(settings.DegreeOfParallelism.HasValue);
            Debug.Assert(settings.DegreeOfParallelism.Value >= 1 && settings.DegreeOfParallelism <= Scheduling.MAX_SUPPORTED_DOP);
            Debug.Assert(settings.ExecutionMode != null);
            Debug.Assert(settings.MergeOptions != null);

            Debug.Assert(settings.MergeOptions != ParallelMergeOptions.Default);

            return settings;
        }

        // Returns the default settings
        internal static QuerySettings Empty
        {
            get { return new QuerySettings(null, null, CancellationToken.None, null, null); }
        }

        // Cleanup internal state once the entire query is complete.
        // (this should not be performed after a 'premature-query' completes as the state should live
        // uninterrupted for the duration of the full query.)
        public void CleanStateAtQueryEnd()
        {
            Debug.Assert(_cancellationState.MergedCancellationTokenSource != null);
            _cancellationState.MergedCancellationTokenSource.Dispose();
        }
    }
}
