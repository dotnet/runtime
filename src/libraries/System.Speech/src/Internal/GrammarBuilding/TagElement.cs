// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal.SrgsParser;
using System.Speech.Recognition;

namespace System.Speech.Internal.GrammarBuilding
{
    [DebuggerDisplay("{DebugSummary}")]
    internal sealed class TagElement : BuilderElements
    {
        #region Constructors

        internal TagElement(object value)
        {
            _value = value;
        }

        internal TagElement(GrammarBuilderBase builder, object value)
            : this(value)
        {
            Add(builder);
        }

        internal TagElement(GrammarBuilder builder, object value)
            : this(value)
        {
            Add(builder);
        }

        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            TagElement refObj = obj as TagElement;
            if (refObj == null)
            {
                return false;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            return _value.Equals(refObj._value);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Internal Methods

        internal override GrammarBuilderBase Clone()
        {
            TagElement tag = new(_value);
            tag.CloneItems(this);
            return tag;
        }

        internal override IElement CreateElement(IElementFactory elementFactory, IElement parent, IRule rule, IdentifierCollection ruleIds)
        {
            // Create the children elements
            IItem item = parent as IItem;
            if (item != null)
            {
                CreateChildrenElements(elementFactory, item, rule, ruleIds);
            }
            else
            {
                if (parent == rule)
                {
                    CreateChildrenElements(elementFactory, rule, ruleIds);
                }
                else
                {
                    System.Diagnostics.Debug.Assert(false);
                }
            }

            // Create the tag element at the end only if there were some children
            IPropertyTag tag = elementFactory.CreatePropertyTag(parent);
            tag.NameValue(parent, null, _value);
            return tag;
        }

        #endregion

        #region Internal Properties

        internal override string DebugSummary
        {
            get
            {
                return base.DebugSummary + " {" + _value + "}";
            }
        }

        #endregion

        #region Private Fields

        private readonly object _value;

        #endregion
    }
}
