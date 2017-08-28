// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Internal;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Represents a filter which allows only certain Unicode code points through.
    /// </summary>
    public class TextEncoderSettings
    {
        private AllowedCharactersBitmap _allowedCharactersBitmap;

        /// <summary>
        /// Instantiates an empty filter (allows no code points through by default).
        /// </summary>
        public TextEncoderSettings()
        {
            _allowedCharactersBitmap = AllowedCharactersBitmap.CreateNew();
        }

        /// <summary>
        /// Instantiates the filter by cloning the allow list of another <see cref="TextEncoderSettings"/>.
        /// </summary>
        public TextEncoderSettings(TextEncoderSettings other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _allowedCharactersBitmap = AllowedCharactersBitmap.CreateNew();
            AllowCodePoints(other.GetAllowedCodePoints());
        }

        /// <summary>
        /// Instantiates the filter where only the character ranges specified by <paramref name="allowedRanges"/>
        /// are allowed by the filter.
        /// </summary>
        public TextEncoderSettings(params UnicodeRange[] allowedRanges)
        {
            if(allowedRanges == null)
            {
                throw new ArgumentNullException(nameof(allowedRanges));
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
                throw new ArgumentNullException(nameof(characters));
            }

            for (int i = 0; i < characters.Length; i++)
            {
                _allowedCharactersBitmap.AllowCharacter(characters[i]);
            }
        }

        /// <summary>
        /// Allows all code points specified by <paramref name="codePoints"/>.
        /// </summary>
        public virtual void AllowCodePoints(IEnumerable<int> codePoints)
        {
            if (codePoints == null)
            {
                throw new ArgumentNullException(nameof(codePoints));
            }

            foreach (var allowedCodePoint in codePoints)
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
                throw new ArgumentNullException(nameof(range));
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
                throw new ArgumentNullException(nameof(ranges));
            }

            for (int i = 0; i < ranges.Length; i++)
            {
                AllowRange(ranges[i]);
            }
        }

        /// <summary>
        /// Resets this settings object by disallowing all characters.
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
                throw new ArgumentNullException(nameof(characters));
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
                throw new ArgumentNullException(nameof(range));
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
                throw new ArgumentNullException(nameof(ranges));
            }

            for (int i = 0; i < ranges.Length; i++)
            {
                ForbidRange(ranges[i]);
            }
        }

        /// <summary>
        /// Retrieves the bitmap of allowed characters from this settings object.
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
    }
}
