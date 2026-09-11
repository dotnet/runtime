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
        (event.data.toolName === "issue_read" || event.data.toolName.endsWith(".issue_read"));
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
        const candidates = new Map();
        const reads = new Map();
        const errors = [];
        let harnessCallCount = 0;

        for (const [index, event] of input.trajectory.events.entries()) {
            if (event.type === "tool_call") {
                if (isSearchHarnessCall(event)) {
                    harnessCallCount++;
                    calls.set(callKey(event), { kind: "search" });
                } else if (isIssueReadCall(event)) {
                    const args = event.data.arguments ?? {};
                    calls.set(callKey(event), {
                        kind: "read",
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
                    event.data.success && !resultText(event.data.result).includes("[Filtered]")) {
                    reads.set(call.number, index);
                }
            }
        }

        if (harnessCallCount === 0) {
            errors.push("search-kbe-issues was not called");
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
