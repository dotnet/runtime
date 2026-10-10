// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal ref struct ParsedMount
        {
            public required ReadOnlySpan<char> Root { get; init; }
            public required ReadOnlySpan<char> MountPoint { get; init; }
            public required ReadOnlySpan<char> FileSystemType { get; init; }
            public required ReadOnlySpan<char> SuperOptions { get; init; }
        }

        internal static bool TryParseMountInfoLine(ReadOnlySpan<char> line, out ParsedMount result)
        {
            result = default;

            // See man page for /proc/[pid]/mountinfo for details, e.g.:
            //     (1)(2)(3)   (4)   (5)      (6)      (7)   (8) (9)   (10)         (11)
            //     36 35 98:0 /mnt1 /mnt2 rw,noatime master:1 - ext3 /dev/root rw,errors=continue
            // but (7) is optional and could exist as multiple fields; the (8) separator marks
            // the end of the optional values.

            MemoryExtensions.SpanSplitEnumerator<char> fields = line.Split(' ');

            // (1) mount ID
            // (2) parent ID
            // (3) major:minor
            if (!fields.MoveNext() || !fields.MoveNext() || !fields.MoveNext())
            {
                return false;
            }

            // (4) root
            if (!fields.MoveNext())
            {
                return false;
            }
            ReadOnlySpan<char> root = line[fields.Current];

            // (5) mount point
            if (!fields.MoveNext())
            {
                return false;
            }
            ReadOnlySpan<char> mountPoint = line[fields.Current];

            // (8) separator
            const string Separator = " - ";
            int endOfOptionalFields = line.IndexOf(Separator, StringComparison.Ordinal);
            if (endOfOptionalFields == -1)
            {
                return false;
            }
            line = line.Slice(endOfOptionalFields + Separator.Length);
            fields = line.Split(' ');

            // (9) filesystem type
            if (!fields.MoveNext())
            {
                return false;
            }
            ReadOnlySpan<char> fileSystemType = line[fields.Current];

            // (10) mount source
            if (!fields.MoveNext())
            {
                return false;
            }

            // (11) super options
            if (!fields.MoveNext())
            {
                return false;
            }
            ReadOnlySpan<char> superOptions = line[fields.Current];

            result = new ParsedMount()
            {
                Root = root,
                MountPoint = mountPoint,
                FileSystemType = fileSystemType,
                SuperOptions = superOptions
            };
            return true;
        }

        internal static string DecodeMountInfoPath(ReadOnlySpan<char> path)
        {
            int backslashIndex = path.IndexOf('\\');
            if (backslashIndex < 0)
            {
                return path.ToString();
            }

            StringBuilder decodedPath = new(path.Length);
            decodedPath.Append(path.Slice(0, backslashIndex));

            for (int i = backslashIndex; i < path.Length; i++)
            {
                if (TryDecodeMountInfoEscape(path, i, out char decodedCharacter))
                {
                    decodedPath.Append(decodedCharacter);
                    i += 3;
                }
                else
                {
                    decodedPath.Append(path[i]);
                }
            }

            return decodedPath.ToString();
        }

        internal static bool MountInfoPathStartsWith(ReadOnlySpan<char> encodedPath, ReadOnlySpan<char> path, out int decodedLength)
        {
            int pathIndex = 0;

            for (int encodedIndex = 0; encodedIndex < encodedPath.Length; encodedIndex++, pathIndex++)
            {
                char decodedCharacter;
                if (TryDecodeMountInfoEscape(encodedPath, encodedIndex, out decodedCharacter))
                {
                    encodedIndex += 3;
                }
                else
                {
                    decodedCharacter = encodedPath[encodedIndex];
                }

                if ((uint)pathIndex >= (uint)path.Length || path[pathIndex] != decodedCharacter)
                {
                    decodedLength = 0;
                    return false;
                }
            }

            decodedLength = pathIndex;
            return true;
        }

        private static bool TryDecodeMountInfoEscape(ReadOnlySpan<char> path, int index, out char decodedCharacter)
        {
            decodedCharacter = path[index] == '\\' && index + 3 < path.Length
                ? path.Slice(index, 4) switch
                {
                    "\\040" => ' ',
                    "\\011" => '\t',
                    "\\012" => '\n',
                    "\\134" => '\\',
                    _ => '\0'
                }
                : '\0';

            return decodedCharacter != '\0';
        }
    }
}
