module.exports = {
    "env": {
        "browser": true,
        "es2021": true,
        "node": true
    },
    "extends": [
        "eslint:recommended",
        "plugin:@typescript-eslint/recommended"
    ],
    "parser": "@typescript-eslint/parser",
    "parserOptions": {
        "ecmaVersion": 12,
        "sourceType": "module"
    },
    "plugins": [
        "@typescript-eslint"
    ],
    "ignorePatterns": [
        "node_modules/**/*.*",
        "bin/**/*.*",
        "es6/*.js",
        "jiterpreter-opcodes.ts",
        "jiterpreter-tables.ts",
    ],
    "rules": {
        "@typescript-eslint/no-explicit-any": "off",
        "@typescript-eslint/no-non-null-assertion": "off",
        "@typescript-eslint/ban-types": "off",
        "@typescript-eslint/no-loss-of-precision": "off",
        "indent": [
            "error",
            4,
            {
                SwitchCase: 1,
                "ignoredNodes": ["VariableDeclaration[declarations.length=0]"] // fixes https://github.com/microsoft/vscode-eslint/issues/1149
            }
        ],
        "no-multi-spaces": ["error"],
        "no-console": ["error"],
        "arrow-spacing": ["error"],
        "block-spacing": ["error"],
        "comma-spacing": ["error"],
        "quotes": [
            "error",
            "double"
        ],
        "semi": [
            "error",
            "always"
        ]
    }
};
