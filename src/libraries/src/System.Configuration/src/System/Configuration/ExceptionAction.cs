// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    internal enum ExceptionAction
    {
        NonSpecific, // Not specific to a particular section, nor a global schema error
        Local, // Error specific to a particular section
        Global, // Error in the global (file) schema 
    }
}