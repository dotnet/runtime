// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// Session state of the user options
    /// </summary>
    public class TDSSessionStateUserOptionsOption : TDSSessionStateOption
    {
        #region Flags

        /// <summary>
        /// Bit that corresponds to ansi warnings flag
        /// </summary>
        private const int OPT_ANSI_WARNINGS = 0x10000000;

        /// <summary>
        /// Bit that corresponds to ansi nulls flag
        /// </summary>
        private const int OPT_ANSI_NULLS = 0x4000000;

        /// <summary>
        /// Bit that corresponds to cursor close on commit flag
        /// </summary>
        private const int OPT_CURSOR_COMMIT_CLOSE = 0x2000000;

        /// <summary>
        /// Bit that corresponds to quoted identifier flag
        /// </summary>
        private const int OPT_QUOTEDIDENT = 0x800000;

        /// <summary>
        /// Bit that corresponds to concatenation of nulls flag
        /// </summary>
        private const int OPT_CATNULL = 0x10000;

        /// <summary>
        /// Bit that corresponds to default position of ansi nulls flag
        /// </summary>
        private const int OPT_ANSINULLDFLTON = 0x4000;

        /// <summary>
        /// Bit that corresponds to ansi padding flag
        /// </summary>
        private const int OPT_ANSI_PADDING = 0x2000;

        /// <summary>
        /// Bit that corresponds to arithmetic abort flag
        /// </summary>
        private const int OPT_ARITHABORT = 0x1000;

        /// <summary>
        /// Bit that corresponds to abort truncation of numeric scale
        /// </summary>
        private const int OPT_NUMEABORT = 0x800;

        /// <summary>
        /// Bit that corresponds to transaction abort flag
        /// </summary>
        private const int OPT_XACTABORT = 0x10;

        /// <summary>
        /// Bit that corresponds to no count flag
        /// </summary>
        private const int OPT_NOCOUNT = 0x4;

        /// <summary>
        /// Bit that corresponds to arithmetic ignore flag
        /// </summary>
        private const int OPT_ARITHIGN = 0x2;

        /// <summary>
        /// Bit that corresponds to implicit transactions flag
        /// </summary>
        private const int OPT2_IMPLICIT_XACT = 0x02;

        #endregion

        /// <summary>
        /// Identifier of the session state option
        /// </summary>
        public const byte ID = 0;

        /// <summary>
        /// Affects the nullability of new columns when the nullability of the column is not specified in the CREATE TABLE and ALTER TABLE statements.
        /// </summary>
        public bool AnsiNullDefaultOn { get; set; }

        /// <summary>
        /// Controls null behavior in T-SQL
        /// </summary>
        public bool AnsiNulls { get; set; }

        /// <summary>
        /// Impacts character column behavior (char, varchar, binary, and varbinary)
        /// </summary>
        public bool AnsiPadding { get; set; }

        /// <summary>
        /// Controls certain warning messages required for ansi compliance
        /// </summary>
        public bool AnsiWarnings { get; set; }

        /// <summary>
        /// Terminates a query when an overflow or divide-by-zero error occurs during query execution
        /// </summary>
        public bool ArithAbort { get; set; }

        /// <summary>
        /// Controls whether error messages are returned from overflow or divide-by-zero errors during a query
        /// </summary>
        public bool ArithIgnore { get; set; }

        /// <summary>
        /// Controls whether concatenation results are treated as null or empty string values
        /// </summary>
        public bool ConcatNullYieldsNull { get; set; }

        /// <summary>
        /// Controls whether the server will close cursors when you commit a transaction
        /// </summary>
        public bool CursorCloseOnCommit { get; set; }

        /// <summary>
        /// Sets implicit transaction mode for the connection
        /// </summary>
        public bool ImplicitTransactions { get; set; }

        /// <summary>
        /// Controls the emitting of Done w/count tokens from Transact-SQL.
        /// </summary>
        public bool NoCount { get; set; }

        /// <summary>
        /// Generates an error when a loss of precision occurs in an expression
        /// </summary>
        public bool NumericRoundAbort { get; set; }

        /// <summary>
        /// Causes SQL Server to follow the ISO rules regarding quotation mark delimiting identifiers and literal strings
        /// </summary>
        public bool QuotedIdentifier { get; set; }

        /// <summary>
        /// Specifies whether SQL Server automatically rolls back the current transaction when a Transact-SQL statement raises a run-time error
        /// </summary>
        public bool TransactionAbortOnError { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSessionStateUserOptionsOption() :
            base(0) // State identifier
        {
        }

        /// <summary>
        /// Deflate state into the stream
        /// </summary>
        public override void Deflate(Stream destination)
        {
            // Write state ID
            destination.WriteByte(StateID);

            // First 4 bytes are flags
            int userOptions = 0;

            // Next 4 bytes are masks
            int userOptionsMask = unchecked((int)0xffffffff);  // We only care about settings all bits

            // Next byte are option user options
            byte optionalUserOptions = 0;

            // OPT_ANSI_WARNINGS
            if (AnsiWarnings)
            {
                userOptions |= OPT_ANSI_WARNINGS;
            }

            // OPT_ANSI_NULLS
            if (AnsiNulls)
            {
                userOptions |= OPT_ANSI_NULLS;
            }

            // OPT_CURSOR_COMMIT_CLOSE
            if (CursorCloseOnCommit)
            {
                userOptions |= OPT_CURSOR_COMMIT_CLOSE;
            }

            // OPT_QUOTEDIDENT
            if (QuotedIdentifier)
            {
                userOptions |= OPT_QUOTEDIDENT;
            }

            // OPT_CATNULL
            if (ConcatNullYieldsNull)
            {
                userOptions |= OPT_CATNULL;
            }

            // OPT_ANSINULLDFLTON
            if (AnsiNullDefaultOn)
            {
                userOptions |= OPT_ANSINULLDFLTON;
            }
            else
            {
                // Turn off the bit in the mask
                userOptionsMask &= ~OPT_ANSINULLDFLTON;
            }

            // OPT_ANSI_PADDING
            if (AnsiPadding)
            {
                userOptions |= OPT_ANSI_PADDING;
            }

            // OPT_ARITHABORT
            if (ArithAbort)
            {
                userOptions |= OPT_ARITHABORT;
            }

            // OPT_NUMEABORT
            if (NumericRoundAbort)
            {
                userOptions |= OPT_NUMEABORT;
            }

            // OPT_XACTABORT
            if (TransactionAbortOnError)
            {
                userOptions |= OPT_XACTABORT;
            }

            // OPT_NOCOUNT
            if (NoCount)
            {
                userOptions |= OPT_NOCOUNT;
            }

            // OPT_ARITHIGN
            if (ArithIgnore)
            {
                userOptions |= OPT_ARITHIGN;
            }

            // OPT2_IMPLICIT_XACT
            if (ImplicitTransactions)
            {
                optionalUserOptions |= OPT2_IMPLICIT_XACT;
            }

            // Allocate an array for all of this
            MemoryStream cache = new MemoryStream();

            // Put user options in it
            byte[] subValue = BitConverter.GetBytes(userOptions);
            cache.Write(subValue, 0, subValue.Length);

            // Store mask in the cache
            subValue = BitConverter.GetBytes(userOptionsMask);
            cache.Write(subValue, 0, subValue.Length);

            // Write additional byte
            cache.WriteByte(optionalUserOptions);

            // Store the value
            DeflateValue(destination, cache.ToArray());
        }

        /// <summary>
        /// Inflate from stream
        /// </summary>
        public override bool Inflate(Stream source)
        {
            // Reset inflation size
            InflationSize = 0;

            // NOTE: state ID is skipped because it is read by the construction factory

            // Read the value
            byte[] value = InflateValue(source);

            // First 4 bytes are flags
            int userOptions = BitConverter.ToInt32(value, 0);

            // Next 4 bytes are masks
            int userOptionsMask = BitConverter.ToInt32(value, 4);

            // Next byte are option user options
            int optionalUserOptions = (int)value[8];

            // OPT_ANSI_WARNINGS
            AnsiWarnings = (userOptions & OPT_ANSI_WARNINGS) != 0;

            // OPT_ANSI_NULLS
            AnsiNulls = (userOptions & OPT_ANSI_NULLS) != 0;

            // OPT_CURSOR_COMMIT_CLOSE
            CursorCloseOnCommit = (userOptions & OPT_CURSOR_COMMIT_CLOSE) != 0;

            // OPT_QUOTEDIDENT
            QuotedIdentifier = (userOptions & OPT_QUOTEDIDENT) != 0;

            // OPT_CATNULL
            ConcatNullYieldsNull = (userOptions & OPT_CATNULL) != 0;

            // OPT_ANSINULLDFLTON - check the mask first
            if ((userOptionsMask & OPT_ANSINULLDFLTON) != 0)
            {
                // Check the but
                AnsiNullDefaultOn = (userOptions & OPT_ANSINULLDFLTON) != 0;
            }
            else
            {
                // Not set
                AnsiNullDefaultOn = false;
            }

            // OPT_ANSI_PADDING
            AnsiPadding = (userOptions & OPT_ANSI_PADDING) != 0;

            // OPT_ARITHABORT
            ArithAbort = (userOptions & OPT_ARITHABORT) != 0;

            // OPT_NUMEABORT
            NumericRoundAbort = (userOptions & OPT_NUMEABORT) != 0;

            // OPT_XACTABORT
            TransactionAbortOnError = (userOptions & OPT_XACTABORT) != 0;

            // OPT_NOCOUNT
            NoCount = (userOptions & OPT_NOCOUNT) != 0;

            // OPT_ARITHIGN
            ArithIgnore = (userOptions & OPT_ARITHIGN) != 0;

            // OPT2_IMPLICIT_XACT
            ImplicitTransactions = (optionalUserOptions & OPT2_IMPLICIT_XACT) != 0;

            // Inflation is complete
            return true;
        }
    }
}
