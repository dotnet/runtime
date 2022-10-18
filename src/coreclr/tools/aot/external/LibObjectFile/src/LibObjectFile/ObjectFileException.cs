// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile
{
    /// <summary>
    /// An exception used when diagnostics error are happening during read/write.
    /// </summary>
    public class ObjectFileException : Exception
    {
        public ObjectFileException(string message, DiagnosticBag diagnostics) : base(message)
        {
            
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public override string Message => base.Message + Environment.NewLine + Diagnostics;
        
        /// <summary>
        /// The associated diagnostics messages.
        /// </summary>
        public DiagnosticBag Diagnostics { get; }
    }
}