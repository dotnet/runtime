// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MyNamespace;
using System.Text.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests.CodeGen
{
    public class ActiveOrUpcomingEventPocoTests
    {
        [Fact]
        public static void RoundTrip()
        {
            ActiveOrUpcomingEvent poco = Create();
            string json = JsonSerializer.Serialize(poco, JsonContext.Default.ActiveOrUpcomingEvent);

            ActiveOrUpcomingEvent obj = JsonSerializer.Deserialize(json, JsonContext.Default.ActiveOrUpcomingEvent);
            Verify(poco, obj);
        }

        internal static void Verify(ActiveOrUpcomingEvent expected, ActiveOrUpcomingEvent obj)
        {
            Assert.Equal(expected.CampaignManagedOrganizerName, obj.CampaignManagedOrganizerName);
            Assert.Equal(expected.CampaignName, obj.CampaignName);
            Assert.Equal(expected.Description, obj.Description);
            Assert.Equal(expected.EndDate, obj.EndDate);
            Assert.Equal(expected.Id, obj.Id);
            Assert.Equal(expected.ImageUrl, obj.ImageUrl);
            Assert.Equal(expected.Name, obj.Name);
            Assert.Equal(expected.StartDate, obj.StartDate);
        }

        private static ActiveOrUpcomingEvent Create()
        {
            return new ActiveOrUpcomingEvent
            {
                Id = 10,
                CampaignManagedOrganizerName = "Name FamiltyName",
                CampaignName = "The very new campaing",
                Description = "The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.",
                EndDate = DateTime.UtcNow.AddYears(1),
                Name = "Just a name",
                ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                StartDate = DateTime.UtcNow
            };
        }
    }
}
