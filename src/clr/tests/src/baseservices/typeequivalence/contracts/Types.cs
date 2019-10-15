// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

[assembly:ImportedFromTypeLib("TypeEquivalenceTest")] // Required to support embeddable types
[assembly:Guid("3B491C47-B176-4CF3-8748-F19E303F1714")]

namespace TypeEquivalenceTypes
{
    [ComImport]
    [Guid("F34D4DE8-B891-4D73-B177-C8F1139A9A67")]
    public interface IEmptyType
    {
    }

    [ComImport]
    [Guid("729E8A0A-ECAB-46F3-A151-EB494B92D40D")]
    public interface IMethodTestType
    {
        /// <summary>
        /// Multiply the input value by the implementation's scale
        /// e.g. scale = 6, i = 3, result = 18
        /// </summary>
        int ScaleInt(int i);

        /// <summary>
        /// Duplicate the input string by the implementation's scale
        /// e.g. scale = 3, s = "ab", result = "ababab"
        /// </summary>
        string ScaleString(string s);
    }

    /// <summary>
    /// Interface used for validating sparse embedded types
    /// </summary>
    [ComImport]
    [Guid("8220DE7C-79FF-40C5-9075-0031514C6930")]
    public interface ISparseType
    {
        int MultiplyBy1(int a);
        int MultiplyBy2(int a);
        int MultiplyBy3(int a);
        int MultiplyBy4(int a);
        int MultiplyBy5(int a);
        int MultiplyBy6(int a);
        int MultiplyBy7(int a);
        int MultiplyBy8(int a);
        int MultiplyBy9(int a);
        int MultiplyBy10(int a);
        int MultiplyBy11(int a);
        int MultiplyBy12(int a);
        int MultiplyBy13(int a);
        int MultiplyBy14(int a);
        int MultiplyBy15(int a);
        int MultiplyBy16(int a);
        int MultiplyBy17(int a);
        int MultiplyBy18(int a);
        int MultiplyBy19(int a);
        int MultiplyBy20(int a);
    }
}
