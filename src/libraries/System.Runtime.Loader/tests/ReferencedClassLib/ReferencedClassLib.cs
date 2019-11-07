// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Resources;
using System.Globalization;

namespace ReferencedClassLib
{
    public class Program
    {
        public static string Describe(string lang)
        {
            try
            {
                ResourceManager rm = new ResourceManager("ReferencedClassLib.ReferencedStrings", typeof(Program).Assembly);

                CultureInfo ci = CultureInfo.CreateSpecificCulture(lang);

                return rm.GetString("Describe", ci);
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}
