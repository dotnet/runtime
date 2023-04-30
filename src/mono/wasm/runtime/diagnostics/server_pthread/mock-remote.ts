// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import monoDiagnosticsMock from "consts:monoDiagnosticsMock";
import { runtimeHelpers } from "../../globals";
import type { Mock } from "../mock";
import { mock } from "../mock";

export function importAndInstantiateMock(mockURL: string): Promise<Mock> {
    if (monoDiagnosticsMock) {
        const mockPrefix = "mock:";
        const scriptURL = mockURL.substring(mockPrefix.length);
        return import(scriptURL).then((mockModule) => {
            const script = mockModule.default;
            return mock(script, { trace: runtimeHelpers.diagnosticTracing });
        });
    } else {
        return Promise.resolve(undefined as unknown as Mock);
    }
}

