// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using System.Xml;

    [DataContract]
    public abstract class SyndicationItemFormatter
    {
        private SyndicationItem _item;

        protected SyndicationItemFormatter()
        {
            _item = null;
        }

        protected SyndicationItemFormatter(SyndicationItem itemToWrite)
        {
            if (itemToWrite == null)
            {
                throw new ArgumentNullException(nameof(itemToWrite));
            }
            _item = itemToWrite;
        }

        public SyndicationItem Item
        {
            get
            {
                return _item;
            }
        }

        public abstract String Version
        { get; }

        public abstract bool CanRead(XmlReader reader);

        public abstract Task ReadFromAsync(XmlReader reader);

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}, SyndicationVersion={1}", this.GetType(), this.Version);
        }

        public abstract Task WriteToAsync(XmlWriter writer);

        internal protected virtual void SetItem(SyndicationItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            _item = item;
        }

        internal static SyndicationItem CreateItemInstance(Type itemType)
        {
            if (itemType.Equals(typeof(SyndicationItem)))
            {
                return new SyndicationItem();
            }
            else
            {
                return (SyndicationItem)Activator.CreateInstance(itemType);
            }
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationItem item)
        {
            SyndicationFeedFormatter.LoadElementExtensions(buffer, writer, item);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationCategory category)
        {
            SyndicationFeedFormatter.LoadElementExtensions(buffer, writer, category);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationLink link)
        {
            SyndicationFeedFormatter.LoadElementExtensions(buffer, writer, link);
        }

        internal static void LoadElementExtensions(XmlBuffer buffer, XmlDictionaryWriter writer, SyndicationPerson person)
        {
            SyndicationFeedFormatter.LoadElementExtensions(buffer, writer, person);
        }

        protected static SyndicationCategory CreateCategory(SyndicationItem item)
        {
            return SyndicationFeedFormatter.CreateCategory(item);
        }

        protected static SyndicationLink CreateLink(SyndicationItem item)
        {
            return SyndicationFeedFormatter.CreateLink(item);
        }

        protected static SyndicationPerson CreatePerson(SyndicationItem item)
        {
            return SyndicationFeedFormatter.CreatePerson(item);
        }

        protected static void LoadElementExtensions(XmlReader reader, SyndicationItem item, int maxExtensionSize)
        {
            SyndicationFeedFormatter.LoadElementExtensions(reader, item, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, SyndicationCategory category, int maxExtensionSize)
        {
            SyndicationFeedFormatter.LoadElementExtensions(reader, category, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, SyndicationLink link, int maxExtensionSize)
        {
            SyndicationFeedFormatter.LoadElementExtensions(reader, link, maxExtensionSize);
        }

        protected static void LoadElementExtensions(XmlReader reader, SyndicationPerson person, int maxExtensionSize)
        {
            SyndicationFeedFormatter.LoadElementExtensions(reader, person, maxExtensionSize);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, SyndicationItem item, string version)
        {
            return SyndicationFeedFormatter.TryParseAttribute(name, ns, value, item, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, SyndicationCategory category, string version)
        {
            return SyndicationFeedFormatter.TryParseAttribute(name, ns, value, category, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, SyndicationLink link, string version)
        {
            return SyndicationFeedFormatter.TryParseAttribute(name, ns, value, link, version);
        }

        protected static bool TryParseAttribute(string name, string ns, string value, SyndicationPerson person, string version)
        {
            return SyndicationFeedFormatter.TryParseAttribute(name, ns, value, person, version);
        }


        protected static bool TryParseContent(XmlReader reader, SyndicationItem item, string contentType, string version, out SyndicationContent content)
        {
            return SyndicationFeedFormatter.TryParseContent(reader, item, contentType, version, out content);
        }


        protected static bool TryParseElement(XmlReader reader, SyndicationItem item, string version)
        {
            return SyndicationFeedFormatter.TryParseElement(reader, item, version);
        }

        protected static bool TryParseElement(XmlReader reader, SyndicationCategory category, string version)
        {
            return SyndicationFeedFormatter.TryParseElement(reader, category, version);
        }

        protected static bool TryParseElement(XmlReader reader, SyndicationLink link, string version)
        {
            return SyndicationFeedFormatter.TryParseElement(reader, link, version);
        }

        protected static bool TryParseElement(XmlReader reader, SyndicationPerson person, string version)
        {
            return SyndicationFeedFormatter.TryParseElement(reader, person, version);
        }

        protected static async Task WriteAttributeExtensionsAsync(XmlWriter writer, SyndicationItem item, string version)
        {
            await SyndicationFeedFormatter.WriteAttributeExtensionsAsync(writer, item, version);
        }

        protected static async Task WriteAttributeExtensionsAsync(XmlWriter writer, SyndicationCategory category, string version)
        {
            await SyndicationFeedFormatter.WriteAttributeExtensionsAsync(writer, category, version);
        }

        protected static async Task WriteAttributeExtensionsAsync(XmlWriter writer, SyndicationLink link, string version)
        {
            await SyndicationFeedFormatter.WriteAttributeExtensions(writer, link, version);
        }

        protected static async Task WriteAttributeExtensionsAsync(XmlWriter writer, SyndicationPerson person, string version)
        {
            await SyndicationFeedFormatter.WriteAttributeExtensionsAsync(writer, person, version);
        }

        protected static async Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationItem item, string version)
        {
            await SyndicationFeedFormatter.WriteElementExtensionsAsync(writer, item, version);
        }

        protected abstract SyndicationItem CreateItemInstance();

        protected Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationCategory category, string version)
        {
            return SyndicationFeedFormatter.WriteElementExtensionsAsync(writer, category, version);
        }

        protected Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationLink link, string version)
        {
            return SyndicationFeedFormatter.WriteElementExtensionsAsync(writer, link, version);
        }

        protected Task WriteElementExtensionsAsync(XmlWriter writer, SyndicationPerson person, string version)
        {
            return SyndicationFeedFormatter.WriteElementExtensionsAsync(writer, person, version);
        }
    }
}
