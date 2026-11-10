// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.HotReload.Utils.Generator;

/// A Delta represents an input that is used to produce the metadata, IL and PDB emitted differences.
/// It contains a Change which identifies the source document that changed and its updated contents
public readonly record struct Delta (Plan.Change<DocumentId,string> Change);
