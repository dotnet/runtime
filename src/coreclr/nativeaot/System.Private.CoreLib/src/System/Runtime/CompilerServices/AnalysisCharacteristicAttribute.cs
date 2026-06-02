// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // When applied to an intrinsic method, the method will become a characteristic check.
    //
    // The method needs to return bool and have no parameters.
    //
    // The compiler will replace the body of the method with a constant true/false depending
    // on whether the characteristic (tag) was added to the whole program view.
    // The name of the characteristic is the name of the method.
    // Compiler can add characteristics to the whole program view using the
    // NodeFactory.AnalysisCharacteristic(string) method.
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    internal class AnalysisCharacteristicAttribute : Attribute
    {
    }
}
