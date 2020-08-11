// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MyNamespace;
using System.Text.Json;
using Xunit;


namespace System.Text.Json.Serialization.Tests.CodeGen
{
    public class CampaignSummaryViewModelPocoTests
    {
        [Fact]
        public static void RoundTrip()
        {
            CampaignSummaryViewModel expected = Create();
            string json = JsonSerializer.Serialize(expected, JsonContext.Default.CampaignSummaryViewModel);

            CampaignSummaryViewModel obj = JsonSerializer.Deserialize(json, JsonContext.Default.CampaignSummaryViewModel);
            Verify(expected, obj);
        }

        internal static void Verify(CampaignSummaryViewModel expected, CampaignSummaryViewModel obj)
        {
            Assert.Equal(expected.Description, obj.Description);
            Assert.Equal(expected.Headline, obj.Headline);
            Assert.Equal(expected.Id, obj.Id);
            Assert.Equal(expected.ImageUrl, obj.ImageUrl);
            Assert.Equal(expected.OrganizationName, obj.OrganizationName);
            Assert.Equal(expected.Title, obj.Title);
        }

        private static CampaignSummaryViewModel Create()
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
    }
}
