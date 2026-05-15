// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * BuildXL spec for src/tests/baseservices/compilerservices/FixedAddressValueType
 */

import * as CoreClr from "CoreClrTest";

@@public
export const fixedAddressValueType = CoreClr.coreclr_test({
    name: "FixedAddressValueType",
    srcs: ["FixedAddressValueType.cs"],
    optimize: true,
    allowUnsafe: true
});
