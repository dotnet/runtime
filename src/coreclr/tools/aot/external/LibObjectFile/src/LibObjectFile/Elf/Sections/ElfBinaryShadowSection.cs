// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.IO;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Equivalent of <see cref="ElfBinarySection"/> but used for shadow.
    /// </summary>
    public sealed class ElfBinaryShadowSection : ElfShadowSection
    {
        public ElfBinaryShadowSection()
        {
        }

        public Stream Stream { get; set; }

        protected override void Read(ElfReader reader)
        {
            Stream = reader.ReadAsStream(Size);
        }

        protected override void Write(ElfWriter writer)
        {
            if (Stream == null) return;
            writer.Write(Stream);
        }

        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            Size = Stream != null ? (ulong)Stream.Length : 0;
        }
    }
}