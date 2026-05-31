// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as Rules from "Sdk.Rules";

export const x86SharedProgram = Rules.getProvider<Rules.FilegroupResult>(Rules.filegroup({
    name: "hwint_x86_shared_program",
    srcs: ["Program.cs"],
}), "FilegroupResult");

export const x86SharedSimpleBinOpDataTable = Rules.getProvider<Rules.FilegroupResult>(Rules.filegroup({
    name: "hwint_x86_shared_binop_datatable",
    srcs: ["SimpleBinOpTest_DataTable.cs"],
}), "FilegroupResult");

export const x86SharedSimpleUnOpDataTable = Rules.getProvider<Rules.FilegroupResult>(Rules.filegroup({
    name: "hwint_x86_shared_unop_datatable",
    srcs: ["SimpleUnOpTest_DataTable.cs"],
}), "FilegroupResult");
