// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;

namespace System.Speech.Recognition.SrgsGrammar
{
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplayString ()}")]
    [DebuggerTypeProxy(typeof(OneOfDebugDisplay))]
    public class SrgsOneOf : SrgsElement, IOneOf
    {
        #region Constructors
        public SrgsOneOf()
        {
        }
        public SrgsOneOf(params string[] items)
            : this()
        {
            Helpers.ThrowIfNull(items, nameof(items));

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    throw new ArgumentNullException(nameof(items), SR.Get(SRID.ParamsEntryNullIllegal));
                }

                _items.Add(new SrgsItem(items[i]));
            }
        }
        public SrgsOneOf(params SrgsItem[] items)
            : this()
        {
            Helpers.ThrowIfNull(items, nameof(items));

            for (int i = 0; i < items.Length; i++)
            {
                SrgsItem item = items[i];
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(items), SR.Get(SRID.ParamsEntryNullIllegal));
                }

                _items.Add(item);
            }
        }

        #endregion

        #region public Method
        public void Add(SrgsItem item)
        {
            Helpers.ThrowIfNull(item, nameof(item));

            Items.Add(item);
        }

        #endregion

        #region public Properties

        // ISSUE: Do we need more constructors? Take a look at RuleElementCollection.AddOneOf methods. [Bug# 37115]
        public Collection<SrgsItem> Items
        {
            get
            {
                return _items;
            }
        }

        #endregion

        #region internal Methods

        internal override void WriteSrgs(XmlWriter writer)
        {
            // Write <one-of>...</one-of>
            writer.WriteStartElement("one-of");
            foreach (SrgsItem element in _items)
            {
                element.WriteSrgs(writer);
            }

            writer.WriteEndElement();
        }

        internal override string DebuggerDisplayString()
        {
            StringBuilder sb = new("SrgsOneOf Count = ");
            sb.Append(_items.Count);
            return sb.ToString();
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Allows the Srgs Element base class to implement
        /// features requiring recursion in the elements tree.
        /// </summary>
        internal override SrgsElement[] Children
        {
            get
            {
                SrgsElement[] elements = new SrgsElement[_items.Count];
                int i = 0;
                foreach (SrgsItem item in _items)
                {
                    elements[i++] = item;
                }
                return elements;
            }
        }

        #endregion

        #region Private Fields

        private SrgsItemList _items = new();

        #endregion

        #region Private Types

        // Used by the debugger display attribute
        internal class OneOfDebugDisplay
        {
            public OneOfDebugDisplay(SrgsOneOf oneOf)
            {
                _items = oneOf._items;
            }
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public SrgsItem[] AKeys
            {
                get
                {
                    SrgsItem[] items = new SrgsItem[_items.Count];
                    for (int i = 0; i < _items.Count; i++)
                    {
                        items[i] = _items[i];
                    }
                    return items;
                }
            }

            private Collection<SrgsItem> _items;
        }

        #endregion
    }
}
