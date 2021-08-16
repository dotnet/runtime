// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Speech.Internal;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;

namespace System.Speech.Recognition.SrgsGrammar
{
    [Serializable]
    [DebuggerDisplay("{DebuggerDisplayString ()}")]
    [DebuggerTypeProxy(typeof(SrgsItemDebugDisplay))]
    public class SrgsItem : SrgsElement, IItem
    {
        #region Constructors
        public SrgsItem()
        {
            _elements = new SrgsElementList();
        }
        public SrgsItem(string text)
            : this()
        {
            Helpers.ThrowIfEmptyOrNull(text, nameof(text));

            _elements.Add(new SrgsText(text));
        }
        public SrgsItem(params SrgsElement[] elements)
            : this()
        {
            Helpers.ThrowIfNull(elements, nameof(elements));

            for (int iElement = 0; iElement < elements.Length; iElement++)
            {
                if (elements[iElement] == null)
                {
                    throw new ArgumentNullException(nameof(elements), SR.Get(SRID.ParamsEntryNullIllegal));
                }
                _elements.Add(elements[iElement]);
            }
        }
        public SrgsItem(int repeatCount)
            : this()
        {
            SetRepeat(repeatCount);
        }
        public SrgsItem(int min, int max)
            : this()
        {
            SetRepeat(min, max);
        }

        //overloads with setting the repeat.
        public SrgsItem(int min, int max, string text)
            : this(text)
        {
            SetRepeat(min, max);
        }
        public SrgsItem(int min, int max, params SrgsElement[] elements)
            : this(elements)
        {
            SetRepeat(min, max);
        }

        #endregion

        #region Public Method
        public void SetRepeat(int count)
        {
            // Negative values are not allowed
            if (count < 0 || count > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            _minRepeat = _maxRepeat = count;
        }
        public void SetRepeat(int minRepeat, int maxRepeat)
        {
            // Negative values are not allowed
            if (minRepeat < 0 || minRepeat > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(minRepeat), SR.Get(SRID.InvalidMinRepeat, minRepeat));
            }
            if (maxRepeat != int.MaxValue && (maxRepeat < 0 || maxRepeat > 255))
            {
                throw new ArgumentOutOfRangeException(nameof(maxRepeat), SR.Get(SRID.InvalidMinRepeat, maxRepeat));
            }

            // Max be greater or equal to min
            if (minRepeat > maxRepeat)
            {
                throw new ArgumentException(SR.Get(SRID.MinGreaterThanMax));
            }
            _minRepeat = minRepeat;
            _maxRepeat = maxRepeat;
        }
        public void Add(SrgsElement element)
        {
            Helpers.ThrowIfNull(element, nameof(element));

            Elements.Add(element);
        }

        #endregion

        #region Public Properties
        public Collection<SrgsElement> Elements
        {
            get
            {
                return _elements;
            }
        }
        // The probability that this item will be repeated.
        public float RepeatProbability
        {
            get
            {
                return _repeatProbability;
            }
            set
            {
                if (value < 0.0f || value > 1.0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.InvalidRepeatProbability, value));
                }

                _repeatProbability = value;
            }
        }
        // The minimum number of occurrences this item can/must be repeated.
        public int MinRepeat
        {
            get
            {
                return _minRepeat == NotSet ? 1 : _minRepeat;
            }
        }
        // The maximum number of occurrences this item can/must be repeated.
        public int MaxRepeat
        {
            get
            {
                return _maxRepeat == NotSet ? 1 : _maxRepeat;
            }
        }
        public float Weight
        {
            get
            {
                return _weight;
            }
            set
            {
                if (value <= 0.0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.InvalidWeight, value));
                }

                _weight = value;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Write the XML fragment describing the object.
        /// </summary>
        internal override void WriteSrgs(XmlWriter writer)
        {
            // Write <item weight="1.0" repeat-prob="0.5" repeat="m-n">
            writer.WriteStartElement("item");
            if (!_weight.Equals(1.0f))
            {
                writer.WriteAttributeString("weight", _weight.ToString("0.########", CultureInfo.InvariantCulture));
            }

            if (!_repeatProbability.Equals(0.5f))
            {
                writer.WriteAttributeString("repeat-prob", _repeatProbability.ToString("0.########", CultureInfo.InvariantCulture));
            }

            if (_minRepeat == _maxRepeat)
            {
                // could be because both value are NotSet of equal
                if (_minRepeat != NotSet)
                {
                    writer.WriteAttributeString("repeat", string.Format(CultureInfo.InvariantCulture, "{0}", _minRepeat));
                }
            }
            else if (_maxRepeat == int.MaxValue || _maxRepeat == NotSet)
            {
                // MinValue Set but not Max Value
                writer.WriteAttributeString("repeat", string.Format(CultureInfo.InvariantCulture, "{0}-", _minRepeat));
            }
            else
            {
                // Max Value Set and maybe MinValue
                int minRepeat = _minRepeat == NotSet ? 1 : _minRepeat;
                writer.WriteAttributeString("repeat", string.Format(CultureInfo.InvariantCulture, "{0}-{1}", minRepeat, _maxRepeat));
            }

            // Write <item> body and footer.
            Type previousElementType = null;

            foreach (SrgsElement element in _elements)
            {
                // Insert space between consecutive SrgsText _elements.
                Type elementType = element.GetType();

                if ((elementType == typeof(SrgsText)) && (elementType == previousElementType))
                {
                    writer.WriteString(" ");
                }

                previousElementType = elementType;
                element.WriteSrgs(writer);
            }

            writer.WriteEndElement();
        }

        internal override string DebuggerDisplayString()
        {
            StringBuilder sb = new();

            if (_elements.Count > 7)
            {
                sb.Append("SrgsItem Count = ");
                sb.Append(_elements.Count.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                if (_minRepeat != _maxRepeat || _maxRepeat != NotSet)
                {
                    sb.Append('[');
                    if (_minRepeat == _maxRepeat)
                    {
                        sb.Append(_minRepeat.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (_maxRepeat == int.MaxValue || _maxRepeat == NotSet)
                    {
                        // MinValue Set but not Max Value
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "{0},-", _minRepeat));
                    }
                    else
                    {
                        // Max Value Set and maybe MinValue
                        int minRepeat = _minRepeat == NotSet ? 1 : _minRepeat;
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "{0},{1}", minRepeat, _maxRepeat));
                    }
                    sb.Append("] ");
                }

                bool first = true;
                foreach (SrgsElement element in _elements)
                {
                    if (!first)
                    {
                        sb.Append(' ');
                    }
                    sb.Append('{');
                    sb.Append(element.DebuggerDisplayString());
                    sb.Append('}');
                    first = false;
                }
            }
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
                SrgsElement[] elements = new SrgsElement[_elements.Count];
                int i = 0;
                foreach (SrgsElement element in _elements)
                {
                    elements[i++] = element;
                }
                return elements;
            }
        }

        #endregion

        #region Private Methods

        #endregion

        #region Private Fields

        private float _weight = 1.0f;

        private float _repeatProbability = 0.5f;

        private int _minRepeat = NotSet;

        private int _maxRepeat = NotSet;

        private SrgsElementList _elements;

        private const int NotSet = -1;

        #endregion

        #region Private Types

        // Used by the debugger display attribute
        internal class SrgsItemDebugDisplay
        {
            public SrgsItemDebugDisplay(SrgsItem item)
            {
                _weight = item._weight;
                _repeatProbability = item._repeatProbability;
                _minRepeat = item._minRepeat;
                _maxRepeat = item._maxRepeat;
                _elements = item._elements;
            }

            public object Weight
            {
                get
                {
                    return _weight;
                }
            }

            public object MinRepeat
            {
                get
                {
                    return _minRepeat;
                }
            }

            public object MaxRepeat
            {
                get
                {
                    return _maxRepeat;
                }
            }

            public object RepeatProbability
            {
                get
                {
                    return _repeatProbability;
                }
            }

            public object Count
            {
                get
                {
                    return _elements.Count;
                }
            }
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public SrgsElement[] AKeys
            {
                get
                {
                    SrgsElement[] elements = new SrgsElement[_elements.Count];
                    for (int i = 0; i < _elements.Count; i++)
                    {
                        elements[i] = _elements[i];
                    }
                    return elements;
                }
            }

            private float _weight = 1.0f;
            private float _repeatProbability = 0.5f;
            private int _minRepeat = NotSet;
            private int _maxRepeat = NotSet;
            private SrgsElementList _elements;
        }

        #endregion
    }
}
