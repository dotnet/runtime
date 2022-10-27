const config = {
    verbose: true,
    moduleNameMapper:
    {
        "consts:monoWasmThreads": "<rootDir>/tests/mocks/false.js"
    },
    transform: {
        "^.+\\.ts?$": "ts-jest",
    },
};
export default config;