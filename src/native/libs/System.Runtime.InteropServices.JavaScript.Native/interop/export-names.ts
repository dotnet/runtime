// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

type JSExportName = {
    namespaceName: string,
    shortClassName: string,
    methodName: string,
    signatureHash: number,
    isDefault: boolean,
};

export function parseJSExportNames(assemblyName: string, exportNames: readonly string[]): JSExportName[] {
    const prefix = `[${assemblyName}]`;
    const aliases = new Set<string>();
    return exportNames.map(key => {
        const hashIndex = key.lastIndexOf("#");
        const name = key.substring(0, hashIndex);
        // Match the managed binder's ASCII-space trimming so both paths use the same lookup key.
        const fqn = name.substring(prefix.length).replace(/^ +| +$/g, "");
        const methodIndex = fqn.indexOf(":");
        const typeName = fqn.substring(0, methodIndex).replace(/^ +| +$/g, "");
        const namespaceIndex = typeName.lastIndexOf(".");
        const namespaceName = namespaceIndex < 0 ? "" : typeName.substring(0, namespaceIndex);
        const shortClassName = typeName.substring(namespaceIndex + 1);
        const methodName = fqn.substring(methodIndex + 1);
        const hashText = key.substring(hashIndex + 1);
        const signatureHash = Number(hashText);
        if (!key.startsWith(prefix) || hashIndex < 0 ||
            methodIndex < 1 || !shortClassName || !methodName ||
            signatureHash !== (signatureHash | 0) || hashText !== String(signatureHash)) {
            throw new Error(`Invalid JSExport catalog key '${key}' for assembly '${assemblyName}'.`);
        }
        const isDefault = !aliases.has(name);
        aliases.add(name);
        return { namespaceName, shortClassName, methodName, signatureHash, isDefault };
    });
}
