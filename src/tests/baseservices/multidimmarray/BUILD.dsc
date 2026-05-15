// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * BuildXL spec for src/tests/baseservices/multidimmarray
 */

import * as CoreClr from "CoreClrTest";

@@public
export const enumTest = CoreClr.coreclr_test({
    name: "MultiDimmArray_Enum",
    srcs: ["enum.cs"],
    optimize: true,
    allowUnsafe: true
});
