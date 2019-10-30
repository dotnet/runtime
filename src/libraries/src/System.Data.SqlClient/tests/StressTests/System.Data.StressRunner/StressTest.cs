// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DPStressHarness
{
    internal class StressTest : TestBase
    {
        private StressTestAttribute _attr;
        private object _targetInstance;
        private TestMethodDelegate _tmd;

        // TODO: MethodInfo objects below can have associated delegates to improve
        // runtime performance.
        protected MethodInfo _globalSetupMethod;
        protected MethodInfo _globalCleanupMethod;

        public delegate void ExceptionHandler(Exception e);

        /// <summary>
        /// Cache the global exception handler method reference. It is
        /// recommended not to actually use this reference to call the
        /// method. Use the delegate instead.
        /// </summary>
        protected MethodInfo _globalExceptionHandlerMethod;

        /// <summary>
        /// Create a delegate to call global exception handler method.
        /// Use this delegate to call test assembly's exception handler.
        /// </summary>
        protected ExceptionHandler _globalExceptionHandlerDelegate;

        public StressTest(StressTestAttribute attr,
                    MethodInfo testMethodInfo,
                    MethodInfo globalSetupMethod,
                    MethodInfo globalCleanupMethod,
                    Type type,
                    List<MethodInfo> setupMethods,
                    List<MethodInfo> cleanupMethods,
                    MethodInfo globalExceptionHandlerMethod)
            : base(attr, testMethodInfo, type, setupMethods, cleanupMethods)
        {
            _attr = attr;
            _globalSetupMethod = globalSetupMethod;
            _globalCleanupMethod = globalCleanupMethod;
            _globalExceptionHandlerMethod = globalExceptionHandlerMethod;
        }

        public StressTest Clone()
        {
            StressTest t = new StressTest(_attr, this._testMethod, this._globalSetupMethod, this._globalCleanupMethod, this._type, this._setupMethods, this._cleanupMethods, this._globalExceptionHandlerMethod);
            return t;
        }

        private void InitTargetInstance()
        {
            _targetInstance = _type.GetConstructor(Type.EmptyTypes).Invoke(null);

            // Create a delegate for exception handling on _targetInstance
            if (_globalExceptionHandlerMethod != null)
            {
                _globalExceptionHandlerDelegate = (ExceptionHandler)_globalExceptionHandlerMethod.CreateDelegate(
                    typeof(ExceptionHandler),
                    _targetInstance
                    );
            }
        }

        /// <summary>
        /// Perform any global initialization for the test assembly. For example, make the connection to the database, load a workspace, etc.
        /// </summary>
        public void RunGlobalSetup()
        {
            if (null == _targetInstance)
            {
                InitTargetInstance();
            }

            if (null != _globalSetupMethod)
            {
                _globalSetupMethod.Invoke(_targetInstance, null);
            }
        }

        /// <summary>
        /// Run any per-thread setup needed
        /// </summary>
        public void RunSetup()
        {
            // create an instance of the class that defines the test method.
            if (null == _targetInstance)
            {
                InitTargetInstance();
            }
            _tmd = CreateTestMethodDelegate();

            // Set variation fields on the target instance
            SetVariations(_targetInstance);

            // Execute the setup phase for this thread.
            ExecuteSetupPhase(_targetInstance);
        }

        /// <summary>
        /// Execute the test method(s)
        /// </summary>
        public override void Run()
        {
            _tmd(_targetInstance);
        }

        /// <summary>
        /// Provide an opportunity to handle the exception
        /// </summary>
        /// <param name="e"></param>
        public void HandleException(Exception e)
        {
            if (null != _globalExceptionHandlerDelegate)
            {
                _globalExceptionHandlerDelegate(e);
            }
        }

        /// <summary>
        /// Run any per-thread cleanup for the test
        /// </summary>
        public void RunCleanup()
        {
            ExecuteCleanupPhase(_targetInstance);
        }

        /// <summary>
        /// Run final global cleanup for the test assembly. Could be used to release resources or for reporting, etc.
        /// </summary>
        public void RunGlobalCleanup()
        {
            if (null != _globalCleanupMethod)
            {
                _globalCleanupMethod.Invoke(_targetInstance, null);
            }
        }

        public int Weight
        {
            get { return _attr.Weight; }
        }
    }
}
