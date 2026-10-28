// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers;

internal delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);
