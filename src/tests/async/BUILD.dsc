// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as Rules from "Sdk.Rules";

export const asyncSharedSrcs = Rules.getProvider<Rules.FilegroupResult>(Rules.filegroup({
    name: "async_shared",
    srcs: ["RuntimeAsyncMethodGenerationAttribute.cs"],
}), "FilegroupResult");
