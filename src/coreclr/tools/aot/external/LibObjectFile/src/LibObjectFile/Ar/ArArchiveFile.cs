// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibObjectFile.Ar
{
    /// <summary>
    /// An 'ar' archive file.
    /// </summary>
    public sealed class ArArchiveFile : ObjectFileNode
    {
        private readonly List<ArFile> _files;

        /// <summary>
        /// Gets the bytes <c>!&lt;arch&gt;\n</c> involved in the magic file header of an archive
        /// </summary>
        public static ReadOnlySpan<byte> Magic => new ReadOnlySpan<byte>(new byte[]
        {
            (byte)'!',
            (byte)'<',
            (byte)'a',
            (byte)'r',
            (byte)'c',
            (byte)'h',
            (byte)'>',
            (byte)'\n',
        });
        
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public ArArchiveFile()
        {
            _files = new List<ArFile>();
            Kind = ArArchiveKind.GNU;
        }

        /// <summary>
        /// Gets or sets the type of this archive file.
        /// </summary>
        public ArArchiveKind Kind { get; set; }

        /// <summary>
        /// Gets the <see cref="ArSymbolTable"/> associated to this instance. Must be first entry in <see cref="Files"/>
        /// </summary>
        public ArSymbolTable SymbolTable { get; private set; }

        /// <summary>
        /// Internal extended file names used for GNU entries.
        /// </summary>
        internal ArLongNamesTable LongNamesTable { get; set; }
        
        /// <summary>
        /// Gets the file entries. Use <see cref="AddFile"/> or <see cref="RemoveFile"/> to manipulate the entries.
        /// </summary>
        public IReadOnlyList<ArFile> Files => _files;
        
        /// <summary>
        /// Adds a file to <see cref="Files"/>.
        /// </summary>
        /// <param name="file">A file</param>
        public void AddFile(ArFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Parent != null)
            {
                if (file.Parent == this) throw new InvalidOperationException("Cannot add the file as it is already added");
                if (file.Parent != this) throw new InvalidOperationException($"Cannot add the file as it is already added to another {nameof(ArArchiveFile)} instance");
            }

            if (file is ArSymbolTable symbolTable)
            {
                InsertFileAt(0, file);
                return;
            }

            file.Parent = this;
            file.Index = (uint)_files.Count;
            _files.Add(file);
        }

        /// <summary>
        /// Inserts a file into <see cref="Files"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Files"/> to insert the specified file</param>
        /// <param name="file">The file to insert</param>
        public void InsertFileAt(int index, ArFile file)
        {
            if (index < 0 || index > _files.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_files.Count}");
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Parent != null)
            {
                if (file.Parent == this) throw new InvalidOperationException("Cannot add the file as it is already added");
                if (file.Parent != this) throw new InvalidOperationException($"Cannot add the file as it is already added to another {nameof(ArArchiveFile)} instance");
            }

            if (file is ArSymbolTable symbolTable)
            {
                if (SymbolTable == null)
                {
                    if (index != 0)
                    {
                        throw new ArgumentException($"Cannot only add a symbol table at index 0", nameof(file));
                    }
                    SymbolTable = symbolTable;
                }
                else
                {
                    throw new ArgumentException($"Cannot add this symbol table as an existing symbol table is already present in the file entries of {this}", nameof(file));
                }
            }
            else
            {
                if (SymbolTable != null && index == 0)
                {
                    throw new ArgumentException($"Cannot add the entry {file} at index 0 because a symbol table is already set and must be the first entry in the list of files", nameof(file));
                }
            }
            
            file.Index = (uint)index;
            _files.Insert(index, file);
            file.Parent = this;

            // Update the index of following files
            for (int i = index + 1; i < _files.Count; i++)
            {
                var nextFile = _files[i];
                nextFile.Index++;
            }
        }

        /// <summary>
        /// Removes a file from <see cref="Files"/>
        /// </summary>
        /// <param name="file">The file to remove</param>
        public void RemoveFile(ArFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (file.Parent != this)
            {
                throw new InvalidOperationException($"Cannot remove this file as it is not part of this {nameof(ArArchiveFile)} instance");
            }

            // If we are removing the SymbolTable
            if (file == SymbolTable)
            {
                SymbolTable = null;
            }

            var i = (int)file.Index;
            _files.RemoveAt(i);
            file.Index = 0;

            // Update indices for other sections
            for (int j = i + 1; j < _files.Count; j++)
            {
                var nextEntry = _files[j];
                nextEntry.Index--;
            }

            file.Parent = null;
        }

        /// <summary>
        /// Removes a file from <see cref="Files"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Files"/> to remove the specified file</param>
        public ArFile RemoveFileAt(int index)
        {
            if (index < 0 || index > _files.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_files.Count}");
            var file = _files[index];
            RemoveFile(file);
            return file;
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            for (var i = 0; i < Files.Count; i++)
            {
                var item = Files[i];
                if (item.Name == null)
                {
                    diagnostics.Error(DiagnosticId.AR_ERR_InvalidNullFileEntryName, $"Invalid null FileName for file entry [{i}] in {this}");
                }
                else if (Kind == ArArchiveKind.Common)
                {
                    var count = Encoding.UTF8.GetByteCount(item.Name);
                    if (count > ArFile.FieldNameLength)
                    {
                        diagnostics.Error(DiagnosticId.AR_ERR_InvalidFileEntryNameTooLong, $"Invalid length ({count} bytes) for file entry [{i}] {item.Name} in {this}. Must be <= {ArFile.FieldNameLength} for {Kind} ar file");
                    }
                }

                item.Verify(diagnostics);
            }
        }

        /// <summary>
        /// Detects from the specified stream if there is an 'ar' archive file header.
        /// </summary>
        /// <param name="stream">The stream to detect the presence of an 'ar' archive file header.</param>
        /// <returns><c>true</c> if an 'ar' archive file header was detected. <c>false</c> otherwise.</returns>
        public static bool IsAr(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var startPosition = stream.Position;
            var result = ArArchiveFileReader.IsAr(stream, null);
            stream.Position = startPosition;
            return result;
        }

        /// <summary>
        /// Detects from the specified stream if there is an 'ar' archive file header.
        /// </summary>
        /// <param name="stream">The stream to detect the presence of an 'ar' archive file header.</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <returns><c>true</c> if an 'ar' archive file header was detected. <c>false</c> otherwise.</returns>
        public static bool IsAr(Stream stream, out DiagnosticBag diagnostics)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var startPosition = stream.Position;
            diagnostics = new DiagnosticBag();
            var result = ArArchiveFileReader.IsAr(stream, diagnostics);
            stream.Position = startPosition;
            return result;
        }

        /// <summary>
        /// Reads an 'ar' archive file from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read 'ar' a archive file from.</param>
        /// <param name="archiveKind">The type of 'ar' archive to read.</param>
        /// <returns>An instance of <see cref="ArArchiveFile"/> if the read was successful.</returns>
        /// <exception cref="ObjectFileException">In case of an invalid file.</exception>
        public static ArArchiveFile Read(Stream stream, ArArchiveKind archiveKind)
        {
            return Read(stream, new ArArchiveFileReaderOptions(archiveKind));
        }

        /// <summary>
        /// Reads an 'ar' archive file from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read 'ar' a archive file from.</param>
        /// <param name="options">The options used for reading this 'ar' file.</param>
        /// <returns>An instance of <see cref="ArArchiveFile"/> if the read was successful.</returns>
        /// <exception cref="ObjectFileException">In case of an invalid file.</exception>
        public static ArArchiveFile Read(Stream stream, ArArchiveFileReaderOptions options)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!TryRead(stream, options, out var arFile, out var diagnostics))
            {
                throw new ObjectFileException("Invalid ar file", diagnostics);
            }

            return arFile;
        }

        /// <summary>
        /// Tries to reads an 'ar' archive file from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read 'ar' a archive file from.</param>
        /// <param name="archiveKind">The type of 'ar' archive to read.</param>
        /// <param name="arArchiveFile">The output 'ar' archive file if the read was successful.</param>
        /// <param name="diagnostics">The output associated diagnostics after reading the archive.</param>
        /// <returns><c>true</c> An instance of <see cref="ArArchiveFile"/> if the read was successful.</returns>
        public static bool TryRead(Stream stream, ArArchiveKind archiveKind, out ArArchiveFile arArchiveFile, out DiagnosticBag diagnostics)
        {
            return TryRead(stream, new ArArchiveFileReaderOptions(archiveKind), out arArchiveFile, out diagnostics);
        }

        /// <summary>
        /// Tries to reads an 'ar' archive file from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read 'ar' a archive file from.</param>
        /// <param name="options">The options used for reading this 'ar' file.</param>
        /// <param name="arArchiveFile">The output 'ar' archive file if the read was successful.</param>
        /// <param name="diagnostics">The output associated diagnostics after reading the archive.</param>
        /// <returns><c>true</c> An instance of <see cref="ArArchiveFile"/> if the read was successful.</returns>
        public static bool TryRead(Stream stream, ArArchiveFileReaderOptions options, out ArArchiveFile arArchiveFile, out DiagnosticBag diagnostics)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (options == null) throw new ArgumentNullException(nameof(options));

            arArchiveFile = new ArArchiveFile { Kind = options.ArchiveKind };
            var reader = new ArArchiveFileReader(arArchiveFile, stream, options);
            diagnostics = reader.Diagnostics;
            reader.Read();

            return !reader.Diagnostics.HasErrors;
        }

        /// <summary>
        /// Writes this 'ar' archive file to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var writer = new ArArchiveFileWriter(this, stream);
            writer.Write();
        }
        
        public void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            Size = 0;

            if (Kind == ArArchiveKind.GNU)
            {
                if (LongNamesTable == null)
                {
                    LongNamesTable = new ArLongNamesTable
                    {
                        Parent = this
                    };
                }

                if (SymbolTable != null && SymbolTable.Index == 0)
                {
                    LongNamesTable.Index = 1;
                }
                else
                {
                    LongNamesTable.Index = 1;
                }
            }
            else
            {
                // Don't use headers
                LongNamesTable = null;
            }

            ulong size = (ulong)Magic.Length;

            // Clear the internal names
            foreach (var entry in Files)
            {
                entry.InternalName = null;
            }

            for (var i = 0; i < Files.Count; i++)
            {
                var entry = Files[i];

                entry.UpdateLayout(diagnostics);
                if (diagnostics.HasErrors) return;

                // If we have a GNU headers and they are required, add them to the offset and size
                if (LongNamesTable != null && LongNamesTable.Index == i)
                {
                    LongNamesTable.UpdateLayout(diagnostics);
                    if (diagnostics.HasErrors) return;

                    var headerSize = LongNamesTable.Size;
                    if (headerSize > 0)
                    {
                        LongNamesTable.Offset = size;
                        size += ArFile.FileEntrySizeInBytes + LongNamesTable.Size;
                        if ((size & 1) != 0) size++;
                    }
                }
                
                entry.Offset = size;
                size += ArFile.FileEntrySizeInBytes + entry.Size;
                if ((size & 1) != 0) size++;
            }

            Size = size;
        }
    }
}