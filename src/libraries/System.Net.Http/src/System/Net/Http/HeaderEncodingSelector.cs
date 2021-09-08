// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Net.Http
{
    /// <summary>
    /// Represents a method that specifies the <see cref="Encoding"/> to use when interpreting header values.
    /// </summary>
    /// <param name="headerName">Name of the header to specify the <see cref="Encoding"/> for.</param>
    /// <param name="context">The <typeparamref name="TContext"/> we are encoding/decoding the headers for.</param>
    /// <typeparam name="TContext">The type which headers are being encoded/decoded;</typeparam>
    /// <returns><see cref="Encoding"/> to use or <see langword="null"/> to use the default behavior.</returns>
    public delegate Encoding? HeaderEncodingSelector<TContext>(string headerName, TContext context);
}
