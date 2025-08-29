// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // When applied to an intrinsic method, the method will become a characteristic check.
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    internal class AnalysisCharacteristicAttribute : Attribute
    {
    }
}
