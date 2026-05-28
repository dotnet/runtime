// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as Rules from "Sdk.Rules";

export const x86SharedProgram = Rules.filegroup({
    name: "hwint_x86_shared_program",
    srcs: ["Program.cs"],
});

export const x86SharedSimpleBinOpDataTable = Rules.filegroup({
    name: "hwint_x86_shared_binop_datatable",
    srcs: ["SimpleBinOpTest_DataTable.cs"],
});

export const x86SharedSimpleUnOpDataTable = Rules.filegroup({
    name: "hwint_x86_shared_unop_datatable",
    srcs: ["SimpleUnOpTest_DataTable.cs"],
});
