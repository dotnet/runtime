// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Unicode;

namespace GenUnicodeProp
{
    /// <summary>
    /// Contains information about a code point's Unicode category,
    /// bidi class, and simple case mapping / folding.
    /// </summary>
    internal sealed class CategoryCasingInfo : IEquatable<CategoryCasingInfo>
    {
        private readonly (UnicodeCategory generalCategory,
            StrongBidiCategory strongBidiCategory,
            ushort offsetToSimpleUppercase,
            ushort offsetToSimpleLowercase,
            ushort offsetToSimpleTitlecase,
            ushort offsetToSimpleCasefold,
            bool isWhitespace) _data;

        public CategoryCasingInfo(CodePoint codePoint)
        {
            _data.generalCategory = codePoint.GeneralCategory;

            switch (codePoint.BidiClass)
            {
                case BidiClass.Left_To_Right:
                    _data.strongBidiCategory = StrongBidiCategory.StrongLeftToRight;
                    break;

                case BidiClass.Right_To_Left:
                case BidiClass.Arabic_Letter:
                    _data.strongBidiCategory = StrongBidiCategory.StrongRightToLeft;
                    break;

                default:
                    _data.strongBidiCategory = StrongBidiCategory.Other;
                    break;
            }

            // For compatibility reasons we are not mapping the Turkish I's nor Latin small letter long S with invariant casing.
            if (Program.IncludeCasingData && codePoint.Value != 0x0130 && codePoint.Value != 0x0131 && codePoint.Value != 0x017f)
            {
                _data.offsetToSimpleUppercase = (ushort)(codePoint.SimpleUppercaseMapping - codePoint.Value);
                _data.offsetToSimpleLowercase = (ushort)(codePoint.SimpleLowercaseMapping - codePoint.Value);
                _data.offsetToSimpleTitlecase = (ushort)(codePoint.SimpleTitlecaseMapping - codePoint.Value);
                _data.offsetToSimpleCasefold = (ushort)(codePoint.SimpleCaseFoldMapping - codePoint.Value);
            }
            else
            {
                _data.offsetToSimpleUppercase = default;
                _data.offsetToSimpleLowercase = default;
                _data.offsetToSimpleTitlecase = default;
                _data.offsetToSimpleCasefold = default;
            }

            _data.isWhitespace = codePoint.Flags.HasFlag(CodePointFlags.White_Space);
        }

        public override bool Equals(object obj) => Equals(obj as CategoryCasingInfo);

        public bool Equals(CategoryCasingInfo other)
        {
            return !(other is null) && this._data.Equals(other._data);
        }

        public override int GetHashCode()
        {
            return _data.GetHashCode();
        }

        public static byte[] ToCategoryBytes(CategoryCasingInfo input)
        {
            // We're storing 3 pieces of information in 8 bits:
            // bit 7 (high bit) = isWhitespace?
            // bits 6..5 = restricted bidi class
            // bits 4..0 = Unicode category

            int combinedValue = Convert.ToInt32(input._data.isWhitespace) << 7;
            combinedValue += (int)input._data.strongBidiCategory << 5;
            combinedValue += (int)input._data.generalCategory;

            return new byte[] { checked((byte)combinedValue) };
        }

        public static byte[] ToUpperBytes(CategoryCasingInfo input)
        {
            byte[] bytes = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, input._data.offsetToSimpleUppercase);
            return bytes;
        }

        public static byte[] ToLowerBytes(CategoryCasingInfo input)
        {
            byte[] bytes = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, input._data.offsetToSimpleLowercase);
            return bytes;
        }

        public static byte[] ToTitleBytes(CategoryCasingInfo input)
        {
            byte[] bytes = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, input._data.offsetToSimpleTitlecase);
            return bytes;
        }

        public static byte[] ToCaseFoldBytes(CategoryCasingInfo input)
        {
            byte[] bytes = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, input._data.offsetToSimpleCasefold);
            return bytes;
        }
    }
}
