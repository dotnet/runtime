// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Tests.Serialization
{
    internal partial class JsonContext : JsonSerializerContext
    {
        public override JsonTypeInfo GetTypeInfo(System.Type type)
        {
            if (type == typeof(WeatherForecastWithPOCOs))
            {
                return this.WeatherForecastWithPOCOs;
            }

            return null!;
        }
    }
}
