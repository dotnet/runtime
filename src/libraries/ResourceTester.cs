using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.ServiceModel.Syndication.Resources;

namespace Microsoft.ServiceModel.Syndication
{
    public class ResourceTester
    {
        private XmlReader reader;

        public void test()
        {
            //reader = XmlReader.Create("feed.xml");

            String test = SR.GetString(SR.Atom10SpecRequiresTextConstruct, "1", "2");

        }
    }
}
