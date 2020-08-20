// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using JsonCodeGeneration;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    [JsonSerializable]
    public class Location
    {
        public int Id { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Country { get; set; }
    }

    [JsonSerializable]
    public class ActiveOrUpcomingEvent
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public string Name { get; set; }
        public string CampaignName { get; set; }
        public string CampaignManagedOrganizerName { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }

    [JsonSerializable]
    public class CampaignSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string OrganizationName { get; set; }
        public string Headline { get; set; }
    }

    [JsonSerializable]
    public class IndexViewModel
    {
        public List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
        public CampaignSummaryViewModel FeaturedCampaign { get; set; }
        public bool IsNewAccount { get; set; }
        public bool HasFeaturedCampaign => FeaturedCampaign != null;
    }

    public class JsonSerializerSourceGeneratorTests
    {
        [Fact]
        public static void RoundTripLocation()
        {
            Location expected = CreateLocation();

            string json = JsonSerializer.Serialize(expected, JsonContext.Instance.SystemTextJsonSourceGenerationTestsLocation);
            Location obj = JsonSerializer.Deserialize(json, JsonContext.Instance.SystemTextJsonSourceGenerationTestsLocation);

            VerifyLocation(expected, obj);
        }

        [Fact]
        public void RoundTripIndexViewModel()
        {
            IndexViewModel expected = CreateIndexViewModel();

            string json = JsonSerializer.Serialize(expected, JsonContext.Instance.SystemTextJsonSourceGenerationTestsIndexViewModel);
            IndexViewModel obj = JsonSerializer.Deserialize(json, JsonContext.Instance.SystemTextJsonSourceGenerationTestsIndexViewModel);

            VerifyIndexViewModel(expected, obj);
        }

        [Fact]
        public static void RoundTripCampaignSummaryViewModel()
        {
            CampaignSummaryViewModel expected = CreateCampaignSummaryViewModel();

            string json = JsonSerializer.Serialize(expected, JsonContext.Instance.SystemTextJsonSourceGenerationTestsCampaignSummaryViewModel);
            CampaignSummaryViewModel obj = JsonSerializer.Deserialize(json, JsonContext.Instance.SystemTextJsonSourceGenerationTestsCampaignSummaryViewModel);

            VerifyCampaignSummaryViewModel(expected, obj);
        }

        [Fact]
        public static void RoundTripActiveOrUpcomingEvent()
        {
            ActiveOrUpcomingEvent expected = CreateActiveOrUpcomingEvent();

            string json = JsonSerializer.Serialize(expected, JsonContext.Instance.SystemTextJsonSourceGenerationTestsActiveOrUpcomingEvent);
            ActiveOrUpcomingEvent obj = JsonSerializer.Deserialize(json, JsonContext.Instance.SystemTextJsonSourceGenerationTestsActiveOrUpcomingEvent);

            VerifyActiveOrUpcomingEvent(expected, obj);
        }

        internal static Location CreateLocation()
        {
            return new Location
            {
                Id = 1234,
                Address1 = "The Street Name",
                Address2 = "20/11",
                City = "The City",
                State = "The State",
                PostalCode = "abc-12",
                Name = "Nonexisting",
                PhoneNumber = "+0 11 222 333 44",
                Country = "The Greatest"
            };
        }

        internal static void VerifyLocation(Location expected, Location obj)
        {
            Assert.Equal(expected.Address1, obj.Address1);
            Assert.Equal(expected.Address2, obj.Address2);
            Assert.Equal(expected.City, obj.City);
            Assert.Equal(expected.State, obj.State);
            Assert.Equal(expected.PostalCode, obj.PostalCode);
            Assert.Equal(expected.Name, obj.Name);
            Assert.Equal(expected.PhoneNumber, obj.PhoneNumber);
            Assert.Equal(expected.Country, obj.Country);
        }

        internal static ActiveOrUpcomingEvent CreateActiveOrUpcomingEvent()
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

        internal static void VerifyActiveOrUpcomingEvent(ActiveOrUpcomingEvent expected, ActiveOrUpcomingEvent obj)
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

        internal static CampaignSummaryViewModel CreateCampaignSummaryViewModel()
        {
            return new CampaignSummaryViewModel
            {
                Description = "Very nice campaing",
                Headline = "The Headline",
                Id = 234235,
                OrganizationName = "The Company XYZ",
                ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                Title = "Promoting Open Source"
            };
        }

        internal static void VerifyCampaignSummaryViewModel(CampaignSummaryViewModel expected, CampaignSummaryViewModel obj)
        {
            Assert.Equal(expected.Description, obj.Description);
            Assert.Equal(expected.Headline, obj.Headline);
            Assert.Equal(expected.Id, obj.Id);
            Assert.Equal(expected.ImageUrl, obj.ImageUrl);
            Assert.Equal(expected.OrganizationName, obj.OrganizationName);
            Assert.Equal(expected.Title, obj.Title);
        }

        internal static IndexViewModel CreateIndexViewModel()
        {
            return new IndexViewModel
            {
                IsNewAccount = false,
                FeaturedCampaign = new CampaignSummaryViewModel
                {
                    Description = "Very nice campaing",
                    Headline = "The Headline",
                    Id = 234235,
                    OrganizationName = "The Company XYZ",
                    ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                    Title = "Promoting Open Source"
                },
                ActiveOrUpcomingEvents = Enumerable.Repeat(
                    new ActiveOrUpcomingEvent
                    {
                        Id = 10,
                        CampaignManagedOrganizerName = "Name FamiltyName",
                        CampaignName = "The very new campaing",
                        Description = "The .NET Foundation works with Microsoft and the broader industry to increase the exposure of open source projects in the .NET community and the .NET Foundation. The .NET Foundation provides access to these resources to projects and looks to promote the activities of our communities.",
                        EndDate = DateTime.UtcNow.AddYears(1),
                        Name = "Just a name",
                        ImageUrl = "https://www.dotnetfoundation.org/theme/img/carousel/foundation-diagram-content.png",
                        StartDate = DateTime.UtcNow
                    },
                    count: 20).ToList()
            };
        }

        internal static void VerifyIndexViewModel(IndexViewModel expected, IndexViewModel obj)
        {
            Assert.Equal(expected.ActiveOrUpcomingEvents.Count, obj.ActiveOrUpcomingEvents.Count);
            for (int i = 0; i < expected.ActiveOrUpcomingEvents.Count; i++)
            {
                VerifyActiveOrUpcomingEvent(expected.ActiveOrUpcomingEvents[i], obj.ActiveOrUpcomingEvents[i]);
            }

            VerifyCampaignSummaryViewModel(expected.FeaturedCampaign, obj.FeaturedCampaign);
            Assert.Equal(expected.HasFeaturedCampaign, obj.HasFeaturedCampaign);
            Assert.Equal(expected.IsNewAccount, obj.IsNewAccount);
        }
    }
}
