// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Represents a filter which allows only certain Unicode code points through.
    /// </summary>
    public class TextEncoderSettings
    {
        private AllowedBmpCodePointsBitmap _allowedCodePointsBitmap;

        /// <summary>
        /// Instantiates an empty filter (allows no code points through by default).
        /// </summary>
        public TextEncoderSettings()
        {
        }

        /// <summary>
        /// Instantiates the filter by cloning the allow list of another <see cref="TextEncoderSettings"/>.
        /// </summary>
        public TextEncoderSettings(TextEncoderSettings other)
        {
            if (other is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            _allowedCodePointsBitmap = other.GetAllowedCodePointsBitmap(); // copy byval
        }

        /// <summary>
        /// Instantiates the filter where only the character ranges specified by <paramref name="allowedRanges"/>
        /// are allowed by the filter.
        /// </summary>
        public TextEncoderSettings(params UnicodeRange[] allowedRanges)
        {
            if (allowedRanges is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.allowedRanges);
            }

            AllowRanges(allowedRanges);
        }

        /// <summary>
        /// Allows the character specified by <paramref name="character"/> through the filter.
        /// </summary>
        public virtual void AllowCharacter(char character)
        {
            _allowedCodePointsBitmap.AllowChar(character);
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="characters"/> through the filter.
        /// </summary>
        public virtual void AllowCharacters(params char[] characters)
        {
            if (characters is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.characters);
            }

            for (int i = 0; i < characters.Length; i++)
            {
                _allowedCodePointsBitmap.AllowChar(characters[i]);
            }
        }

        /// <summary>
        /// Allows all code points specified by <paramref name="codePoints"/>.
        /// </summary>
        public virtual void AllowCodePoints(IEnumerable<int> codePoints)
        {
            if (codePoints is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.codePoints);
            }

            foreach (var allowedCodePoint in codePoints)
            {
                // If the code point can't be represented as a BMP character, skip it.
                if (UnicodeUtility.IsBmpCodePoint((uint)allowedCodePoint))
                {
                    _allowedCodePointsBitmap.AllowChar((char)allowedCodePoint);
                }
            }
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="range"/> through the filter.
        /// </summary>
        public virtual void AllowRange(UnicodeRange range)
        {
            if (range is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.range);
            }

            int firstCodePoint = range.FirstCodePoint;
            int rangeSize = range.Length;
            for (int i = 0; i < rangeSize; i++)
            {
                int codePoint = firstCodePoint + i;
                UnicodeDebug.AssertIsBmpCodePoint((uint)codePoint); // UnicodeRange only supports BMP
                _allowedCodePointsBitmap.AllowChar((char)codePoint);
            }
        }

        /// <summary>
        /// Allows all characters specified by <paramref name="ranges"/> through the filter.
        /// </summary>
        public virtual void AllowRanges(params UnicodeRange[] ranges)
        {
            if (ranges is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.ranges);
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
            _allowedCodePointsBitmap = default;
        }

        /// <summary>
        /// Disallows the character <paramref name="character"/> through the filter.
        /// </summary>
        public virtual void ForbidCharacter(char character)
        {
            _allowedCodePointsBitmap.ForbidChar(character);
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="characters"/> through the filter.
        /// </summary>
        public virtual void ForbidCharacters(params char[] characters)
        {
            if (characters is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.characters);
            }

            for (int i = 0; i < characters.Length; i++)
            {
                _allowedCodePointsBitmap.ForbidChar(characters[i]);
            }
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="range"/> through the filter.
        /// </summary>
        public virtual void ForbidRange(UnicodeRange range)
        {
            if (range is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.range);
            }

            int firstCodePoint = range.FirstCodePoint;
            int rangeSize = range.Length;
            for (int i = 0; i < rangeSize; i++)
            {
                int codePoint = firstCodePoint + i;
                UnicodeDebug.AssertIsBmpCodePoint((uint)codePoint); // UnicodeRange only supports BMP
                _allowedCodePointsBitmap.ForbidChar((char)codePoint);
            }
        }

        /// <summary>
        /// Disallows all characters specified by <paramref name="ranges"/> through the filter.
        /// </summary>
        public virtual void ForbidRanges(params UnicodeRange[] ranges)
        {
            if (ranges is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.ranges);
            }

            for (int i = 0; i < ranges.Length; i++)
            {
                ForbidRange(ranges[i]);
            }
        }

        /// <summary>
        /// Gets an enumeration of all allowed code points.
        /// </summary>
        public virtual IEnumerable<int> GetAllowedCodePoints()
        {
            for (int i = 0; i <= char.MaxValue; i++)
            {
                if (_allowedCodePointsBitmap.IsCharAllowed((char)i))
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Retrieves the bitmap of allowed characters from this settings object.
        /// The data is returned readonly byref.
        /// </summary>
        internal ref readonly AllowedBmpCodePointsBitmap GetAllowedCodePointsBitmap()
        {
            if (GetType() == typeof(TextEncoderSettings))
            {
                return ref _allowedCodePointsBitmap;
            }
            else
            {
                // Somebody may have overridden GetAllowedCodePoints, and we need to honor that.
                // Fabricate a new bitmap and populate it from the virtual overrides.
                StrongBox<AllowedBmpCodePointsBitmap> newBitmap = new StrongBox<AllowedBmpCodePointsBitmap>();
                foreach (int allowedCodePoint in GetAllowedCodePoints())
                {
                    if ((uint)allowedCodePoint <= char.MaxValue)
                    {
                        newBitmap.Value.AllowChar((char)allowedCodePoint);
                    }
                }
                return ref newBitmap.Value;
            }
        }
    }
}
