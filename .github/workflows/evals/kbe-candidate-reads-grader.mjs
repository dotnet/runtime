function callKey(event) {
    return JSON.stringify([event.agentId ?? null, event.data.toolCallId]);
}

function resultText(result) {
    if (typeof result === "string") {
        return result;
    }

    if (typeof result?.content === "string") {
        return result.content;
    }

    if (result === null || result === undefined) {
        return "";
    }

    try {
        return JSON.stringify(result);
    } catch {
        return String(result);
    }
}

function resultIssueNumber(result) {
    if (typeof result !== "object" || result === null) {
        return undefined;
    }

    const issue = typeof result.issue === "object" && result.issue !== null
        ? result.issue
        : result;
    return issue.number;
}

function isSearchHarnessCall(event) {
    if (event.type !== "tool_call" || !/^(bash|powershell)$/.test(event.data.toolName)) {
        return false;
    }

    const command = event.data.arguments?.command;
    return typeof command === "string" &&
        /(?:^|[\\/])search-kbe-issues\.cjs(?:\s|$)/.test(command);
}

function isIssueReadCall(event) {
    return event.type === "tool_call" &&
        /^(?:mcp__)?github[-_.]+issue_read$/.test(event.data.toolName);
}

function parseCandidates(result) {
    const text = resultText(result).trim();
    const payloads = [text, ...text.split(/\r?\n/).reverse()];
    let parsed;
    for (const payload of payloads) {
        try {
            const value = JSON.parse(payload);
            if (Array.isArray(value)) {
                parsed = value;
                break;
            }
        } catch {
            // Shell tools may surround stdout with execution metadata.
        }
    }

    if (!parsed) {
        throw new Error("search-kbe-issues returned output that is not JSON");
    }

    for (const candidate of parsed) {
        if (!Number.isInteger(candidate?.number) ||
            typeof candidate?.user?.login !== "string" ||
            candidate.user.login.length === 0) {
            throw new Error("search-kbe-issues returned a malformed candidate");
        }
    }

    return parsed.map((candidate) => candidate.number);
}

class KbeCandidateReadsGrader {
    metadata = {
        name: "kbe-candidate-reads",
        description: "Verifies every issue-search candidate receives a successful, unfiltered issue_read",
        behavior: {},
        determinism: "complex-static",
        reference: "reference-free",
        temporalScope: "trajectory-level",
        costProfile: "free",
    };

    async grade(input) {
        if (!input.trajectory) {
            throw new Error("Missing trajectory");
        }

        const calls = new Map();
        const pendingSearches = new Set();
        const candidates = new Map();
        const reads = new Map();
        const errors = [];
        let harnessCallCount = 0;

        for (const [index, event] of input.trajectory.events.entries()) {
            if (event.type === "tool_call") {
                if (isSearchHarnessCall(event)) {
                    harnessCallCount++;
                    const key = callKey(event);
                    calls.set(key, { kind: "search" });
                    pendingSearches.add(key);
                } else if (isIssueReadCall(event)) {
                    const args = event.data.arguments ?? {};
                    calls.set(callKey(event), {
                        kind: "read",
                        callIndex: index,
                        number: Number(args.issue_number),
                        validScope: args.owner === "dotnet" &&
                            args.repo === "runtime" &&
                            args.method === "get",
                    });
                }
            } else if (event.type === "tool_result") {
                const call = calls.get(callKey(event));
                if (!call) {
                    continue;
                }

                if (call.kind === "search") {
                    pendingSearches.delete(callKey(event));
                    if (!event.data.success) {
                        errors.push("search-kbe-issues call failed");
                        continue;
                    }

                    try {
                        for (const number of parseCandidates(event.data.result)) {
                            if (!candidates.has(number)) {
                                candidates.set(number, index);
                            }
                        }
                    } catch (error) {
                        errors.push(error instanceof Error ? error.message : String(error));
                    }
                } else if (call.validScope && Number.isInteger(call.number) &&
                    event.data.success &&
                    !(event.data.result &&
                        typeof event.data.result === "object" &&
                        event.data.result.isError === true) &&
                    resultIssueNumber(event.data.result) === call.number &&
                    !resultText(event.data.result).includes("[Filtered]")) {
                    reads.set(call.number, call.callIndex);
                }
            }
        }

        if (harnessCallCount === 0) {
            errors.push("search-kbe-issues was not called");
        }
        if (pendingSearches.size > 0) {
            errors.push("search-kbe-issues call did not return a result");
        }

        const missing = [...candidates]
            .filter(([number, searchIndex]) => !reads.has(number) || reads.get(number) < searchIndex)
            .map(([number]) => number)
            .sort((left, right) => left - right);
        const passed = errors.length === 0 && missing.length === 0;
        const evidence = passed
            ? `Read all ${candidates.size} candidate issue(s) returned by ${harnessCallCount} search call(s).`
            : [...errors, missing.length > 0 ? `Missing successful issue_read for: ${missing.join(", ")}` : ""]
                .filter(Boolean)
                .join("\n");

        return {
            name: this.metadata.name,
            kind: "code",
            passed,
            score: passed ? 1 : 0,
            evidence,
            metadata: {
                candidates: [...candidates.keys()].sort((left, right) => left - right),
                missing,
            },
        };
    }
}

export function registerGraders(registry) {
    registry.register(new KbeCandidateReadsGrader());
}
