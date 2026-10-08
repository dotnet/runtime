// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;

namespace Microsoft.DotNet.HotReload.Utils.Generator;

/// What we know about the base compilation
///
/// BaselineSolution: the solution we're working on
/// BaselineProjectId: the project we're working on; FIXME: need to be more clever when there are project references
/// BaselineOutputAsmPath: absolute path of the baseline assembly
/// DocResolver: a map from document ids to documents
/// ChangeMakerService: A stateful encapsulatio of the series of changes that have been made to the baseline
internal record struct BaselineArtifacts (Solution BaselineSolution, ProjectId BaselineProjectId, string BaselineOutputAsmPath, DocResolver DocResolver, HotReloadService HotReloadService);
