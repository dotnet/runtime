// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime;

// Suppresses automatic canonical roots, not compilation required by actual dependencies.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
    AttributeTargets.Delegate | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
internal sealed class SuppressReadyToRunCanonicalRootingAttribute : Attribute
{
}
