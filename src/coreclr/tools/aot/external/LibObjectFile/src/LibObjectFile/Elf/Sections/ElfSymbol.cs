// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A symbol entry in the <see cref="ElfSymbolTable"/>
    /// This is the value seen in <see cref="ElfNative.Elf32_Sym"/> or <see cref="ElfNative.Elf64_Sym"/>
    /// </summary>
    public struct ElfSymbol : IEquatable<ElfSymbol>
    {
        public static readonly ElfSymbol Empty = new ElfSymbol();

        /// <summary>
        /// Gets or sets the value associated to this symbol.
        /// </summary>
        public ulong Value { get; set; }

        /// <summary>
        /// Gets or sets the size of this symbol.
        /// </summary>
        public ulong Size { get; set; }

        /// <summary>
        /// Gets or sets the type of this symbol (e.g <see cref="ElfSymbolType.Function"/> or <see cref="ElfSymbolType.NoType"/>).
        /// </summary>
        public ElfSymbolType Type { get; set; }

        /// <summary>
        /// Get or sets the binding applying to this symbol (e.g <see cref="ElfSymbolBind.Global"/> or <see cref="ElfSymbolBind.Local"/>).
        /// </summary>
        public ElfSymbolBind Bind { get; set; }

        /// <summary>
        /// Gets or sets the visibility of this symbol (e.g <see cref="ElfSymbolVisibility.Hidden"/>)
        /// </summary>
        public ElfSymbolVisibility Visibility { get; set; }

        /// <summary>
        /// Gets or sets the associated section to this symbol.
        /// </summary>
        public ElfSectionLink Section { get; set; }

        /// <summary>
        /// Gets or sets the name of this symbol.
        /// </summary>
        public ElfString Name { get; set; }

        public override string ToString()
        {
            return $"{nameof(Value)}: 0x{Value:X16}, {nameof(Size)}: {Size:#####}, {nameof(Type)}: {Type}, {nameof(Bind)}: {Bind}, {nameof(Visibility)}: {Visibility}, {nameof(Section)}: {Section}, {nameof(Name)}: {Name}";
        }

        public bool Equals(ElfSymbol other)
        {
            return Value == other.Value && Size == other.Size && Type == other.Type && Bind == other.Bind && Visibility == other.Visibility && Section.Equals(other.Section) && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is ElfSymbol other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Value.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Type;
                hashCode = (hashCode * 397) ^ (int) Bind;
                hashCode = (hashCode * 397) ^ (int) Visibility;
                hashCode = (hashCode * 397) ^ Section.GetHashCode();
                hashCode = (hashCode * 397) ^ Name.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ElfSymbol left, ElfSymbol right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ElfSymbol left, ElfSymbol right)
        {
            return !left.Equals(right);
        }
    }
}