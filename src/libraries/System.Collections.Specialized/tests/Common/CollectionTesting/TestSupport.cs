// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace TestSupport
{
    /// <summary>
    /// Modifies the given collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection">The collection to modify</param>
    /// <param name="expectedItems">The current items in the collection</param>
    /// <returns>The items in the collection after it has been modified.</returns>
    public delegate T[] ModifyUnderlyingCollection_T<T>(System.Collections.Generic.IEnumerable<T> collection, T[] expectedItems);

    /// <summary>
    /// Modifies the given collection
    /// </summary>
    /// <param name="collection">The collection to modify</param>
    /// <param name="expectedItems">The current items in the collection</param>
    /// <returns>The items in the collection after it has been modified.</returns>
    public delegate Object[] ModifyUnderlyingCollection(IEnumerable collection, Object[] expectedItems);

    /// <summary>
    /// Creates a new ICollection
    /// </summary>
    /// <returns></returns>
    public delegate ICollection CreateNewICollection();

    /// <summary>
    /// Generates a new unique item
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>A new unique item.</returns>
    public delegate T GenerateItem<T>();

    /// <summary>
    /// Compares x and y
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>true if x and y are equal else false.</returns>
    public delegate bool ItemEquals_T<T>(T x, T y);

    /// <summary>
    /// Compares x and y
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>true if x and y are equal else false.</returns>
    public delegate bool ItemEquals(Object x, Object y);

    /// <summary>
    /// An action to perform on a Multidimentional array
    /// </summary>
    /// <param name="array">The array to perform the action on</param>
    /// <param name="indicies">The current indicies of the array</param>
    public delegate void MultiDimArrayAction(Array array, int[] indicies);

    public delegate void ModifyCollection();

    /// <summary>
    /// The expected range an enumerator will enumerate.
    /// </summary>
    [Flags]
    public enum ExpectedEnumeratorRange { None = 0, Start = 1, End = 2 };

    /// <summary>
    /// The methods to use for verification
    /// </summary>
    [Flags]
    public enum VerificationMethod { None = 0, Item = 1, Contains = 2, IndexOf = 4, ICollection = 8 };

    /// <summary>
    /// The verification level
    /// </summary>
    public enum VerificationLevel { None, Normal, Extensive };

    /// <summary>
    /// This specifies how the collection is ordered.
    /// Sequential specifies that Add places items at  the end of the collection and Remove will remove the first item found.
    /// Reverse specifies that Add places items at the begining of the collection and Remove will remove the first item found.
    /// Unspecified specifies that Add and Remove do not specify where items are added or removed.
    /// </summary>
    public enum CollectionOrder { Sequential, Unspecified };


    /// <summary>
    /// Geneartes an exception
    /// </summary>
    public delegate void ExceptionGenerator();

    /// <summary>
    /// Test Scenario to run
    /// </summary>
    /// <returns>true if the test scenario passed else false</returns>
    public delegate bool TestScenario();

    public class Test
    {
        /// <summary>
        /// The default exit code to use if the test failed
        /// </summary>
        public const int DEFAULT_FAIL_EXITCODE = 50;

        /// <summary>
        /// The default exit code to use if the test passed
        /// </summary>
        public const int PASS_EXITCODE = 100;

        private static int _numErrors = 0;

        private static int _numTestcaseFailures = 0;
        private static int _numTestcases = 0;

        private static int _failExitCode = DEFAULT_FAIL_EXITCODE;
        private static bool _failExitCodeSet = false;

        private static bool _suppressStackOutput = false;

        private static bool _outputExceptionMessages = false;

        private static List<Scenario> _scenarioDescriptions = new List<Scenario>();

        private static System.IO.TextWriter _outputWriter = Console.Out;

        private class Scenario
        {
            public String Description;
            public bool DescriptionPrinted;

            public Scenario(string description)
            {
                Description = description;
                DescriptionPrinted = false;
            }
        }

        public static void InitScenario(string scenarioDescription)
        {
            _scenarioDescriptions.Clear();
            _scenarioDescriptions.Add(new Scenario(scenarioDescription));
        }

        public static void PushScenario(string scenarioDescription)
        {
            _scenarioDescriptions.Add(new Scenario(scenarioDescription));
        }

        public static void PushScenario(string scenarioDescription, int maxScenarioDepth)
        {
            while (maxScenarioDepth < _scenarioDescriptions.Count)
            {
                _scenarioDescriptions.RemoveAt(_scenarioDescriptions.Count - 1);
            }
            _scenarioDescriptions.Add(new Scenario(scenarioDescription));
        }

        public static int ScenarioDepth
        {
            get
            {
                return _scenarioDescriptions.Count;
            }
        }

        public static void PopScenario()
        {
            if (0 < _scenarioDescriptions.Count)
            {
                _scenarioDescriptions.RemoveAt(_scenarioDescriptions.Count - 1);
            }
        }

        public static void OutputDebugInfo(string debugInfo)
        {
            OutputMessage(debugInfo);
        }

        public static void OutputDebugInfo(string format, params object[] args)
        {
            OutputDebugInfo(String.Format(format, args));
        }

        private static void OutputMessage(string message)
        {
            if (0 < _scenarioDescriptions.Count)
            {
                Scenario currentScenario = _scenarioDescriptions[_scenarioDescriptions.Count - 1];

                if (!currentScenario.DescriptionPrinted)
                {
                    _outputWriter.WriteLine();
                    _outputWriter.WriteLine();
                    _outputWriter.WriteLine("**********************************************************************");
                    _outputWriter.WriteLine("** {0,-64} **", "SCENARIO:");

                    for (int i = 0; i < _scenarioDescriptions.Count; ++i)
                    {
                        _outputWriter.WriteLine(_scenarioDescriptions[i].Description);
                    }

                    _outputWriter.WriteLine("**********************************************************************");
                    currentScenario.DescriptionPrinted = true;
                }
            }

            _outputWriter.WriteLine(message);
        }

        /// <summary>
        /// If the expression is false writes the message to the console and increments the error count.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <param name="message">The message to print to the console if the expression is false.</param>
        /// <returns>true if expression is true else false.</returns>
        public static bool Eval(bool expression, string message)
        {
            if (!expression)
            {
                OutputMessage(message);
                ++_numErrors;

                _outputWriter.WriteLine();
            }

            return expression;
        }

        /// <summary>
        /// If the expression is false outputs the formatted message(String.Format(format, args)
        /// and increments the error count.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <param name="format">A String containing zero or more format items.</param>
        /// <param name="args">An Object array containing zero or more objects to format.</param>
        /// <returns>true if expression is true else false.</returns>
        public static bool Eval(bool expression, String format, params object[] args)
        {
            if (!expression)
            {
                return Eval(expression, String.Format(format, args));
            }

            return true;
        }

        /// <summary>
        /// Compares expected and actual if expected and actual are differnet outputs
        /// and increments the error count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="errorMsg">The message to output.
        /// Uses String.Format(errorMsg, expected, actual)</param>
        /// <returns>true if expected and actual are equal else false.</returns>
        public static bool EvalFormatted<T>(T expected, T actual, String errorMsg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, String.Format(errorMsg, expected, actual));

            return true;
        }

        /// <summary>
        /// Compares expected and actual if expected and actual are differnet outputs the
        /// error message and increments the error count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="errorMsg">The message to output.</param>
        /// <returns>true if expected and actual are equal else false.</returns>
        public static bool Eval<T>(T expected, T actual, String errorMsg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, errorMsg +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }

        public static bool Eval<T>(IEqualityComparer<T> comparer, T expected, T actual, String errorMsg)
        {
            bool retValue = comparer.Equals(expected, actual);

            if (!retValue)
                return Eval(retValue, errorMsg +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }

        public static bool Eval<T>(IComparer<T> comparer, T expected, T actual, String errorMsg)
        {
            bool retValue = 0 == comparer.Compare(expected, actual);

            if (!retValue)
                return Eval(retValue, errorMsg +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }

        /// <summary>
        /// Compares expected and actual if expected and actual are differnet outputs the
        /// error message and increments the error count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="format">A String containing zero or more format items.</param>
        /// <param name="args">An Object array containing zero or more objects to format.</param>
        /// <returns>true if expected and actual are equal else false.</returns>
        public static bool Eval<T>(T expected, T actual, String format, params object[] args)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, String.Format(format, args) +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }

        /// <summary>
        /// Runs a test scenario.
        /// </summary>
        /// <param name="test">The test testscenario to run.</param>
        /// <returns>true if the test passed else false.</returns>
        public static bool RunTestScenario(TestScenario test)
        {
            bool retValue;

            if (test())
            {
                _numTestcases++;
                retValue = true;
            }
            else
            {
                _numTestcaseFailures++;
                retValue = false;
            }

            _outputWriter.WriteLine("");
            return retValue;
        }

        /// <summary>
        /// The number of failed test cases.
        /// </summary>
        /// <value>The number of failed test cases.</value>
        public static int NumberOfFailedTestcases
        {
            get
            {
                return _numTestcaseFailures;
            }
        }

        /// <summary>
        /// The number of test cases.
        /// </summary>
        /// <value>The number of test cases.</value>
        public static int NumberOfTestcases
        {
            get
            {
                return _numTestcases;
            }
        }

        /// <summary>
        /// The number of errors.
        /// </summary>
        /// <value>The number of errors.</value>
        public static int NumberOfErrors
        {
            get
            {
                return _numErrors;
            }
        }

        /// <summary>
        /// The exit code to use if the test failed.
        /// </summary>
        /// <value>The exit code to use if the test failed.</value>
        public static int FailExitCode
        {
            get
            {
                return _failExitCode;
            }
            set
            {
                _failExitCodeSet = true;
                _failExitCode = value;
            }
        }

        /// <summary>
        /// The exit code to use.
        /// </summary>
        /// <value></value>
        public static int ExitCode
        {
            get
            {
                if (Pass)
                    return PASS_EXITCODE;

                return _failExitCode;
            }
        }

        /// <summary>
        /// If the fail exit code was set.
        /// </summary>
        /// <value>If the fail exit code was set.</value>
        public static bool IsFailExitCodeSet
        {
            get
            {
                return _failExitCodeSet;
            }
        }

        /// <summary>
        /// Returns true if all test cases passed else false.
        /// </summary>
        /// <value>If all test cases passed else false.</value>
        public static bool Pass
        {
            get
            {
                return 0 == _numErrors && 0 == _numTestcaseFailures;
            }
        }

        /// <summary>
        /// Determines if the stack is inlcluded with the output
        /// </summary>
        /// <value>False to output the stack with every failure else true to suppress the stack output</value>
        public static bool SuppressStackOutput
        {
            get
            {
                return _suppressStackOutput;
            }
            set
            {
                _suppressStackOutput = value;
            }
        }

        public static bool OutputExceptionMessages
        {
            get
            {
                return _outputExceptionMessages;
            }
            set
            {
                _outputExceptionMessages = value;
            }
        }

        public static System.IO.TextWriter OutputWriter
        {
            get
            {
                return _outputWriter;
            }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }

                _outputWriter = value;
            }
        }


        /// <summary>
        /// Resets all of the counters.
        /// </summary>
        public static void Reset()
        {
            _numErrors = 0;
            _numTestcaseFailures = 0;
            _numTestcases = 0;
            _failExitCodeSet = false;
            _failExitCode = DEFAULT_FAIL_EXITCODE;
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of type T.
        /// </summary>
        /// <typeparam name="T">The type of the exception to throw.</typeparam>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of type T.</param>
        /// <returns>true if exceptionGenerator through an exception of type T else false.</returns>
        public static bool VerifyException<T>(ExceptionGenerator exceptionGenerator) where T : Exception
        {
            return VerifyException(typeof(T), exceptionGenerator);
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of type T.
        /// </summary>
        /// <typeparam name="T">The type of the exception to throw.</typeparam>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of type T.</param>
        /// <param name="message">The message to output if the verification fails.</param>
        /// <returns>true if exceptionGenerator through an exception of type T else false.</returns>
        public static bool VerifyException<T>(ExceptionGenerator exceptionGenerator, string message) where T : Exception
        {
            return VerifyException(typeof(T), exceptionGenerator, message);
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of type T.
        /// </summary>
        /// <typeparam name="T">The type of the exception to throw.</typeparam>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of type T.</param>
        /// <param name="format">A String containing zero or more format items.</param>
        /// <param name="args">An Object array containing zero or more objects to format.</param>
        /// <returns>true if exceptionGenerator through an exception of type T else false.</returns>
        public static bool VerifyException<T>(ExceptionGenerator exceptionGenerator, String format, params object[] args) where T : Exception
        {
            return VerifyException(typeof(T), exceptionGenerator, String.Format(format, args));
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of type expectedExceptionType.
        /// </summary>
        /// <param name="expectedExceptionType"></param>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of
        /// type expectedExceptionType.</param>
        /// <returns>true if exceptionGenerator through an exception of type expectedExceptionType else false.</returns>
        public static bool VerifyException(Type expectedExceptionType, ExceptionGenerator exceptionGenerator)
        {
            return VerifyException(expectedExceptionType, exceptionGenerator, String.Empty);
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of type expectedExceptionType.
        /// </summary>
        /// <param name="expectedExceptionType">The type of the exception to throw.</typeparam>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of type T.</param>
        /// <param name="format">A String containing zero or more format items.</param>
        /// <param name="args">An Object array containing zero or more objects to format.</param>
        /// <returns>true if exceptionGenerator through an exception of type expectedExceptionType else false.</returns>
        public static bool VerifyException(Type expectedExceptionType, ExceptionGenerator exceptionGenerator, String format, params object[] args)
        {
            return VerifyException(expectedExceptionType, exceptionGenerator, String.Format(format, args));
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of type expectedExceptionType.
        /// </summary>
        /// <param name="expectedExceptionType"></param>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of
        /// type expectedExceptionType.</param>
        /// <param name="message">The message to output if the verification fails.</param>
        /// <returns>true if exceptionGenerator through an exception of type expectedExceptionType else false.</returns>
        public static bool VerifyException(Type expectedExceptionType, ExceptionGenerator exceptionGenerator, string message)
        {
            bool retValue = true;

            try
            {
                exceptionGenerator();
                retValue &= Test.Eval(false, (String.IsNullOrEmpty(message) ? String.Empty : (message + Environment.NewLine)) +
                    "Err Expected exception of the type {0} to be thrown and nothing was thrown",
                    expectedExceptionType);
            }
            catch (Exception exception)
            {
                retValue &= Test.Eval<Type>(expectedExceptionType, exception.GetType(),
                    (String.IsNullOrEmpty(message) ? String.Empty : (message + Environment.NewLine)) +
                    "Err Expected exception and actual exception differ.  Expected {0}, got \n{1}", expectedExceptionType, exception);

                if (retValue && _outputExceptionMessages)
                {
                    OutputDebugInfo("{0} message: {1}" + Environment.NewLine, expectedExceptionType, exception.Message);
                }
            }

            return retValue;
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of type expectedExceptionType1
        /// or expectedExceptionType2.
        /// </summary>
        /// <param name="expectedExceptionType1">The first exception type exceptionGenerator may throw.</param>
        /// <param name="expectedExceptionType2">The second exception type exceptionGenerator may throw.</param>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of type
        /// expectedExceptionType1 or expectedExceptionType2.</param>
        /// <returns>true if exceptionGenerator through an exception of type expectedExceptionType1
        /// of expectedExceptionType2 else false.</returns>
        public static bool VerifyException(Type expectedExceptionType1, Type expectedExceptionType2, ExceptionGenerator exceptionGenerator)
        {
            return VerifyException(new Type[] { expectedExceptionType1, expectedExceptionType2 }, exceptionGenerator);
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of type expectedExceptionType1
        /// or expectedExceptionType2 or expectedExceptionType3.
        /// </summary>
        /// <param name="expectedExceptionType1">The first exception type exceptionGenerator may throw.</param>
        /// <param name="expectedExceptionType2">The second exception type exceptionGenerator may throw.</param>
        /// <param name="expectedExceptionType3">The third exception type exceptionGenerator may throw.</param>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of type
        /// expectedExceptionType1 or expectedExceptionType2 or expectedExceptionType3.</param>
        /// <returns>true if exceptionGenerator through an exception of type expectedExceptionType1
        /// or expectedExceptionType2 or expectedExceptionType3 else false.</returns>
        public static bool VerifyException(Type expectedExceptionType1, Type expectedExceptionType2,
            Type expectedExceptionType3, ExceptionGenerator exceptionGenerator)
        {
            return VerifyException(new Type[] { expectedExceptionType1, expectedExceptionType2, expectedExceptionType3 }, exceptionGenerator);
        }

        public static bool VerifyException(Type[] expectedExceptionTypes, ExceptionGenerator exceptionGenerator)
        {
            return VerifyException(expectedExceptionTypes, exceptionGenerator, string.Empty);
        }

        public static bool VerifyException(Type[] expectedExceptionTypes, ExceptionGenerator exceptionGenerator, String format, params object[] args)
        {
            return VerifyException(expectedExceptionTypes, exceptionGenerator, String.Format(format, args));
        }

        /// <summary>
        /// Verifies that exceptionGenerator throws an exception of one of types in expectedExceptionTypes.
        /// </summary>
        /// <param name="expectedExceptionTypes">An array of the expected exception type that exceptionGenerator may throw.</param>
        /// <param name="exceptionGenerator">A delegate that is expected to throw and exception of
        /// one of the types in expectedExceptionTypes.</param>
        /// <returns>true if exceptionGenerator through an exception of one of types in
        /// expectedExceptionTypes else false.</returns>
        public static bool VerifyException(Type[] expectedExceptionTypes, ExceptionGenerator exceptionGenerator, string message)
        {
            bool retValue = true;
            bool exceptionNotThrown = false;
            bool exceptionTypeInvalid = true;
            Type exceptionType = null;
            Exception exceptionInstance = null;

            try
            {
                exceptionGenerator();
                exceptionNotThrown = true;
            }
            catch (Exception exception)
            {
                exceptionType = exception.GetType();
                exceptionInstance = exception;

                for (int i = 0; i < expectedExceptionTypes.Length; ++i)
                {
                    if (null != expectedExceptionTypes[i] && exceptionType == expectedExceptionTypes[i]) //null is not a valid exception type
                        exceptionTypeInvalid = false;
                }
            }

            if (exceptionNotThrown || exceptionTypeInvalid)
            {
                System.Text.StringBuilder exceptionTypeNames = new System.Text.StringBuilder();

                for (int i = 0; i < expectedExceptionTypes.Length; ++i)
                {
                    if (null != expectedExceptionTypes[i])
                    {
                        exceptionTypeNames.Append(expectedExceptionTypes[i].ToString());
                        exceptionTypeNames.Append(" ");
                    }
                }

                if (exceptionNotThrown)
                {
                    retValue &= Test.Eval(false, (String.IsNullOrEmpty(message) ? String.Empty : (message + Environment.NewLine)) +
                        "Err Expected exception of one of the following types to be thrown: {0} and nothing was thrown",
                        exceptionTypeNames.ToString());
                }
                else if (exceptionTypeInvalid)
                {
                    retValue &= Test.Eval(false, (String.IsNullOrEmpty(message) ? String.Empty : (message + Environment.NewLine)) +
                        "Err Expected exception of one of the following types to be thrown: {0} and the following was thrown:\n {1}",
                        exceptionTypeNames.ToString(), exceptionInstance.ToString());

                }
            }

            return retValue;
        }

        [Obsolete]
        public static bool VerifyException(ExceptionGenerator exceptionGenerator, Type expectedExceptionType)
        {
            bool retValue = true;

            try
            {
                exceptionGenerator();
                retValue &= Test.Eval(false, "Err Expected exception of the type {0} to be thrown and nothing was thrown",
                    expectedExceptionType);
            }
            catch (Exception exception)
            {
                retValue &= Test.Eval<Type>(expectedExceptionType, exception.GetType(), "Err Expected exception and actual exception differ");
            }

            return retValue;
        }
    }

    public class ArrayUtils
    {
        /// <summary>
        /// Creates array of the parameters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The paramaters to create array from.</param>
        /// <returns>An array of the parameters.</returns>
        public static T[] Create<T>(params T[] array)
        {
            return array;
        }

        /// <summary>
        /// Creates and sub array from array
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="array">The array to create the sub array from.</param>
        /// <param name="length">The length of the sub array.</param>
        /// <returns>A sub array from array starting at 0 with a length of length.</returns>
        public static V[] SubArray<V>(V[] array, int length)
        {
            return SubArray(array, 0, length);
        }

        /// <summary>
        /// Creates and sub array from array
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="array">The array to create the sub array from.</param>
        /// <param name="startIndex">The start index of the sub array.</param>
        /// <param name="length">The length of the sub array.</param>
        /// <returns></returns>
        public static V[] SubArray<V>(V[] array, int startIndex, int length)
        {
            V[] tempArray = new V[length];

            Array.Copy(array, startIndex, tempArray, 0, length);

            return tempArray;
        }

        /// <summary>
        /// Remove item from array.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="array">The array to remove the item from.</param>
        /// <param name="item">The item to remove from the array.</param>
        /// <returns>The array with the item removed.</returns>
        public static V[] RemoveItem<V>(V[] array, V item)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(item))
                {
                    return RemoveAt(array, i);
                }
            }

            return array;
        }

        /// <summary>
        /// Remove item at index from array.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="array">The array to remove the item from.</param>
        /// <param name="index">The index of the item to remove from the array.</param>
        /// <returns></returns>
        public static V[] RemoveAt<V>(V[] array, int index)
        {
            V[] tempArray = new V[array.Length - 1];

            Array.Copy(array, 0, tempArray, 0, index);
            Array.Copy(array, index + 1, tempArray, index, array.Length - index - 1);

            return tempArray;
        }

        public static V[] RemoveRange<V>(V[] array, int startIndex, int count)
        {
            V[] tempArray = new V[array.Length - count];

            Array.Copy(array, 0, tempArray, 0, startIndex);
            Array.Copy(array, startIndex + count, tempArray, startIndex, tempArray.Length - startIndex);

            return tempArray;
        }

        public static V[] CopyRanges<V>(V[] array, int startIndex1, int count1, int startIndex2, int count2)
        {
            V[] tempArray = new V[count1 + count2];

            Array.Copy(array, startIndex1, tempArray, 0, count1);
            Array.Copy(array, startIndex2, tempArray, count1, count2);

            return tempArray;
        }

        /// <summary>
        /// Concatenates of of the items to the end of array1.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="array1">The array to concatenate.</param>
        /// <param name="array2">The items to concatonate to the end of array1.</param>
        /// <returns>An array with all of the items from array1 followed by all of
        /// the items from array2.</returns>
        public static V[] Concat<V>(V[] array1, params V[] array2)
        {
            if (array1.Length == 0)
                return array2;

            if (array2.Length == 0)
                return array1;

            V[] tempArray = new V[array1.Length + array2.Length];

            Array.Copy(array1, 0, tempArray, 0, array1.Length);
            Array.Copy(array2, 0, tempArray, array1.Length, array2.Length);

            return tempArray;
        }

        /// <summary>
        /// Concatenates of of the items to the begining of array1.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="array1">The array to concatenate.</param>
        /// <param name="array2">The items to concatonate to the begining of array1.</param>
        /// <returns>An array with all of the items from array2 followed by all of
        /// the items from array1.</returns>
        public static V[] Prepend<V>(V[] array1, params V[] array2)
        {
            if (array1.Length == 0)
                return array2;

            if (array2.Length == 0)
                return array1;

            V[] tempArray = new V[array1.Length + array2.Length];

            Array.Copy(array2, 0, tempArray, 0, array2.Length);
            Array.Copy(array1, 0, tempArray, array2.Length, array1.Length);

            return tempArray;
        }

        /// <summary>
        /// Rverses array.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="array">The array to revers.</param>
        /// <returns>A copy of array with the items reversed.</returns>
        public static V[] Reverse<V>(V[] array)
        {
            return Reverse(array, 0, array.Length);
        }

        /// <summary>
        /// Rverses length items in array starting at index.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="array">The array to reverse.</param>
        /// <param name="index">The index to start reversing items at.</param>
        /// <param name="length">The number of items to revers</param>
        /// <returns>A copy of array with length items reversed starting at index.</returns>
        public static V[] Reverse<V>(V[] array, int index, int length)
        {
            if (array.Length < 2)
                return array;

            V[] tempArray = new V[array.Length];

            Array.Copy(array, 0, tempArray, 0, array.Length);
            Array.Reverse(tempArray, index, length);

            return tempArray;
        }

        /// <summary>
        /// Creates an array with a length of size and fills it with the items
        /// returned from generatedItem.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="size">The size of the array to create and fill.</param>
        /// <param name="generateItem">Returns the items to place into the array.</param>
        /// <returns>An array of length size with items returned from generateItem</returns>
        public static T[] CreateAndFillArray<T>(int size, GenerateItem<T> generateItem)
        {
            return FillArray<T>(new T[size], generateItem);
        }

        /// <summary>
        /// Fills array with items returned from generateItem.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array to to place the items in.</param>
        /// <param name="generateItem">Returns the items to place into the array.</param>
        /// <returns>The array with the items in it returned from generateItem.</returns>
        public static T[] FillArray<T>(T[] array, GenerateItem<T> generateItem)
        {
            int arrayLength = array.Length;
            for (int i = 0; i < arrayLength; ++i)
                array[i] = generateItem();

            return array;
        }

        /// <summary>
        /// Fills array with items returned from generateItem.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array to to place the items in.</param>
        /// <param name="generateItem">Returns the items to place into the array.</param>
        /// <returns>The array with the items in it returned from generateItem.</returns>
        public static Array FillArray<T>(Array array, GenerateItem<T> generateItem)
        {
            if (array.Rank == 1)
            {
                int arrayLength = array.Length;
                int lowerBound = array.GetLowerBound(0);
                for (int i = 0; i < arrayLength; ++i)
                    array.SetValue(generateItem(), i + lowerBound);
            }
            else
            {
                PerformAction(array, delegate(Array arrayParameter, int[] indicies) { arrayParameter.SetValue(generateItem(), indicies); });
            }

            return array;
        }

        /// <summary>
        /// Calls arrayAction on every item in array.
        /// </summary>
        /// <param name="array">The array to call arrayAction for each item in array.</param>
        /// <param name="arrayAction">The action to perform on every item in the array.</param>
        public static void PerformAction(Array array, MultiDimArrayAction arrayAction)
        {
            bool retValue = true;
            int rank = array.Rank;
            int[] indicies = new int[rank];
            int[] lengths = new int[rank];
            int[] bases = new int[rank];
            int[] lowerBounds = new int[rank];
            int tempLength = array.Length;

            for (int i = 0; i < rank; ++i)
            {
                indicies[i] = -1;
                lengths[i] = array.GetLength(i);
                lowerBounds[i] = array.GetLowerBound(i);
            }

            bases[rank - 1] = lengths[rank - 1];
            for (int i = rank - 2; 0 <= i; --i)
            {
                bases[i] = lengths[i] * bases[i + 1];
            }

            for (int i = 0; i < tempLength; ++i)
            {
                for (int j = 0; j < rank; ++j)
                {
                    if (i % bases[j] == 0)
                        ++indicies[j];
                }

                retValue = Test.Eval(array.GetValue(indicies), array.GetValue(indicies), "Err Expected Array index=" + i.ToString());
            }
        }

        /// <summary>
        /// Verifies the contents of the array.
        /// </summary>
        /// <param name="expectedArray">The expected items in the array.</param>
        /// <param name="actualArray">The actual array.</param>
        /// <returns>true if expectedArray and actualArray have the same contents.</returns>
        public static bool VerifyArray(Array expectedArray, Array actualArray)
        {
            if (!Test.Eval(expectedArray.Length, actualArray.Length, "Err Array Length"))
            {
                return false;
            }
            else
            {
                if (expectedArray.Rank == 1)
                    return VerifyArray(expectedArray, actualArray, expectedArray.GetLowerBound(0), expectedArray.Length);
                else
                {
                    bool retValue = true;
                    int rank = expectedArray.Rank;
                    int[] indicies = new int[rank];
                    int[] lengths = new int[rank];
                    int[] bases = new int[rank];
                    int[] lowerBounds = new int[rank];
                    int tempLength = expectedArray.Length;

                    for (int i = 0; i < rank; ++i)
                    {
                        indicies[i] = -1;
                        lengths[i] = expectedArray.GetLength(i);
                        lowerBounds[i] = expectedArray.GetLowerBound(i);
                    }

                    bases[rank - 1] = lengths[rank - 1];
                    for (int i = rank - 2; 0 <= i; --i)
                    {
                        bases[i] = lengths[i] * bases[i + 1];
                    }

                    for (int i = 0; i < tempLength; ++i)
                    {
                        for (int j = 0; j < rank; ++j)
                        {
                            if (i % bases[j] == 0)
                                ++indicies[j];
                        }

                        retValue = Test.Eval(expectedArray.GetValue(indicies), actualArray.GetValue(indicies), "Err Expected Array index=" + i.ToString());
                    }

                    return retValue;
                }
            }
        }

        /// <summary>
        /// Verifies the contents of the array.
        /// </summary>
        /// <param name="expectedArray">The expected items in the array.</param>
        /// <param name="actualArray">The actual array.</param>
        /// <param name="index">The index to start verifying the items at.</param>
        /// <param name="length">The number of item to verify</param>
        /// <returns>true if expectedArray and actualArray have the same contents.</returns>
        public static bool VerifyArray(Array expectedArray, Array actualArray, int index, int length)
        {
            bool retValue = true;
            int tempLength;

            tempLength = length + index;
            for (int i = index; i < tempLength; ++i)
                retValue &= Test.Eval<object>(expectedArray.GetValue(i), actualArray.GetValue(i),
                "Err Array index=" + i.ToString());

            return retValue;
        }

        /// <summary>
        /// Verifies the contents of the array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expectedArray">The expected items in the array.</param>
        /// <param name="actualArray">The actual array.</param>
        /// <returns>true if expectedArray and actualArray have the same contents.</returns>
        public static bool VerifyArray<T>(T[] expectedArray, T[] actualArray)
        {
            if (!Test.Eval(expectedArray.Length, actualArray.Length, "Err Array Length"))
            {
                return false;
            }
            else
            {
                return VerifyArray<T>(expectedArray, actualArray, 0, expectedArray.Length);
            }
        }

        /// <summary>
        /// Verifies the contents of the array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expectedArray">The expected items in the array.</param>
        /// <param name="actualArray">The actual array.</param>
        /// <param name="index">The index to start verifying the items at.</param>
        /// <param name="length">The number of item to verify</param>
        /// <returns>true if expectedArray and actualArray have the same contents.</returns>
        /// <summary>
        public static bool VerifyArray<T>(T[] expectedArray, T[] actualArray, int index, int length)
        {
            bool retValue = true;
            int tempLength;

            tempLength = length + index;
            for (int i = index; i < tempLength; ++i)
                retValue &= Test.Eval<object>(expectedArray[i], actualArray[i],
                "Err! Array index=" + i.ToString());

            return retValue;
        }
    }
}
