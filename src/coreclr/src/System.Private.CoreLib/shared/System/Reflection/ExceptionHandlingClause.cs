// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Globalization;

namespace System.Reflection
{
    public class ExceptionHandlingClause
    {
        protected ExceptionHandlingClause() { }
        public virtual ExceptionHandlingClauseOptions Flags => default;
        public virtual int TryOffset => 0;
        public virtual int TryLength => 0;
        public virtual int HandlerOffset => 0;
        public virtual int HandlerLength => 0;
        public virtual int FilterOffset => throw new InvalidOperationException(SR.Arg_EHClauseNotFilter);
        public virtual Type? CatchType => null;

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentUICulture,
                "Flags={0}, TryOffset={1}, TryLength={2}, HandlerOffset={3}, HandlerLength={4}, CatchType={5}",
                Flags, TryOffset, TryLength, HandlerOffset, HandlerLength, CatchType);
        }
    }
}

