// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System
{
    // Internal methodtable used to instantiate the "canonical" methodtable for generic instantiations.
    // The name "__Canon" will never been seen by users but it will appear a lot in debugger stack traces
    // involving generics so it is kept deliberately short as to avoid being a nuisance.

    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    internal class __Canon
    {
    }
}
