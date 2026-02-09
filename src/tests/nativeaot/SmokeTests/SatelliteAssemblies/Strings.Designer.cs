// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SatelliteAssemblies
{
    using System;
    using System.Reflection;
    using System.Resources;

    internal class Strings
    {
        private static ResourceManager s_resourceManager;

        internal static ResourceManager ResourceManager
        {
            get
            {
                if (s_resourceManager is null)
                {
                    s_resourceManager = new ResourceManager("SatelliteAssemblies.Strings", typeof(Strings).Assembly);
                }
                return s_resourceManager;
            }
        }

        internal static string Greeting => ResourceManager.GetString("Greeting");
    }
}
