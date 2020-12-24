// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Speech.Internal.SrgsParser;
using System.Xml;

namespace System.Speech.Recognition.SrgsGrammar
{
    /// <summary>
    /// Base class for all SRGS object to build XML fragment corresponding to the object.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("SrgsElement Children:[{_items.Count}]")]
    [DebuggerTypeProxy(typeof(SrgsElementDebugDisplay))]
    public abstract class SrgsElement : MarshalByRefObject, IElement
    {
        protected SrgsElement()
        {
        }

        #region Internal methods

        // Write the XML fragment describing the object.
        internal abstract void WriteSrgs(XmlWriter writer);

        // Debugger display string.
        internal abstract string DebuggerDisplayString();

        // Validate the SRGS element.
        /// <summary>
        /// Validate each element and recurse through all the children srgs
        /// elements if any.
        /// Any derived class implementing this method must call the base class
        /// in order for the children to be processed.
        /// </summary>
        internal virtual void Validate(SrgsGrammar grammar)
        {
            foreach (SrgsElement element in Children)
            {
                // Child validation
                element.Validate(grammar);
            }
        }

        void IElement.PostParse(IElement parent)
        {
        }

        #endregion

        #region Protected Properties

        internal virtual SrgsElement[] Children
        {
            get
            {
                return Array.Empty<SrgsElement>();
            }
        }

        #endregion

        #region Private Types

        // Used by the debugger display attribute
        internal class SrgsElementDebugDisplay
        {
            public SrgsElementDebugDisplay(SrgsElement element)
            {
                _elements = element.Children;
            }
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public SrgsElement[] AKeys
            {
                get
                {
                    return _elements;
                }
            }

            private SrgsElement[] _elements;
        }

        #endregion
    }
}
