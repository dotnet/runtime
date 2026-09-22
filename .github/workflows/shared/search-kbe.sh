#!/usr/bin/env bash

set -euo pipefail

usage="Usage: search-kbe.sh <issues|pull-requests> <GitHub search query> <evidence directory>"
if [ "$#" -ne 3 ] || [ -z "$2" ] || [ -z "$3" ]; then
    echo "$usage" >&2
    exit 1
fi

case "$1" in
    issues) tool="search_issues" ;;
    pull-requests) tool="search_pull_requests" ;;
    *) echo "$usage" >&2; exit 1 ;;
esac

directory="$3"
mkdir -p "$directory"
jq -n '{status: "blocked", reason: "lookup did not complete"}' > "$directory/summary.json"
jq -n --arg query "repo:dotnet/runtime $2" '{
    owner: "dotnet",
    repo: "runtime",
    query: $query,
    fields: ["number", "title", "state", "user", "labels", "html_url"],
    perPage: 100
}' > "$directory/request.json"

if ! github "$tool" . < "$directory/request.json" > "$directory/response.json"; then
    echo "KBE lookup failed; do not create an issue from this search." >&2
    cat "$directory/summary.json"
    exit 1
fi

if ! jq -s '
    def filtered:
        any(.. | strings; test("\\[Filtered\\]|\\[DIFC-FILTERED\\]"; "i"));
    def blocked($reason):
        {status: "blocked", reason: $reason};
    def nonempty_string:
        type == "string" and length > 0;
    def candidate:
        type == "object" and
        (.number | type == "number" and . > 0 and floor == .) and
        (.title | nonempty_string) and
        (.state == "open" or .state == "closed") and
        (.user.login | nonempty_string) and
        (.labels | type == "array") and
        (.html_url | nonempty_string);

    if length != 1 then
        blocked("expected one JSON response")
    elif filtered then
        blocked("integrity-filtered candidate")
    else
        .[0] |
        if type == "array" and length == 1 then .[0] else . end |
        if type != "object" then error("expected an object")
        elif .isError == true or has("error") then error("tool reported an error")
        elif has("content") then
            if (.content | type == "array" and length == 1) and
                .content[0].type == "text" then
                .content[0].text | fromjson
            else error("expected one MCP text result")
            end
        else .
        end |
        if filtered then
            blocked("integrity-filtered candidate")
        elif type != "object" or .isError == true or has("error") then
            blocked("invalid search result")
        elif .incomplete_results != false or (.items | type != "array") or
            (.total_count | type != "number") then
            blocked("incomplete or invalid search result")
        elif .total_count != (.items | length) then
            blocked("not all candidates returned; narrow the query")
        elif all(.items[]; candidate) | not then
            blocked("candidate metadata is missing or invalid")
        else
            {
                status: (if .total_count == 0 then "no_match" else "candidates" end),
                total_count,
                items
            }
        end
    end
' "$directory/response.json" > "$directory/summary.json"; then
    jq -n '{status: "blocked", reason: "malformed search response"}' > "$directory/summary.json"
fi

cat "$directory/summary.json"
if ! jq -e '.status == "no_match" or .status == "candidates"' "$directory/summary.json" > /dev/null; then
    echo "KBE lookup is inconclusive; do not create an issue from this search." >&2
    exit 1
fi
