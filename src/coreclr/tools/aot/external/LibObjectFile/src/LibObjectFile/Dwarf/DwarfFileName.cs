// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfFileName
    {
        public string Name { get; set; }

        public string Directory { get; set; }
        
        public ulong Time { get; set; }

        public ulong Size { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name)) return "<empty>";
            if (Directory != null)
            {
                return Directory.Contains(Path.AltDirectorySeparatorChar) ? $"{Directory}{Path.AltDirectorySeparatorChar}{Name}" : $"{Directory}{Path.DirectorySeparatorChar}{Name}";
            }

            return Name;
        }
    }
}