// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.even

using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class EventLogPropertySelectorTests
    {
        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Ctor_NullPropertyQueries_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new EventLogPropertySelector(null));
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void Ctor_NonEmptyPropertyQuery_Success()
        {
            IDictionary<string, string> dictionary = new SortedDictionary<string, string>() { ["key"] = "value" };
            var selector = new EventLogPropertySelector(dictionary.Keys);
            Assert.NotNull(selector);
            selector.Dispose();
        }

        [ConditionalFact(typeof(Helpers), nameof(Helpers.SupportsEventLogs))]
        public void GetPropertyValues_MatchProviderIdUsingProviderMetadata_Success()
        {
            Dictionary<string, Guid> providerNameAndIds = new Dictionary<string, Guid>();

            string logName = "Application";
            string queryString = "*[System/Level=4]";
            var xPathEnum = new List<string>() { "Event/System/EventID", "Event/System/Provider/@Name" };
            var logPropertyContext = new EventLogPropertySelector(xPathEnum);
            var eventsQuery = new EventLogQuery(logName, PathType.LogName, queryString);
            try 
            {
                using (var logReader = new EventLogReader(eventsQuery))
                {
                    for (EventLogRecord eventRecord = (EventLogRecord)logReader.ReadEvent();
                            eventRecord != null;
                            eventRecord = (EventLogRecord)logReader.ReadEvent())
                    {
                        IList<object> logEventProps;
                        logEventProps = eventRecord.GetPropertyValues(logPropertyContext);
                        int eventId;
                        Assert.True(int.TryParse(string.Format("{0}", logEventProps[0]), out eventId));
                        string providerName = (string)logEventProps[1];
                        if (!providerNameAndIds.ContainsKey(providerName) && eventRecord.ProviderId.HasValue)
                        {
                            providerNameAndIds.Add(providerName, eventRecord.ProviderId.Value);
                        }
                    }
                }
            }
            catch (EventLogNotFoundException) { }

            if (providerNameAndIds.Count > 0)
            {
                using (var session = new EventLogSession())
                {
                    foreach (var nameAndId in providerNameAndIds)
                    {
                        ProviderMetadata providerMetadata = null;
                        try
                        {
                            providerMetadata = new ProviderMetadata(nameAndId.Key);
                            Assert.Equal(providerMetadata.Id, nameAndId.Value);
                        }
                        catch (EventLogException)
                        {
                            continue;
                        }
                        finally
                        {
                            providerMetadata?.Dispose();
                        }
                    }
                }
            }
        }
    }
}
