// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
namespace Microsoft.Diagnostics.DataContractReader.Legacy;
internal interface IEnum<T>
{
    IEnumerator<T> Enumerator { get; }
    TargetPointer LegacyHandle { get; }
}
