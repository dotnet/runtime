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
    public class CodePointFilter
    {
        private AllowedCharactersBitmap _allowedCharactersBitmap;

        /// <summary>
        /// Instantiates an empty filter (allows no code points through by default).
        /// </summary>
        public CodePointFilter()
        {
            _allowedCharactersBitmap = AllowedCharactersBitmap.CreateNew();
        }

        /// <summary>
        /// Instantiates the filter by cloning the allow list of another <see cref="CodePointFilter"/>.
        /// </summary>
        public CodePointFilter(CodePointFilter other)
        {
            _allowedCharactersBitmap = AllowedCharactersBitmap.CreateNew();
            AllowFilter(other);
        }

        /// <summary>
        /// Instantiates the filter where only the character ranges specified by <paramref name="allowedRanges"/>
        /// are allowed by the filter.
        /// </summary>
        public CodePointFilter(params UnicodeRange[] allowedRanges)
        {
            if(allowedRanges == null)
            {
                throw new ArgumentNullException("allowedRanges");
            }
            _allowedCharactersBitmap = AllowedCharactersBitmap.CreateNew();
            AllowRanges(allowedRanges);
        }

        /// <summary>
        /// Allows the character specified by <paramref name="character"/> through the filter.
        /// </summary>
        public virtual void AllowCharacter(char character)
        {
            _allowedCharactersBitmap.AllowCharacter(character);
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="characters"/> through the filter.
        /// </summary>
        public virtual void AllowCharacters(params char[] characters)
        {
            if (characters == null)
            {
                throw new ArgumentNullException("characters");
            }

            for (int i = 0; i < characters.Length; i++)
            {
                _allowedCharactersBitmap.AllowCharacter(characters[i]);
            }
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="filter"/> through the filter.
        /// </summary>
        public virtual void AllowFilter(CodePointFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            foreach (var allowedCodePoint in filter.GetAllowedCodePoints())
            {
                // If the code point can't be represented as a BMP character, skip it.
                char codePointAsChar = (char)allowedCodePoint;
                if (allowedCodePoint == codePointAsChar)
                {
                    _allowedCharactersBitmap.AllowCharacter(codePointAsChar);
                }
            }
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="range"/> through the filter.
        /// </summary>
        public virtual void AllowRange(UnicodeRange range)
        {
            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            int firstCodePoint = range.FirstCodePoint;
            int rangeSize = range.Length;
            for (int i = 0; i < rangeSize; i++)
            {
                _allowedCharactersBitmap.AllowCharacter((char)(firstCodePoint + i));
            }
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="ranges"/> through the filter.
        /// </summary>
        public virtual void AllowRanges(params UnicodeRange[] ranges)
        {
            if (ranges == null)
            {
                throw new ArgumentNullException("ranges");
            }

            for (int i = 0; i < ranges.Length; i++)
            {
                AllowRange(ranges[i]);
            }
        }

        /// <summary>
        /// Resets this filter by disallowing all characters.
        /// </summary>
        public virtual void Clear()
        {
            _allowedCharactersBitmap.Clear();
        }

        /// <summary>
        /// Disallows the character <paramref name="character"/> through the filter.
        /// </summary>
        public virtual void ForbidCharacter(char character)
        {
            _allowedCharactersBitmap.ForbidCharacter(character);
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="characters"/> through the filter.
        /// </summary>
        public virtual void ForbidCharacters(params char[] characters)
        {
            if (characters == null)
            {
                throw new ArgumentNullException("characters");
            }

            for (int i = 0; i < characters.Length; i++)
            {
                _allowedCharactersBitmap.ForbidCharacter(characters[i]);
            }
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="range"/> through the filter.
        /// </summary>
        public virtual void ForbidRange(UnicodeRange range)
        {
            if (range == null)
            {
                throw new ArgumentNullException("range");
            }

            int firstCodePoint = range.FirstCodePoint;
            int rangeSize = range.Length;
            for (int i = 0; i < rangeSize; i++)
            {
                _allowedCharactersBitmap.ForbidCharacter((char)(firstCodePoint + i));
            }
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="ranges"/> through the filter.
        /// </summary>
        public virtual void ForbidRanges(params UnicodeRange[] ranges)
        {
            if (ranges == null)
            {
                throw new ArgumentNullException("ranges");
            }

            for (int i = 0; i < ranges.Length; i++)
            {
                ForbidRange(ranges[i]);
            }
        }

        /// <summary>
        /// Retrieves the bitmap of allowed characters from this filter.
        /// The returned bitmap is a clone of the original bitmap to avoid unintentional modification.
        /// </summary>
        internal AllowedCharactersBitmap GetAllowedCharacters()
        {
            return _allowedCharactersBitmap.Clone();
        }

        /// <summary>
        /// Gets an enumeration of all allowed code points.
        /// </summary>
        public virtual IEnumerable<int> GetAllowedCodePoints()
        {
            for (int i = 0; i < 0x10000; i++)
            {
                if (_allowedCharactersBitmap.IsCharacterAllowed((char)i))
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Returns a value stating whether the character <paramref name="character"/> is allowed through the filter.
        /// </summary>
        public virtual bool IsCharacterAllowed(char character)
        {
            return _allowedCharactersBitmap.IsCharacterAllowed(character);
        }

        /// <summary>
        /// Wraps the provided filter as a CodePointFilter, avoiding the clone if possible.
        /// </summary>
        internal static CodePointFilter Wrap(CodePointFilter filter)
        {
            return (filter as CodePointFilter) ?? new CodePointFilter(filter);
        }
    }
}
