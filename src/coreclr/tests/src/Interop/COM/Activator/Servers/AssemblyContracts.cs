// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

[Guid("6B002803-4C9B-4E4F-BDA2-46AEFFD8D559")]
public interface IGetTypeFromC
{
    object GetTypeFromC();
}

[Guid("DA746E78-E1E8-44DD-8184-203AB57B3002")]
public interface IValidateRegistrationCallbacks
{
    bool DidRegister();
    bool DidUnregister();
    void Reset();
}