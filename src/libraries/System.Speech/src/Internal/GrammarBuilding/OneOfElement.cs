// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Recognition;
using System.Speech.Internal.SrgsParser;
using System.Text;

namespace System.Speech.Internal.GrammarBuilding
{
    [DebuggerDisplay("{DebugSummary}")]
    internal sealed class OneOfElement : BuilderElements
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal OneOfElement()
        {
        }

        #endregion


        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override GrammarBuilderBase Clone()
        {
            OneOfElement oneOf = new OneOfElement();
            oneOf.CloneItems(this);
            return oneOf;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementFactory"></param>
        /// <param name="parent"></param>
        /// <param name="rule"></param>
        /// <param name="ruleIds"></param>
        /// <returns></returns>
        internal override IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
        {
            // Create and return the IOneOf representing the current object
            IOneOf oneOf = elementFactory.CreateOneOf(parent, rule);
            foreach (GrammarBuilderBase item in Items)
            {
                ItemElement newItem = item as ItemElement;
                if (newItem == null)
                {
                    newItem = new ItemElement(item);
                }

                IItem element = (IItem)newItem.CreateElement(elementFactory, oneOf, rule, ruleIds);
                element.PostParse(oneOf);
                elementFactory.AddItem(oneOf, element);
            }
            return oneOf;
        }

        #endregion

        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Properties

        override internal string DebugSummary
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (GrammarBuilderBase item in Items)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(",");
                    }
                    sb.Append(item.DebugSummary);
                }
                return "[" + sb.ToString() + "]";
            }
        }

        #endregion
    }
}
