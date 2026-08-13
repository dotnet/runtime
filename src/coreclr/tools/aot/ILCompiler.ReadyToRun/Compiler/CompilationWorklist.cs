// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler
{
    internal delegate void CompilationWorkerAction<TWorkerState>(
        DependencyNodeCore<NodeFactory> item,
        ref TWorkerState workerState);

    internal sealed class CompilationWorklist<TWorkerState> : IDisposable
    {
        private sealed class Worker
        {
            public readonly SemaphoreSlim Start = new(0);
            public readonly Thread Thread;
            public TWorkerState State;
            private readonly CompilationWorklist<TWorkerState> _worklist;

            public Worker(CompilationWorklist<TWorkerState> worklist)
            {
                _worklist = worklist;
                Thread = new Thread(Run);
            }

            private void Run() => _worklist.RunWorkerLoop(this);
        }

        private readonly ManualResetEventSlim _complete = new();
        private readonly int _parallelism;
        private readonly CompilationWorkerAction<TWorkerState> _processItem;
        private TWorkerState _mainWorkerState;
        private IReadOnlyList<DependencyNodeCore<NodeFactory>> _items;
        private ExceptionDispatchInfo _exception;
        private Worker[] _workers;
        private int _nextIndex;
        private int _startedWorkerCount;
        private int _workersRemaining;
        private volatile bool _stopping;

        public CompilationWorklist(int parallelism, CompilationWorkerAction<TWorkerState> processItem)
        {
            _parallelism = parallelism;
            _processItem = processItem;
        }

        public void Run(IReadOnlyList<DependencyNodeCore<NodeFactory>> items)
        {
            EnsureWorkers();
            Debug.Assert(_items is null);

            _items = items;
            _exception = null;
            _nextIndex = -1;
            _workersRemaining = _parallelism;
            _complete.Reset();

            foreach (Worker worker in _workers)
            {
                worker.Start.Release();
            }

            try
            {
                ProcessItems(ref _mainWorkerState);
            }
            finally
            {
                WaitForCompletion()?.Throw();
            }
        }

        public void Dispose()
        {
            if (_stopping)
            {
                return;
            }

            _stopping = true;

            if (_workers is not null)
            {
                for (int i = 0; i < _startedWorkerCount; i++)
                {
                    _workers[i].Start.Release();
                }

                for (int i = 0; i < _startedWorkerCount; i++)
                {
                    Worker worker = _workers[i];
                    worker.Thread.Join();
                    worker.Start.Dispose();
                    worker.State = default;
                }
            }

            _mainWorkerState = default;
            _complete.Dispose();
        }

        private void EnsureWorkers()
        {
            if (_workers is not null)
            {
                return;
            }

            _workers = new Worker[_parallelism - 1];
            for (int i = 0; i < _workers.Length; i++)
            {
                Worker worker = new(this);
                _workers[i] = worker;
                worker.Thread.Start();
                _startedWorkerCount++;
            }
        }

        private void RunWorkerLoop(Worker worker)
        {
            while (true)
            {
                worker.Start.Wait();
                if (_stopping)
                {
                    return;
                }

                ProcessItems(ref worker.State);
            }
        }

        private void ProcessItems(ref TWorkerState workerState)
        {
            try
            {
                while (TryTake(out DependencyNodeCore<NodeFactory> item))
                {
                    _processItem(item, ref workerState);
                }
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    _exception ??= ExceptionDispatchInfo.Capture(ex);
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref _workersRemaining) == 0)
                {
                    _complete.Set();
                }
            }
        }

        private bool TryTake(out DependencyNodeCore<NodeFactory> item)
        {
            lock (this)
            {
                if (_exception is not null)
                {
                    item = null;
                    return false;
                }

                int index = ++_nextIndex;
                IReadOnlyList<DependencyNodeCore<NodeFactory>> items = _items;
                if ((uint)index >= (uint)items.Count)
                {
                    item = null;
                    return false;
                }

                item = items[index];
                return true;
            }
        }

        private ExceptionDispatchInfo WaitForCompletion()
        {
            _complete.Wait();
            _items = null;
            ExceptionDispatchInfo exception = _exception;
            _exception = null;
            return exception;
        }
    }
}
