// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Xml;
using System.Globalization;

namespace System.ServiceModel.Syndication
{
    internal static class FeedUtils
    {
        public static string AddLineInfo(XmlReader reader, string error)
        {
            IXmlLineInfo lineInfo = reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                error = string.Format(CultureInfo.InvariantCulture, "{0} {1}", SR.Format(SR.ErrorInLine, lineInfo.LineNumber, lineInfo.LinePosition), SR.Format(error));
            }
            return error;
        }

        static internal Collection<SyndicationCategory> CloneCategories(Collection<SyndicationCategory> categories)
        {
            if (categories == null)
            {
                return null;
            }
            Collection<SyndicationCategory> result = new NullNotAllowedCollection<SyndicationCategory>();
            for (int i = 0; i < categories.Count; ++i)
            {
                result.Add(categories[i].Clone());
            }
            return result;
        }

        static internal Collection<SyndicationLink> CloneLinks(Collection<SyndicationLink> links)
        {
            if (links == null)
            {
                return null;
            }
            Collection<SyndicationLink> result = new NullNotAllowedCollection<SyndicationLink>();
            for (int i = 0; i < links.Count; ++i)
            {
                result.Add(links[i].Clone());
            }
            return result;
        }

        static internal Collection<SyndicationPerson> ClonePersons(Collection<SyndicationPerson> persons)
        {
            if (persons == null)
            {
                return null;
            }
            Collection<SyndicationPerson> result = new NullNotAllowedCollection<SyndicationPerson>();
            for (int i = 0; i < persons.Count; ++i)
            {
                result.Add(persons[i].Clone());
            }
            return result;
        }

        static internal TextSyndicationContent CloneTextContent(TextSyndicationContent content)
        {
            if (content == null)
            {
                return null;
            }
            return (TextSyndicationContent)(content.Clone());
        }

        internal static Uri CombineXmlBase(Uri rootBase, string newBase)
        {
            if (string.IsNullOrEmpty(newBase))
            {
                return rootBase;
            }
            Uri newBaseUri = new Uri(newBase, UriKind.RelativeOrAbsolute);
            if (rootBase == null || newBaseUri.IsAbsoluteUri)
            {
                return newBaseUri;
            }
            return new Uri(rootBase, newBase);
        }

        internal static Uri GetBaseUriToWrite(Uri rootBase, Uri currentBase)
        {
            Uri uriToWrite;
            if (rootBase == currentBase || currentBase == null)
            {
                uriToWrite = null;
            }
            else if (rootBase == null)
            {
                uriToWrite = currentBase;
            }
            else
            {
                // rootBase != currentBase and both are not null
                // Write the relative base if possible
                if (rootBase.IsAbsoluteUri && currentBase.IsAbsoluteUri && rootBase.IsBaseOf(currentBase))
                {
                    uriToWrite = rootBase.MakeRelativeUri(currentBase);
                }
                else
                {
                    uriToWrite = currentBase;
                }
            }
            return uriToWrite;
        }

        static internal string GetUriString(Uri uri)
        {
            if (uri == null)
            {
                return null;
            }
            if (uri.IsAbsoluteUri)
            {
                return uri.AbsoluteUri;
            }
            else
            {
                return uri.ToString();
            }
        }

        static internal bool IsXmlns(string name, string ns)
        {
            return name == "xmlns" || ns == "http://www.w3.org/2000/xmlns/";
        }

        internal static bool IsXmlSchemaType(string name, string ns)
        {
            return name == "type" && ns == "http://www.w3.org/2001/XMLSchema-instance";
        }
    }
}
