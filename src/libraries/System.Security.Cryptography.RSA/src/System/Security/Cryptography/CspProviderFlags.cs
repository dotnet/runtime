// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography
{
    //In case you are adding more values in the CspProviderFlags Flags below.
    // please change the Flags Set where int allFlags is initialized with 0x00FF;
    [Flags]
    public enum CspProviderFlags
    {
        NoFlags = 0x0000,
        UseMachineKeyStore = 0x0001,
        UseDefaultKeyContainer = 0x0002,
        UseNonExportableKey = 0x0004,
        UseExistingKey = 0x0008,
        UseArchivableKey = 0x0010,
        UseUserProtectedKey = 0x0020,
        NoPrompt = 0x0040,
        CreateEphemeralKey = 0x0080
    }
}
