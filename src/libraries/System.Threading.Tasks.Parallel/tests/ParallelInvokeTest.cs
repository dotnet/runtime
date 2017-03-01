// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// This file contains functional tests for Parallel.Invoke
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=--=-=-=-=-=-=-=-=-=-=-=-

using Xunit;

namespace System.Threading.Tasks.Tests
{
    public sealed class ParallelInvokeTest
    {
        #region Member Variables

        private const int SEED = 1000; // minimum seed to be passed into ZetaSequence workload

        private int _count; // no of actions
        private ActionType _actionType; // type of actions

        private Action[] _actions;
        private double[] _results;  // global place to store the workload results for verification

        #endregion

        public ParallelInvokeTest(ParallelInvokeTestParameters parameters)
        {
            _count = parameters.Count;
            _actionType = parameters.ActionType;

            _actions = new Action[_count];
            _results = new double[_count];

            // initialize actions 
            for (int i = 0; i < _count; i++)
            {
                int iCopy = i;
                if (_actionType == ActionType.Empty)
                {
                    _actions[i] = new Action(delegate { });
                }
                else if (_actionType == ActionType.EqualWorkload)
                {
                    _actions[i] = new Action(delegate
                    {
                        _results[iCopy] = ZetaSequence(SEED);
                    });
                }
                else
                {
                    _actions[i] = new Action(delegate
                    {
                        _results[iCopy] = ZetaSequence((iCopy + 1) * SEED);
                    });
                }
            }
        }

        /// <summary>
        /// Actual Test.
        /// Call Parallel.Invoke with actions using different workloads
        /// 
        /// Expected: Each action was invoked and returned the expected result
        /// </summary>
        /// <returns></returns>
        public void RealRun()
        {
            Parallel.Invoke(_actions);
            // verify result

            //Function point comparison cant be done by rounding off to nearest decimal points since
            //1.64 could be represented as 1.63999999 or as 1.6499999999. To perform floating point comparisons, 
            //a range has to be defined and check to ensure that the result obtained is within the specified range
            double minLimit = 1.63;
            double maxLimit = 1.65;

            foreach (double r in _results)
            {
                //If action is empty we are expected zero as result
                Assert.False(_actionType == ActionType.Empty && r != 0, String.Format("Differ in results. Expected result to be Zero but got {0}", r));

                Assert.False(_actionType != ActionType.Empty && (r < minLimit || r > maxLimit), String.Format("Differ in results. Expected result to lie between {0} and {1} but got {2}", minLimit, maxLimit, r));
            }
        }

        #region Helper Methods

        // calculate 1 + 1/(2*2) + 1/(3*3) + ... +  1/(n*n) = Math.Pow (Math.PI, 2) / 6
        private static double ZetaSequence(int n)
        {
            double result = 0;
            for (int i = 1; i < n; i++)
            {
                result += 1.0 / ((double)i * (double)i);
            }

            return result;
        }

        #endregion
    }

    public enum ActionType
    {
        Empty,
        EqualWorkload,
        DifferentWorkload,
    }

    public class ParallelInvokeTestParameters
    {
        public ParallelInvokeTestParameters()
        {
            Count = 0;
            ActionType = ActionType.Empty;
        }

        public int Count;
        public ActionType ActionType;
    }

     #region Test Methods
    public static class TestMethods
    {
   
        [Fact]
        public static void ParallelInvoke0()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 0,
                ActionType = ActionType.DifferentWorkload,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke1()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 1,
                ActionType = ActionType.DifferentWorkload,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke2()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 10,
                ActionType = ActionType.DifferentWorkload,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke3()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 97,
                ActionType = ActionType.DifferentWorkload,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke4()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 0,
                ActionType = ActionType.Empty,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke5()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 1,
                ActionType = ActionType.Empty,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke6()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 10,
                ActionType = ActionType.Empty,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke7()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 97,
                ActionType = ActionType.Empty,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke8()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 0,
                ActionType = ActionType.EqualWorkload,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke9()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 1,
                ActionType = ActionType.EqualWorkload,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke10()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 10,
                ActionType = ActionType.EqualWorkload,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

        [Fact]
        public static void ParallelInvoke11()
        {
            ParallelInvokeTestParameters parameters = new ParallelInvokeTestParameters
            {
                Count = 97,
                ActionType = ActionType.EqualWorkload,
            };
            ParallelInvokeTest test = new ParallelInvokeTest(parameters);
            test.RealRun();
        }

    }

#endregion
}
