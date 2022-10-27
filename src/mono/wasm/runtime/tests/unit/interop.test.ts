import { ManagedError } from "../../marshal";
import { describe, expect, it } from "@jest/globals";

describe("Interop", () => {
    /*beforeAll(async () => {
    })
    afterAll(async () => {
    })*/

    describe("ManagedError", () => {
        it("should be cheap to create", async () => {
            const err = new ManagedError("test");

            expect(err).not.toBeNull();
            expect(err.stack).not.toBeNull();
        });
    });
});