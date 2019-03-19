// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Internal;
using System.IO;
using System.Xml;

namespace System.Configuration
{
    /// <summary>
    /// This section handler allows &lt;appSettings file="user.config" /&gt;
    /// The file pointed to by the file= attribute is read as if it is
    /// an appSettings section in the config file.
    /// Note: the user.config file must have its root element match the 
    /// section referring to it.  So if appSettings has a file="user.config" 
    /// attribute the root element in user.config must also be named appSettings.
    /// </summary>
    public class NameValueFileSectionHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            object result = parent;

            // parse XML
            XmlNode fileAttribute = section.Attributes.RemoveNamedItem("file");

            result = NameValueSectionHandler.CreateStatic(result, section);

            if (fileAttribute != null && fileAttribute.Value.Length != 0)
            {
                string filename = null;
                filename = fileAttribute.Value;
                IConfigErrorInfo configXmlNode = fileAttribute as IConfigErrorInfo;
                if (configXmlNode == null)
                {
                    return null;
                }

                string configFile = configXmlNode.Filename;
                string directory = Path.GetDirectoryName(configFile);
                string sourceFileFullPath = Path.Combine(directory, filename);

                if (File.Exists(sourceFileFullPath))
                {

                    ConfigXmlDocument doc = new ConfigXmlDocument();
                    try
                    {
                        doc.Load(sourceFileFullPath);
                    }
                    catch (XmlException e)
                    {
                        throw new ConfigurationErrorsException(e.Message, e, sourceFileFullPath, e.LineNumber);
                    }

                    if (section.Name != doc.DocumentElement.Name)
                    {
                        throw new ConfigurationErrorsException(
                            SR.Format(SR.Config_name_value_file_section_file_invalid_root, section.Name),
                            doc.DocumentElement);
                    }

                    result = NameValueSectionHandler.CreateStatic(result, doc.DocumentElement);
                }
            }

            return result;
        }
    }

}
