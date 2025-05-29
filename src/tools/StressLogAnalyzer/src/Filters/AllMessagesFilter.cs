// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Filters;

internal sealed class AllMessagesFilter(bool includeAllMessages) : IMessageFilter
{
    public bool IncludeMessage(StressMsgData message)
    {
        return includeAllMessages;
    }
}
