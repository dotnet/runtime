// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Filters;

internal sealed class InterestingMessageFilter(IMessageFilter inner, IInterestingStringFinder stringFinder) : IMessageFilter
{
    public bool IncludeMessage(StressMsgData message)
        => stringFinder.IsInteresting(message.FormatString, out _) || inner.IncludeMessage(message);
}
