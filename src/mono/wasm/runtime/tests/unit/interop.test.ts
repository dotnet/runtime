import { ManagedError } from "../../marshal";
import { describe, expect, it } from "@jest/globals";

describe("Interop", () => {
    describe("ManagedError", () => {
        it("should be cheap to create", async () => {
            const err = new ManagedError("test");

            expect(err).not.toBeNull();
            expect(err.message).toEqual("test");
            expect(err.stack).not.toBeNull();
            expect(err.stack).toContain("it");

            const stack = Object.getOwnPropertyDescriptor(err, "stack")!;
            expect(typeof stack).toEqual("object");
            expect(typeof stack.value).toEqual("undefined");
            expect(typeof stack.get).toEqual("function");
        });
    });
});