// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Internal;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Represents a filter which allows only certain Unicode code points through.
    /// </summary>
    public sealed class CodePointFilter : ICodePointFilter
    {
        private AllowedCharsBitmap _allowedCharsBitmap;

        /// <summary>
        /// Instantiates an empty filter (allows no code points through by default).
        /// </summary>
        public CodePointFilter()
        {
            _allowedCharsBitmap = AllowedCharsBitmap.CreateNew();
        }

        /// <summary>
        /// Instantiates the filter by cloning the allow list of another <see cref="ICodePointFilter"/>.
        /// </summary>
        public CodePointFilter(ICodePointFilter other)
        {
            CodePointFilter otherAsCodePointFilter = other as CodePointFilter;
            if (otherAsCodePointFilter != null)
            {
                _allowedCharsBitmap = otherAsCodePointFilter.GetAllowedCharsBitmap();
            }
            else
            {
                _allowedCharsBitmap = AllowedCharsBitmap.CreateNew();
                AllowFilter(other);
            }
        }

        /// <summary>
        /// Instantiates the filter where only the character ranges specified by <paramref name="allowedRanges"/>
        /// are allowed by the filter.
        /// </summary>
        public CodePointFilter(params UnicodeRange[] allowedRanges)
        {
            _allowedCharsBitmap = AllowedCharsBitmap.CreateNew();
            AllowRanges(allowedRanges);
        }

        /// <summary>
        /// Allows the character specified by <paramref name="character"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowChar(char character)
        {
            _allowedCharsBitmap.AllowCharacter(character);
            return this;
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="characters"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowChars(params char[] characters)
        {
            if (characters != null)
            {
                for (int i = 0; i < characters.Length; i++)
                {
                    _allowedCharsBitmap.AllowCharacter(characters[i]);
                }
            }
            return this;
        }

        /// <summary>
        /// Allows all characters in the string <paramref name="characters"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowChars(string characters)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                _allowedCharsBitmap.AllowCharacter(characters[i]);
            }
            return this;
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="filter"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowFilter(ICodePointFilter filter)
        {
            foreach (var allowedCodePoint in filter.GetAllowedCodePoints())
            {
                // If the code point can't be represented as a BMP character, skip it.
                char codePointAsChar = (char)allowedCodePoint;
                if (allowedCodePoint == codePointAsChar)
                {
                    _allowedCharsBitmap.AllowCharacter(codePointAsChar);
                }
            }
            return this;
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="range"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowRange(UnicodeRange range)
        {
            int firstCodePoint = range.FirstCodePoint;
            int rangeSize = range.RangeSize;
            for (int i = 0; i < rangeSize; i++)
            {
                _allowedCharsBitmap.AllowCharacter((char)(firstCodePoint + i));
            }
            return this;
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="ranges"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter AllowRanges(params UnicodeRange[] ranges)
        {
            if (ranges != null)
            {
                for (int i = 0; i < ranges.Length; i++)
                {
                    AllowRange(ranges[i]);
                }
            }
            return this;
        }

        /// <summary>
        /// Resets this filter by disallowing all characters.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter Clear()
        {
            _allowedCharsBitmap.Clear();
            return this;
        }

        /// <summary>
        /// Disallows the character <paramref name="character"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidChar(char character)
        {
            _allowedCharsBitmap.ForbidCharacter(character);
            return this;
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="characters"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidChars(params char[] characters)
        {
            if (characters != null)
            {
                for (int i = 0; i < characters.Length; i++)
                {
                    _allowedCharsBitmap.ForbidCharacter(characters[i]);
                }
            }
            return this;
        }

        /// <summary>
        /// Disallows all characters in the string <paramref name="characters"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidChars(string characters)
        {
            for (int i = 0; i < characters.Length; i++)
            {
                _allowedCharsBitmap.ForbidCharacter(characters[i]);
            }
            return this;
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="range"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidRange(UnicodeRange range)
        {
            int firstCodePoint = range.FirstCodePoint;
            int rangeSize = range.RangeSize;
            for (int i = 0; i < rangeSize; i++)
            {
                _allowedCharsBitmap.ForbidCharacter((char)(firstCodePoint + i));
            }
            return this;
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="ranges"/> through the filter.
        /// </summary>
        /// <returns>
        /// The 'this' instance.
        /// </returns>
        public CodePointFilter ForbidRanges(params UnicodeRange[] ranges)
        {
            if (ranges != null)
            {
                for (int i = 0; i < ranges.Length; i++)
                {
                    ForbidRange(ranges[i]);
                }
            }
            return this;
        }

        /// <summary>
        /// Retrieves the bitmap of allowed characters from this filter.
        /// The returned bitmap is a clone of the original bitmap to avoid unintentional modification.
        /// </summary>
        internal AllowedCharsBitmap GetAllowedCharsBitmap()
        {
            return _allowedCharsBitmap.Clone();
        }

        /// <summary>
        /// Gets an enumeration of all allowed code points.
        /// </summary>
        public IEnumerable<int> GetAllowedCodePoints()
        {
            for (int i = 0; i < 0x10000; i++)
            {
                if (_allowedCharsBitmap.IsCharacterAllowed((char)i))
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Returns a value stating whether the character <paramref name="character"/> is allowed through the filter.
        /// </summary>
        public bool IsCharacterAllowed(char character)
        {
            return _allowedCharsBitmap.IsCharacterAllowed(character);
        }

        /// <summary>
        /// Wraps the provided filter as a CodePointFilter, avoiding the clone if possible.
        /// </summary>
        internal static CodePointFilter Wrap(ICodePointFilter filter)
        {
            return (filter as CodePointFilter) ?? new CodePointFilter(filter);
        }
    }
}
