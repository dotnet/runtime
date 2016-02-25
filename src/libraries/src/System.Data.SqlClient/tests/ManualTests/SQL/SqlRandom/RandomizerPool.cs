// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    /// <summary>
    /// Enables test app to run in dual mode: regular and repro. Typical usage pattern is:
    /// 
    /// RandomizerPool pool = new RandomizerPool(ReproFile); // for repro
    /// RandomizerPool pool = new RandomizerPool(seed); // for deterministic generation with constant seed (useful for unit tests or perf tests)
    /// RandomizerPool pool = new RandomizerPool(); // for generation with random seeds (for stress tests)
    /// 
    /// try
    /// {
    ///     for(outer test loop counter)
    ///     {
    ///         using (Scope rootScope = pool.RootScope())
    ///         {
    ///             GlobalSetup(rootScope.Current);
    ///             
    ///             if pool.ReproMode, set loop counter to 1
    ///             foreach (inner test loop counter)
    ///             {
    ///                 using (Scope localScope = rootScope.NewScope())
    ///                 {
    ///                     RunLocalSetup(localScope.Current);
    ///
    ///                     foreach (inner test loop counter)
    ///                     {
    ///                         using (Scope testCaseScope = localScope.NewScope())
    ///                         {
    ///                             RunIteration(testCaseScope.Current);
    ///                         }
    ///                     }
    ///                 }
    ///             }
    ///         }
    ///     }
    /// }
    /// catch(Exception)
    /// {
    ///     string tempFile = Path.GetRandomFileName() + ".repro"; // or any other place to place repro files
    ///     // dumps the latest scope created on the current thread to the file
    ///     pool.SaveLastThreadScopeRepro(tempFile)
    /// }
    /// </summary>
    public sealed class RandomizerPool
    {
        /// <summary>
        /// holds repro states captured from repro file or null in case of regular test run
        /// </summary>
        private readonly Randomizer.State[] _reproStates;

        /// <summary>
        /// this seed is used create root scope randomizer objects, it increments every use
        /// </summary>
        /// <remarks>use Interlocked methods to access this instance to ensure multi-threading safety</remarks>
        private int _rootScopeNextSeed;

        /// <summary>
        /// indication whether to use random seeds for root scopes or fixed ones
        /// </summary>
        private readonly bool _rootScopeNextSeedUsed;

        /// <summary>
        /// generates next root scope seed
        /// </summary>
        private int NextRootSeed()
        {
            // at least one repro state must present in repro file
            // thus, root scopes will be generated the first state, and not with seed
            if (_reproStates != null)
                throw new InvalidOperationException("");

            if (_rootScopeNextSeedUsed)
                return Interlocked.Increment(ref _rootScopeNextSeed);
            else
                return Randomizer.CreateSeed();
        }

        /// <summary>
        /// creates randomizer pool with random seeds, useful for stress testing
        /// </summary>
        public RandomizerPool()
        {
            _reproStates = null;
            _rootScopeNextSeedUsed = false;
        }

        /// <summary>
        /// creates randomizer pool with fixed master seed, use it to make the test run deterministic
        /// </summary>
        public RandomizerPool(int masterSeed)
        {
            _reproStates = null;
            _rootScopeNextSeed = masterSeed;
            _rootScopeNextSeedUsed = true;
        }

        /// <summary>
        /// create randomizer pool from repro file
        /// </summary>
        public RandomizerPool(string reproFile)
        {
            if (string.IsNullOrEmpty(reproFile))
                throw new ArgumentNullException("Invalid repro file");

            _rootScopeNextSeedUsed = false;

            using (StreamReader reproStream = new StreamReader(new FileStream(reproFile, FileMode.Open)))
            {
                _reproStates = LoadFromStream(reproStream);
            }
        }


        /// <summary>
        /// helper method to load repro states from stream
        /// </summary>
        private static Randomizer.State[] LoadFromStream(StreamReader reproStream)
        {
            var reproStack = new List<Randomizer.State>();

            string reproStateStr = reproStream.ReadLine();
            do
            {
                Randomizer.State state = Randomizer.State.Parse(reproStateStr);
                reproStack.Add(state);
                reproStateStr = reproStream.ReadLine();
            } while (reproStateStr != null);

            return reproStack.ToArray();
        }

        /// <summary>
        /// Indicator whether the pool is running in repro mode.
        /// </summary>
        public bool ReproMode
        {
            get
            {
                return _reproStates != null;
            }
        }

        /// <summary>
        /// Create the root scope of the randomizer, this method is thread safe!
        /// Note that scopes themselves are NOT thread-safe, while the pool is.
        /// </summary>
        public Scope<RandomizerType> RootScope<RandomizerType>()
            where RandomizerType : Randomizer, new()
        {
            return new Scope<RandomizerType>(this, null);
        }

        /// <summary>
        /// helper method called from the scope c-tor to construct the new scope
        /// </summary>
        private void CreateScopeRandomizer<RandomizerType>(IScope parentScope, out Randomizer.State[] scopeStates, out RandomizerType current)
            where RandomizerType : Randomizer, new()
        {
            Randomizer.State[] parentStates = parentScope != null ? parentScope.GetStates() : null;
            int newLength = parentStates != null ? parentStates.Length + 1 : 1;

            scopeStates = new Randomizer.State[newLength];
            if (parentStates != null)
            {
                // clone the states from the parent scope first. Note that it creates shallow copy of the state only.
                // this is OK since Randomizer.State is immutable
                Array.Copy(parentStates, scopeStates, newLength - 1);
            }

            // select the randomizer and state for the new scope
            if (_reproStates != null && newLength <= _reproStates.Length)
            {
                // repro mode
                Randomizer.State reproState = _reproStates[newLength - 1];
                current = Randomizer.Create<RandomizerType>(reproState);
                scopeStates[newLength - 1] = reproState;
            }
            else
            {
                // either generation more or repro state did not capture this depth, thus create random randomizer
                // to make the scope generation deterministic, use the parent scope randomizer or the pool's one for roots
                int seed;
                if (parentScope != null)
                {
                    seed = parentScope.Current.NextSeed();
                }
                else
                {
                    seed = NextRootSeed();
                }

                current = Randomizer.Create<RandomizerType>(seed);
                scopeStates[newLength - 1] = current.GetCurrentState();
            }
        }

        /// <summary>
        /// for internal use only - used to store the scope in arguments / local variables
        /// </summary>
        internal interface IScope
        {
            Randomizer.State[] GetStates();
            Randomizer Current { get; }
        }

        /// <summary>
        /// holds the last scope created on the current thread. This scope is used to generate repro file when
        /// application crashes
        /// </summary>
        [ThreadStatic]
        private static IScope s_ts_lastCreatedScope;

        /// <summary>
        /// holds the current scope on the thread, it is used to ensure scope creation and disposal calls are balanced
        /// </summary>
        [ThreadStatic]
        private static IScope s_ts_currentScope;
        /// <summary>
        /// represents a randomizer scope, that makes use of Randomizer or derived types
        /// </summary>
        public class Scope<RandomizerType> : IScope, IDisposable
            where RandomizerType : Randomizer, new()
        {
            private readonly RandomizerPool _pool;
            private RandomizerType _current;
            internal Randomizer.State[] _scopeStates;
            private IScope _previousScope;

            public RandomizerType Current
            {
                get
                {
                    if (_current == null)
                        throw new ObjectDisposedException(GetType().FullName);
                    return _current;
                }
            }

            /// <summary>
            /// Clones the random states from parent scope and generates a new one for itself.
            /// Each time new scope is created, it is set as ts_lastCreatedScope on the current thread.
            /// </summary>
            internal Scope(RandomizerPool pool, IScope parent)
            {
                s_ts_lastCreatedScope = this;
                _previousScope = s_ts_currentScope;
                s_ts_currentScope = this;
                _pool = pool;

                RandomizerType current;
                _pool.CreateScopeRandomizer(parent, out _scopeStates, out current);

                // mark the scope as constructed
                _current = current;
            }

            Randomizer.State[] IScope.GetStates()
            {
                return _scopeStates;
            }

            Randomizer IScope.Current { get { return Current; } }

            /// <summary>
            /// Disposes the scope and reverts the current thread scope to previos one. 
            /// Note that the "last created scope" is not not changed on Dispose, thus the scope instance
            /// itself can still be used to collect repro states.
            /// </summary>
            public void Dispose()
            {
                if (_current != null)
                {
                    _current = null;

                    if (s_ts_currentScope != this)
                    {
                        // every creation of scope in test must be balanced with Dispose call, use 'using' to enforce that!
                        // nested scopes are allowed, child scope must be disposed before the parent one
                        throw new InvalidOperationException("Unbalanced call to scope.Dispose");
                    }

                    s_ts_currentScope = _previousScope;
                }
            }

            /// <summary>
            /// creates a new scope with Randomizer type or derived
            /// </summary>
            /// <returns></returns>
            public Scope<NestedRandomizerType> NewScope<NestedRandomizerType>()
                where NestedRandomizerType : Randomizer, new()
            {
                IScope newScope;

                if (typeof(NestedRandomizerType) == typeof(Randomizer))
                    newScope = new Scope(_pool, this); // to ensure later casting works fine
                else
                    newScope = new Scope<NestedRandomizerType>(_pool, this);

                return (Scope<NestedRandomizerType>)newScope;
            }

            /// <summary>
            /// shortcut to create Scope with Randomizer type
            /// </summary>
            /// <returns></returns>
            public Scope NewScope()
            {
                return new Scope(_pool, this);
            }
        }

        /// <summary>
        /// wrapping class for Scope that creates random instances using the Randomizer type itself
        /// </summary>
        public class Scope : Scope<Randomizer>
        {
            internal Scope(RandomizerPool pool, IScope parent)
                : base(pool, parent)
            { }
        }

        /// <summary>
        /// helper method to dump scope information to the file
        /// </summary>
        private void SaveReproInternal(string reproFile, IScope scope)
        {
            Randomizer.State[] states = scope.GetStates();
            using (StreamWriter reproStream = new StreamWriter(new FileStream(reproFile, FileMode.OpenOrCreate)))
            {
                for (int i = 0; i < states.Length; i++)
                {
                    // dump the scope as string
                    reproStream.WriteLine(states[i].ToString());
                }
            }
        }

        /// <summary>
        /// saves the repro data for specific scope
        /// </summary>
        public void SaveRepro<RandomizerType>(string reproFile, Scope<RandomizerType> scope)
            where RandomizerType : Randomizer, new()
        {
            SaveReproInternal(reproFile, scope);
        }

        /// <summary>
        /// save the repro data for the last scope created on the current thread.
        /// </summary>
        public void SaveLastThreadScopeRepro(string reproFile)
        {
            SaveReproInternal(reproFile, s_ts_lastCreatedScope);
        }
    }
}
