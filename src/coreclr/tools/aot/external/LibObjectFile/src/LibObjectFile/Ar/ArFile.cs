// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Ar
{
    /// <summary>
    /// Base class for a file entry in <see cref="ArArchiveFile.Files"/>
    /// </summary>
    public abstract partial class ArFile : ArObject
    {
        private string _name;
        private DateTimeOffset _timestamp;

        protected ArFile()
        {
            Timestamp = DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// Gets or sets the name of the file in the archive entry.
        /// </summary>
        public virtual string Name
        {
            get => _name;
            set
            {
                if (IsSystem)
                {
                    throw CannotModifyProperty(nameof(Name));
                }

                if (value != null && value.Contains('/'))
                {
                    throw new ArgumentException("The character `/` is not allowed in a file name entry");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Gets or sets the real (internal) name used for storing this entry (used by <see cref="ArLongNamesTable"/>)
        /// </summary>
        internal string InternalName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of this file (clamped to seconds since 1970/01/01)
        /// </summary>
        public DateTimeOffset Timestamp
        {
            get => _timestamp;

            // We clamp the timestamp to the precision supported by the system
            set => _timestamp = DateTimeOffset.FromUnixTimeSeconds(value.ToUnixTimeSeconds());
        }

        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        public uint OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the group id.
        /// </summary>
        public uint GroupId { get; set; }

        /// <summary>
        /// Gets or sets the file mode.
        /// </summary>
        public uint FileMode { get; set; }

        /// <summary>
        /// Gets a boolean indicating if this entry is a system entry (symbol table, header references)
        /// and so does not respect naming (that should exclude for example `/`)
        /// </summary>
        public virtual bool IsSystem => false;

        internal void AfterReadInternal(DiagnosticBag diagnostics)
        {
            AfterRead(diagnostics);
        }

        internal void ReadInternal(ArArchiveFileReader reader)
        {
            var expectedSize = (long)Size;
            var beforePosition = reader.Stream.Position;
            Read(reader);
            var afterPosition = reader.Stream.Position;
            var size = afterPosition - beforePosition;
            // Verifies that the Size property is actually valid with what is being read
            if (size != expectedSize)
            {
                reader.Diagnostics.Error(DiagnosticId.CMN_ERR_UnexpectedEndOfFile, $"Unexpected EOF / size (expected: {expectedSize} != read: {size})  while trying to read file entry {Name}");
            }
        }

        internal void WriteInternal(ArArchiveFileWriter writer)
        {
            var expectedSize = (long)Size;
            var beforePosition = writer.Stream.Position;
            Write(writer);
            var afterPosition = writer.Stream.Position;
            var size = afterPosition - beforePosition;

            // Verifies that the Size property is actually valid with what is being written
            if (size != expectedSize)
            {
                // In that case, we don't log a diagnostics but throw an error, as it is an implementation problem.
                throw new InvalidOperationException($"Invalid implementation of {GetType()}.{nameof(Write)} method. The Size written to the disk doesn't match (expected: {expectedSize} != written: {size}) while trying to write file entry {Name}");
            }
        }

        /// <summary>
        /// Reads this entry from a stream.
        /// </summary>
        /// <param name="reader">The reader associated with the stream to read from.</param>
        protected abstract void Read(ArArchiveFileReader reader);

        /// <summary>
        /// Performs after-read operation after all the other entries have been loaded.
        /// </summary>
        /// <param name="diagnostics">A diagnostic bag</param>
        protected virtual void AfterRead(DiagnosticBag diagnostics) { }

        /// <summary>
        /// Writes this entry to a stream.
        /// </summary>
        /// <param name="writer">The writer associated with the stream to write to.</param>
        protected abstract void Write(ArArchiveFileWriter writer);

        protected InvalidOperationException CannotModifyProperty(string propertyName)
        {
            return new InvalidOperationException($"Cannot modify the property {propertyName} for this {GetType()} file entry instance");
        }

        public override string ToString()
        {
            return $"{this.GetType().Name} [{Index}] `{Name}`";
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
        }
    }
}