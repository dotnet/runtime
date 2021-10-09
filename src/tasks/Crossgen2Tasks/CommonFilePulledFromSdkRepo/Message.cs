// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Build.Tasks
{
    internal readonly struct Message
    {
        public readonly MessageLevel Level;
        public readonly string Code;
        public readonly string Text;
        public readonly string File;

        public Message(
            MessageLevel level,
            string text,
            string code = default,
            string file = default)
        {
            Level = level;
            Code = code;
            Text = text;
            File = file;
        }
    }
}
