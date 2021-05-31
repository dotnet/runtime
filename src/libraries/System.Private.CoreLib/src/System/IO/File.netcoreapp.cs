// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public static partial class File
    {
        public static StreamReader OpenText(string path, FileStreamOptions options)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return new StreamReader(path, Override(options, FileMode.Open));
        }

        public static StreamWriter CreateText(string path, FileStreamOptions options)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return new StreamWriter(path, Override(options, FileMode.Create));
        }

        public static StreamWriter AppendText(string path, FileStreamOptions options)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return new StreamWriter(path, Override(options, FileMode.Append));
        }

        public static FileStream Create(string path, FileStreamOptions options)
            => new FileStream(path, Override(options, FileMode.Create));

        public static FileStream Open(string path, FileStreamOptions options)
        {
            return new FileStream(path, options);
        }

        public static FileStream OpenRead(string path, FileStreamOptions options)
        {
            return new FileStream(path, Override(options, FileMode.Open, FileAccess.Read));
        }

        public static FileStream OpenWrite(string path, FileStreamOptions options)
        {
            return new FileStream(path, Override(options, FileMode.OpenOrCreate, FileAccess.Write));
        }

        private static FileStreamOptions Override(FileStreamOptions options, FileMode? fileMode = default, FileAccess? fileAccess = default)
        {
            FileMode overriddenMode = fileMode ?? options.Mode;
            FileAccess overriddenAccess = fileAccess ?? options.Access;

            if (overriddenMode == options.Mode && overriddenAccess == options.Access)
                return options;

            return new FileStreamOptions
            {
                Mode = overriddenMode,
                Access = overriddenAccess,
                Share = options.Share,
                BufferSize = options.BufferSize,
                Options = options.Options,
                PreallocationSize = options.PreallocationSize
            };
        }
    }
}
