// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Recognition;
using System.Speech.Internal.SrgsParser;
using System.Text;

namespace System.Speech.Internal.GrammarBuilding
{
    /// <summary>
    /// 
    /// </summary>
    [DebuggerDisplay ("{DebugSummary}")]
    internal sealed class TagElement : BuilderElements
    {

        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        internal TagElement (object value)
        {
            _value = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="value"></param>
        internal TagElement (GrammarBuilderBase builder, object value)
            : this (value)
        {
            Add (builder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="value"></param>
        internal TagElement (GrammarBuilder builder, object value)
            : this (value)
        {
            Add (builder);
        }

        #endregion

        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region Public Methods

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.Equals"]/*' />
        public override bool Equals (object obj)
        {
            TagElement refObj = obj as TagElement;
            if (refObj == null)
            {
                return false;
            }
            if (!base.Equals (obj))
            {
                return false;
            }
            return _value.Equals (refObj._value);
        }

        /// TODOC <_include file='doc\SpeechAudioFormatInfo.uex' path='docs/doc[@for="SpeechAudioFormatInfo.GetHashCode"]/*' />
        public override int GetHashCode ()
        {
            return base.GetHashCode ();
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
        internal override GrammarBuilderBase Clone ()
        {
            TagElement tag = new TagElement (_value);
            tag.CloneItems (this);
            return tag;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementFactory"></param>
        /// <param name="parent"></param>
        /// <param name="rule"></param>
        /// <param name="ruleIds"></param>
        /// <returns></returns>
        internal override IElement CreateElement (IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
        {
            // Create the children elements
            IItem item = parent as IItem;
            if (item != null)
            {
                CreateChildrenElements (elementFactory, item, rule, ruleIds);
            }
            else
            {
                if (parent == rule)
                {
                    CreateChildrenElements (elementFactory, rule, ruleIds);
                }
                else
                {
                    System.Diagnostics.Debug.Assert (false);
                }
            }

            // Create the tag element at the end only if there were some children
            IPropertyTag tag = elementFactory.CreatePropertyTag (parent);
            tag.NameValue (parent, null, _value);
            return tag;
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
                return base.DebugSummary + " {" + _value + "}";
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private readonly object _value;

        #endregion

    }
}
