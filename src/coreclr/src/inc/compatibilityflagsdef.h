// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file contains list of the CLR compatibility flags. The compatibility flags
// are used to mitigate breaking changes in the platform code. They are used to trigger the legacy
// behavior.

// The general usage pattern is:

// if (GetCompatibilityFlag(CompatibilityFlag.Foo)) {
//     // the legacy behavior
// }
// else {
//     // the new behavior
// }

// Add your own compatibility flags to the end of the list. You should go through the breaking 
// change approval process before adding it.
//
// Do not remove definitions for deprecated compatibility flags. Once the value is 
// assigned to the compatibility flag, it has to be kept forever.

// This file is compiled twice: once to generate managed enum in clr\src\bcl, and second time
// to generate the unmanaged enum in clr\src\vm.


#ifndef COMPATFLAGDEF
#error You must define COMPATFLAGDEF macro before including compatibilityflagsdef.h
#endif

COMPATFLAGDEF(SwallowUnhandledExceptions) // Legacy exception handling policy - swallow unhandled exceptions

COMPATFLAGDEF(NullReferenceExceptionOnAV) // Legacy null reference exception policy - throw NullReferenceExceptions for access violations

COMPATFLAGDEF(EagerlyGenerateRandomAsymmKeys) // Legacy mode for DSACryptoServiceProvider/RSACryptoServiceProvider - create a random key in the constructor eagerly

COMPATFLAGDEF(FullTrustListAssembliesInGac) // Legacy mode for not requiring FT list assemblies to be in the GAC - if set, the requirement to be in the GAC would not be enforced.

COMPATFLAGDEF(DateTimeParseIgnorePunctuation) // Through to V1.1, DateTime parse would ignore any unrecognized punctuation. 
                                              // This flag restores that behavior.

COMPATFLAGDEF(OnlyGACDomainNeutral)         // This allows late setting of app domain security and 
                                            // assembly evidence, even when LoaderOptimization=MultiDomain

COMPATFLAGDEF(DisableReplacementCustomCulture) // This allow disabling replacement custom cultures. will always get the shipped framework culture.

#undef COMPATFLAGDEF
