// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

// See MethodClassification in src/coreclr/vm/method.hpp
internal enum MethodClassification
{
    IL = 0, // IL
    FCall = 1, // FCall (also includes tlbimped ctor, Delegate ctor)
    PInvoke = 2, // PInvoke Method
    EEImpl = 3, // special method; implementation provided by EE (like Delegate Invoke)
    Array = 4, // Array ECall
    Instantiated = 5, // Instantiated generic methods, including descriptors
                      // for both shared and unshared code (see InstantiatedMethodDesc)
    ComInterop = 6,
    Dynamic = 7, // for method desc with no metadata behind
}
