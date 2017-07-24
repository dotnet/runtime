// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Text;
using Xunit;

namespace System.Xml.Tests
{
    public class TCWriterSettingsMisc
    {
        [Fact]
        public void Reset_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Encoding = Encoding.UTF8;
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.None;
            wSettings.NewLineChars = "\n";
            wSettings.IndentChars = "\t\t";
            wSettings.NewLineOnAttributes = true;
            wSettings.CloseOutput = true;
            wSettings.CheckCharacters = false;
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.WriteEndDocumentOnClose = false;
            wSettings.Reset();

            CError.Equals(wSettings.Encoding, Encoding.UTF8, "Encoding");
            CError.Equals(wSettings.OmitXmlDeclaration, false, "OmitXmlDeclaration");
            CError.Equals(wSettings.NewLineHandling, NewLineHandling.Replace, "NewLineHandling");
            CError.Equals(wSettings.NewLineChars, Environment.NewLine, "NewLineChars");
            CError.Equals(wSettings.Indent, false, "Indent");
            CError.Equals(wSettings.IndentChars, "  ", "IndentChars");
            CError.Equals(wSettings.NewLineOnAttributes, false, "NewLineOnAttributes");
            CError.Equals(wSettings.CloseOutput, false, "CloseOutput");
            CError.Equals(wSettings.CheckCharacters, true, "CheckCharacters");
            CError.Equals(wSettings.ConformanceLevel, ConformanceLevel.Document, "ConformanceLevel");
            CError.Equals(wSettings.WriteEndDocumentOnClose, true, "WriteEndDocumentOnClose");
        }

        [Fact]
        public void Clone_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.Encoding = Encoding.UTF8;
            wSettings.OmitXmlDeclaration = true;
            wSettings.NewLineHandling = NewLineHandling.Entitize;
            wSettings.NewLineChars = "\n";
            wSettings.IndentChars = "                ";
            wSettings.NewLineOnAttributes = true;
            wSettings.CloseOutput = true;
            wSettings.CheckCharacters = false;
            wSettings.ConformanceLevel = ConformanceLevel.Document;
            wSettings.WriteEndDocumentOnClose = false;

            XmlWriterSettings newSettings = wSettings.Clone();

            CError.Equals(wSettings.Encoding, newSettings.Encoding, "Encoding");
            CError.Equals(wSettings.OmitXmlDeclaration, newSettings.OmitXmlDeclaration, "OmitXmlDeclaration");
            CError.Equals(wSettings.NewLineHandling, newSettings.NewLineHandling, "NewLineHandling");
            CError.Equals(wSettings.NewLineChars, newSettings.NewLineChars, "NewLineChars");
            CError.Equals(wSettings.Indent, newSettings.Indent, "Indent");
            CError.Equals(wSettings.IndentChars, newSettings.IndentChars, "IndentChars");
            CError.Equals(wSettings.NewLineOnAttributes, newSettings.NewLineOnAttributes, "NewLineOnAttributes");
            CError.Equals(wSettings.CloseOutput, newSettings.CloseOutput, "CloseOutput");
            CError.Equals(wSettings.CheckCharacters, newSettings.CheckCharacters, "CheckCharacters");
            CError.Equals(wSettings.ConformanceLevel, newSettings.ConformanceLevel, "ConformanceLevel");
            CError.Equals(wSettings.WriteEndDocumentOnClose, newSettings.WriteEndDocumentOnClose, "WriteEndDocumentOnClose");
        }
    }
}
