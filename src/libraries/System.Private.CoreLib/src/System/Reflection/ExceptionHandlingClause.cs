// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public override string ToString() =>
            $"Flags={Flags}, TryOffset={TryOffset}, TryLength={TryLength}, HandlerOffset={HandlerOffset}, HandlerLength={HandlerLength}, CatchType={CatchType}";
    }
}
