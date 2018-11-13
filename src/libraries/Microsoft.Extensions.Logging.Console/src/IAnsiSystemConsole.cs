// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Console
{
    internal interface IAnsiSystemConsole
    {
        void Write(string message);
    }
}
