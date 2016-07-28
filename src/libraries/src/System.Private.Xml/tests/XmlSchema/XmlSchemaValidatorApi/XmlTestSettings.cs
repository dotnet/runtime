// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Xml.Tests
{
    public class XmlTestSettings
    {
        public string DataPath
        {
            get { return ""; }
        }

        public int MaxPriority
        {
            get { return 3; }
        }
    }

	public static class FilePathUtil
	{
		public static XmlTestSettings TestSettings = new XmlTestSettings();

	    public static string GetDataPath()
        {
            return TestSettings.DataPath;
        }

        public static string GetTestDataPath()
        {
            return Path.Combine(GetDataPath(), "TestFiles", "TestData");
        }
        public static string GetStandardPath()
        {
            return Path.Combine(GetDataPath(), "TestFiles", "StandardTests");
        }
	}
}
