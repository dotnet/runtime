// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Options
{
    [EventSource(Name = "Microsoft-Extensions-Options")]
    internal sealed partial class OptionsEventSource : EventSource
    {
        public static readonly OptionsEventSource Log = new OptionsEventSource();

        [Event(1, Level = EventLevel.Error)]
        public void ReloadValidationFailed(string optionsName, string optionsType, string exception)
        {
            WriteEvent(1, optionsName, optionsType, exception);
        }

        [NonEvent]
        public void ReloadValidationFailed(string? optionsName, Type optionsType, Exception exception)
        {
            if (IsEnabled(EventLevel.Error, EventKeywords.All))
            {
                ReloadValidationFailed(optionsName ?? string.Empty, optionsType.ToString(), exception.ToString());
            }
        }
    }
}
