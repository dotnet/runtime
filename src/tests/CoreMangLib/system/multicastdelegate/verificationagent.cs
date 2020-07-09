// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

/// <summary>
/// Summary description for Class1
/// </summary>
public class VerificationAgent
{
    #region Public Methods
    /// <summary>
    /// Throws System.Exception when test case failed.
    /// </summary>
    /// <param name="message">message want to be logged</param>
    /// <param name="actual">actual result</param>
    /// <param name="expected">expected result</param>
    public static void ThrowVerificationException(string message, object actual, object expected)
    {
        throw new Exception(string.Format("{0}, actual: {1}, expected: {2}",
            message,
            actual,
            expected));
    }
    #endregion
}
