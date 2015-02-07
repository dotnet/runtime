// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ServiceProcess
{
    public enum ServiceStartMode
    {
        Manual = Interop.START_TYPE_DEMAND,
        Automatic = Interop.START_TYPE_AUTO,
        Disabled = Interop.START_TYPE_DISABLED,
    }
}
