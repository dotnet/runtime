// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as Rules from "Sdk.Rules";

export const hwintGeneralSharedSrcs = Rules.filegroup({
    name: "hwint_general_shared",
    srcs: ["Program.cs"],
});
