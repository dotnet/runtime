// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code-gen'd

using System.Text.Json.Serialization;

namespace MyNamespace
{
    public partial class JsonContext : JsonSerializerContext
    {
        private static JsonContext _sDefault;
        public static JsonContext Default
        {
            get
            {
                if (_sDefault == null)
                {
                    _sDefault = new JsonContext();
                }

                return _sDefault;
            }
        }
    }
}
