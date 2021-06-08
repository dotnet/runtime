// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public sealed class FileStreamOptions
    {
        private FileMode? _mode;
        private FileAccess? _access;
        private FileShare _share = FileStream.DefaultShare;
        private FileOptions _options;
        private long _preallocationSize;
        private int _bufferSize = FileStream.DefaultBufferSize;

        /// <summary>
        /// One of the enumeration values that determines how to open or create the file.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">When <paramref name="value" /> contains an invalid value.</exception>
        /// <exception cref="T:System.ArgumentException">When <paramref name="value" /> is <see cref="System.IO.FileMode.Append" /> but <see cref="Access" />  is not <see cref="System.IO.FileAccess.Write" />.
        ///  -or-
        /// <see cref="Access" /> is <see cref="System.IO.FileAccess.Read" /> while given mode requires <see cref="System.IO.FileAccess.Write" />.
        /// </exception>
        public FileMode Mode
        {
            get => _mode.HasValue ? _mode.GetValueOrDefault() : FileMode.Open;
            set
            {
                if (value < FileMode.CreateNew || value > FileMode.Append)
                {
                    ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
                }
                else if (_access.HasValue && (_access & FileAccess.Read) != 0 && value == FileMode.Append)
                {
                    throw new ArgumentException(SR.Argument_InvalidAppendMode, nameof(value));
                }
                else if (_access.HasValue && (_access & FileAccess.Write) == 0)
                {
                    if (value == FileMode.Truncate || value == FileMode.CreateNew || value == FileMode.Create || value == FileMode.Append)
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidFileModeAndAccessCombo, value, _access), nameof(value));
                    }
                }

                _mode = value;
            }
        }

        /// <summary>
        /// A bitwise combination of the enumeration values that determines how the file can be accessed by the <see cref="FileStream" /> object. This also determines the values returned by the <see cref="System.IO.FileStream.CanRead" /> and <see cref="System.IO.FileStream.CanWrite" /> properties of the <see cref="FileStream" /> object.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">When <paramref name="value" /> contains an invalid value.</exception>
        /// <exception cref="T:System.ArgumentException">When <see cref="Mode" /> is <see cref="System.IO.FileMode.Append" /> but <paramref name="value" />  is not <see cref="System.IO.FileAccess.Write" />.
        ///  -or-
        /// <see cref="Mode" /> requires <see cref="System.IO.FileAccess.Write" /> while <paramref name="value" /> is <see cref="System.IO.FileAccess.Read" />.
        /// </exception>
        public FileAccess Access
        {
            get => _access.HasValue ? _access.GetValueOrDefault() : (_mode.HasValue && _mode == FileMode.Append) ? FileAccess.Write : FileAccess.ReadWrite;
            set
            {
                if (value < FileAccess.Read || value > FileAccess.ReadWrite)
                {
                    ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
                }
                else if ((value & FileAccess.Read) != 0 && _mode.HasValue && _mode == FileMode.Append)
                {
                    throw new ArgumentException(SR.Argument_InvalidAppendMode, nameof(value));
                }
                else if ((value & FileAccess.Write) == 0 && _mode.HasValue)
                {
                    if (_mode == FileMode.Truncate || _mode == FileMode.CreateNew || _mode == FileMode.Create || _mode == FileMode.Append)
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidFileModeAndAccessCombo, _mode, value), nameof(value));
                    }
                }

                _access = value;
            }
        }

        /// <summary>
        /// A bitwise combination of the enumeration values that determines how the file will be shared by processes. The default value is <see cref="System.IO.FileShare.Read" />.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">When <paramref name="value" /> contains an invalid value.</exception>
        public FileShare Share
        {
            get => _share;
            set
            {
                // don't include inheritable in our bounds check for share
                FileShare tempshare = value & ~FileShare.Inheritable;
                if (tempshare < FileShare.None || tempshare > (FileShare.ReadWrite | FileShare.Delete))
                {
                    ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
                }

                _share = value;
            }
        }

        /// <summary>
        /// A bitwise combination of the enumeration values that specifies additional file options. The default value is <see cref="System.IO.FileOptions.None" />, which indicates synchronous IO.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">When <paramref name="value" /> contains an invalid value.</exception>
        public FileOptions Options
        {
            get => _options;
            set
            {
                // NOTE: any change to FileOptions enum needs to be matched here in the error validation
                if (value != FileOptions.None && (value & ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Encrypted | (FileOptions)0x20000000 /* NoBuffering */)) != 0)
                {
                    ThrowHelper.ArgumentOutOfRangeException_Enum_Value();
                }

                _options = value;
            }
        }

        /// <summary>
        /// The initial allocation size in bytes for the file. A positive value is effective only when a regular file is being created, overwritten, or replaced.
        /// Negative values are not allowed.
        /// In other cases (including the default 0 value), it's ignored.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">When <paramref name="value" /> is negative.</exception>
        public long PreallocationSize
        {
            get => _preallocationSize;
            set => _preallocationSize = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);
        }

        /// <summary>
        /// The size of the buffer used by <see cref="FileStream" /> for buffering. The default buffer size is 4096.
        /// 0 or 1 means that buffering should be disabled. Negative values are not allowed.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">When <paramref name="value" /> is negative.</exception>
        public int BufferSize
        {
            get => _bufferSize;
            set => _bufferSize = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);
        }
    }
}
