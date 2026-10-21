#!/bin/bash
# P0.3 verification - resolve EVERY "<oracle> <hash> <path>:<line>" citation in the ledger and the
# probes document against the READ-ONLY reference oracle, and check the ledger's structural
# completeness.  Exits non-zero if any citation does not resolve.
#
# Citations resolve with `git show <rev>:<path>` against C:\github\lxr-reference, which holds all
# four revisions as objects.  Nothing is checked out and nothing is written: `git show`, `ls-tree`
# and `cat-file` are read-only.  This is preferred over the cargo git checkouts because the clone
# is the declared oracle and is reproducible on any machine that has it, whereas a cargo checkout
# is a machine-local cache that cargo may prune.  Section 1b proves the two agree byte for byte
# for every cited file, so citations derived from the cargo checkouts transfer unchanged.
set -uo pipefail
REF=${REF:-/mnt/c/github/lxr-reference}
PLDI=/root/.cargo/git/checkouts/mmtk-core-10faf03793f704d0/df8d30a
HEAD=/root/.cargo/git/checkouts/mmtk-core-91cf05d634be0a1e/304ce69
PLDI_BIND=/root/lxr/pldi/mmtk-openjdk
HEAD_BIND=/root/lxr/head/mmtk-openjdk
# The docs directory defaults to the one this script SHIPS IN, so the gate always audits the tree it
# is part of.  It used to default to an absolute path into the author's worktree, which meant a run
# from a fresh extract silently audited a different - and dirty - checkout, reporting on an artifact
# the operator was not looking at.  That is the same failure this whole step is about, this time
# inside the tool rather than a document: see probes 8, correction 6.  An explicit argument overrides.
SELF_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
DOCS=${1:-$(cd "$SELF_DIR/.." && pwd)}
FAIL=0

# Emits "<repo>|<rev>|<path>|<line>" for every citation in the documents.  Handles three forms:
#   full     <hash> `src/foo.rs:123`      /  <hash> `openjdk/bar.cpp:12`
#   shorthand `:456` continuing the most recent full citation in the SAME bullet
#   ranges    `src/foo.rs:116-126`        -> first line of the range
# A revision hash selects the SIDE; the path prefix selects the REPO.
extract () {
  python3 - "$@" <<'PY'
import re, sys
# a revision hash selects the SIDE; the path prefix selects the REPO
side = {'df8d30a': 'pldi', 'abbdd1d': 'pldi', '304ce69': 'head', '0682434': 'head'}
repo = {('pldi', 'src'): ('mmtk-core', 'df8d30a3'),
        ('pldi', 'openjdk'): ('mmtk-openjdk', 'abbdd1d'),
        ('head', 'src'): ('mmtk-core', '304ce69d'),
        ('head', 'openjdk'): ('mmtk-openjdk', '0682434')}
HASH = r'(df8d30a|304ce69|abbdd1d|0682434)'
PATH = r'`((?:src|openjdk)/[\w/.\-]+\.(?:rs|cpp|toml)):(\d+)(?:[-\u2013]\d+)?`'
BARE = r'`:(\d+)(?:[-\u2013]\d+)?`'
tok = re.compile(rf'\b{HASH}\w*\b|{PATH}|{BARE}')
out = set()

def emit(s, p, l):
    key = (s, p.split('/')[0])
    if key in repo:
        r, rev = repo[key]
        out.add((r, rev, p, l))

for path in sys.argv[1:]:
    cur_side = cur_path = None      # context, scoped to one bullet
    col_side = {}                   # for comparison tables: column index -> side
    for line in open(path, encoding='utf-8'):
        # a comparison table whose header names a revision per column binds each
        # column to that revision for the rows that follow
        if line.lstrip().startswith('|'):
            cells = line.split('|')
            if re.search(r'\|\s*-+\s*\|', line):
                continue
            hdr = {i: side[m.group(1)] for i, c in enumerate(cells)
                   if (m := re.search(HASH, c))}
            if hdr and not re.search(PATH, line):
                col_side = hdr
                continue
            if col_side:
                last = None
                for i, c in enumerate(cells):
                    s = col_side.get(i)
                    for m in re.finditer(rf'{PATH}|{BARE}', c):
                        if m.group(1):
                            last = m.group(1)
                            if s: emit(s, last, m.group(2))
                        elif s and last:
                            emit(s, last, m.group(3))
                continue
        # a blank line or a new bullet ends the citation context, so a bare `:NN`
        # can never bind to a file named in an unrelated earlier row
        if not line.strip() or re.match(r'\s*[-*]\s+\*\*', line) or line.startswith('**'):
            cur_side = cur_path = None
            col_side = {}
        for m in tok.finditer(line):
            if m.group(1):
                cur_side = side[m.group(1)]
            elif m.group(2):
                cur_path = m.group(2)
                if cur_side: emit(cur_side, cur_path, m.group(3))
            elif m.group(4) and cur_side and cur_path:
                emit(cur_side, cur_path, m.group(4))
for r, rev, p, l in sorted(out):
    print(f"{r}|{rev}|{p}|{l}")
PY
}

echo "########## 1. citation resolution (read-only, against $REF) ##########"
n=0
CITED=$(mktemp)
CITED_FULL=$(mktemp)
while IFS='|' read -r gitrepo rev path line; do
  [ -z "$gitrepo" ] && continue
  n=$((n+1))
  echo "$gitrepo|$rev|$path" >> "$CITED"
  echo "$gitrepo|$rev|$path|$line" >> "$CITED_FULL"
  blob=$(git -C "$REF/$gitrepo" show "$rev:$path" 2>/dev/null)
  if [ -z "$blob" ]; then
    echo "  FAIL  missing file      $path  (at $rev)"; FAIL=1; continue
  fi
  total=$(printf '%s\n' "$blob" | wc -l)
  if [ "$line" -gt "$total" ] || [ "$line" -lt 1 ]; then
    echo "  FAIL  line out of range $path:$line  (at $rev the file has $total lines)"; FAIL=1; continue
  fi
  txt=$(printf '%s\n' "$blob" | sed -n "${line}p" | sed 's/^[[:space:]]*//' | cut -c1-64)
  printf '  ok    %-8s %-40s :%-5s %s\n' "$rev" "$path" "$line" "$txt"
done < <(extract "$DOCS/P0.3-parity-ledger.md" "$DOCS/P0.3-oracle-probes.md")
echo "  -> $n distinct citations checked"

echo
echo "########## 1b. cargo checkouts vs the reference clone, for every cited file ##########"
# P0.3's citations were derived from the cargo git checkouts.  This proves those trees are
# byte-identical to the reference clone at the pinned revision, so the citations transfer.
same=0; diffn=0; absent=0
while IFS='|' read -r gitrepo rev path; do
  case "$gitrepo|$rev" in
    "mmtk-core|df8d30a3")    local_f="$PLDI/$path" ;;
    "mmtk-core|304ce69d")    local_f="$HEAD/$path" ;;
    "mmtk-openjdk|abbdd1d")  local_f="$PLDI_BIND/$path" ;;
    "mmtk-openjdk|0682434")  local_f="$HEAD_BIND/$path" ;;
    *) continue ;;
  esac
  if [ ! -f "$local_f" ]; then absent=$((absent+1)); echo "  absent locally  $rev $path"; continue; fi
  if git -C "$REF/$gitrepo" show "$rev:$path" 2>/dev/null | diff -q - "$local_f" >/dev/null 2>&1; then
    same=$((same+1))
  else
    diffn=$((diffn+1)); echo "  DIFFERS  $rev $path"; FAIL=1
  fi
done < <(sort -u "$CITED")
rm -f "$CITED"
echo "  identical: $same   differing: $diffn   absent locally: $absent"

echo
echo "########## 1c. is the cited code actually COMPILED in this configuration? ##########"
# Section 1 proves a citation resolves.  It does NOT prove the line does anything: a line can sit
# at the right revision and path, read exactly as the argument needs, and still be #[cfg]-gated
# out or commented out.  P0.3 shipped that defect once - two handle_user_collection_request
# overrides cited as the oracles' behaviour, both under #[cfg(feature = "nogc_no_zeroing")], which
# neither oracle enables - and section 1 passed anyway.  This section closes that gap.
#
# Three signals, all heuristic and all deliberately noisy rather than silent:
#   commented    the cited line itself is a comment
#   cfg-gated    an enclosing brace block, or the cited line's own item, carries #[cfg(...)]
#   empty-body   the cited line opens a block whose body has no executable line
# A flagged citation is NOT automatically an error - dead code may be cited precisely to record
# that it is dead.  But it must be acknowledged: every flag must appear in ACK below, whose
# entries are the citations the documents explicitly discuss as not-compiled.  Anything flagged
# and unacknowledged fails, so a future edit cannot quietly reintroduce the defect.
python3 - "$CITED_FULL" "$REF" <<'PY'
import re, subprocess, sys

cited_file, ref = sys.argv[1], sys.argv[2]

# citations the documents explicitly label as not-compiled / commented-out dead code
ACK = {
    ('mmtk-core', 'df8d30a3', 'src/plan/immix/global.rs', 532),  # cfg(nogc_no_zeroing)
    ('mmtk-core', '304ce69d', 'src/plan/lxr/global.rs',   377),  # cfg(nogc_no_zeroing)
    ('mmtk-core', 'df8d30a3', 'src/plan/global.rs',       590),  # body entirely commented out
    ('mmtk-core', 'df8d30a3', 'src/plan/global.rs',       591),  # a commented line, cited as such
    ('mmtk-core', 'df8d30a3', 'src/plan/global.rs',       593),
    ('mmtk-core', 'df8d30a3', 'src/plan/global.rs',       595),
    # cfg(object_pinning): enabled by nothing at 304ce69d and not forwarded by the binding.
    # Ledger row R05 states this - the pinning API is source-only, never compiled.  The row's
    # load-bearing citations are the UNGATED opt-out sites and are deliberately NOT listed here:
    # lxr/gc_work.rs:12/:21, lxr/global.rs:707/:708, immixspace.rs:1239/:1243/:1244 must stay
    # unflagged, because "compiled into every build" is the whole claim.
    ('mmtk-core', '304ce69d', 'src/policy/sft.rs',                51),
    ('mmtk-core', '304ce69d', 'src/policy/sft.rs',                53),
    ('mmtk-core', '304ce69d', 'src/policy/sft.rs',                55),
    ('mmtk-core', '304ce69d', 'src/policy/immix/immixspace.rs',  170),
    ('mmtk-core', '304ce69d', 'src/policy/immix/immixspace.rs', 1240),
    ('mmtk-core', '304ce69d', 'src/policy/immix/immixspace.rs', 1241),
    # cfg(feature = "sanity") applied at the module declaration, util/mod.rs:66.  Probes 4.1 cites
    # this line precisely to show HEAD's only zero-RC check is compiled OUT under P0.1's recipe.
    ('mmtk-core', '304ce69d', 'src/util/sanity/sanity_checker.rs', 311),
    ('mmtk-core', '304ce69d', 'src/util/sanity/sanity_checker.rs', 313),
    # A /// doc comment, cited deliberately and only as evidence of DOCUMENTED INTENT: the reference
    # states in its own words that UnsupportedProcessEdges is "used for plans that do not support
    # transitively pinning".  R05's behavioural cites are the five panic! sites at :1457/:1463/
    # :1478/:1482/:1486, which are ungated and must stay unflagged.
    ('mmtk-core', '304ce69d', 'src/scheduler/gc_work.rs', 1447),
    ('mmtk-core', '304ce69d', 'src/scheduler/gc_work.rs', 1448),
}

# Gated, but the gate IS satisfied in the configuration under discussion - acknowledged WITH the
# reason, because "gated" and "not compiled" are different claims and the distinction is the point.
ACK_COMPILED = {
    # cargo builds the mmtk crate with the dev profile inside the fastdebug JDK, so
    # debug_assertions is ON and this assertion IS compiled there.  It is NOT compiled in the
    # release build - which is exactly what probes 4 is about.
    ('mmtk-core', 'df8d30a3', 'src/plan/barriers.rs', 315):
        'debug_assertions on in the fastdebug build (cargo dev profile)',
    ('mmtk-core', 'df8d30a3', 'src/plan/barriers.rs', 316):
        'debug_assertions on in the fastdebug build (cargo dev profile)',
    ('mmtk-core', 'df8d30a3', 'src/plan/barriers.rs', 317):
        'debug_assertions on in the fastdebug build (cargo dev profile)',
    ('mmtk-core', 'df8d30a3', 'src/plan/barriers.rs', 318):
        'debug_assertions on in the fastdebug build (cargo dev profile)',
    ('mmtk-core', 'df8d30a3', 'src/plan/barriers.rs', 319):
        'debug_assertions on in the fastdebug build (cargo dev profile)',
}

# Gates applied at the module DECLARATION site are invisible to a per-file scan, so they are
# recorded here explicitly.  Without this the scan would call sanity_checker.rs compiled.
MODULE_GATES = {
    ('mmtk-core', '304ce69d', 'src/util/sanity/'):
        'cfg(feature = "sanity") at src/util/mod.rs:66',
}

def code_only(s):
    """strip string/char literals and line comments so brace counting is not fooled"""
    s = re.sub(r'"(\\.|[^"\\])*"', '""', s)
    s = re.sub(r"'(\\.|[^'\\])'", "''", s)
    return s.split('//')[0]

def is_comment(s):
    t = s.strip()
    return t.startswith('//') or t.startswith('/*') or t.startswith('*')

blobs = {}
def blob(repo, rev, path):
    key = (repo, rev, path)
    if key not in blobs:
        r = subprocess.run(['git', '-C', f'{ref}/{repo}', 'show', f'{rev}:{path}'],
                           capture_output=True, text=True)
        blobs[key] = r.stdout.split('\n') if r.returncode == 0 else None
    return blobs[key]

def attrs_above(lines, i):
    """#[cfg(...)] in the attribute/comment run immediately above 1-based line i"""
    found, j = [], i - 2
    while j >= 0:
        t = lines[j].strip()
        if t == '' or t.startswith('//') or t.startswith('#['):
            if t.startswith('#[') and 'cfg(' in t:
                found.append(t)
            j -= 1
        else:
            break
    return found

def enclosing_opens(lines, target):
    """1-based line numbers of brace blocks still open when `target` is reached"""
    stack = []
    for idx in range(min(target - 1, len(lines))):
        c = code_only(lines[idx])
        for ch in c:
            if ch == '{':
                stack.append(idx + 1)
            elif ch == '}' and stack:
                stack.pop()
    return stack

def empty_block_at(lines, i):
    """cited line opens a block; True if the body holds no executable line"""
    if '{' not in code_only(lines[i - 1]):
        return False
    depth, body, j = 0, [], i - 1
    while j < len(lines):
        c = code_only(lines[j])
        opened = depth
        for ch in c:
            if ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
        if opened > 0 or (j > i - 1 and depth > 0):
            body.append(lines[j])
        if depth <= 0 and j >= i - 1 and '{' in code_only(lines[i - 1]):
            break
        j += 1
    inner = body[1:] if body else []
    return bool(inner) and all(l.strip() == '' or is_comment(l) for l in inner)

flagged, unack = 0, 0
seen = set()
for raw in open(cited_file):
    parts = raw.rstrip('\n').split('|')
    if len(parts) != 4:
        continue
    repo, rev, path, line = parts[0], parts[1], parts[2], int(parts[3])
    if (repo, rev, path, line) in seen:
        continue
    seen.add((repo, rev, path, line))
    lines = blob(repo, rev, path)
    if lines is None or line > len(lines):
        continue
    why = []
    if is_comment(lines[line - 1]):
        why.append('commented')
    cfgs = attrs_above(lines, line)
    for op in enclosing_opens(lines, line):
        cfgs += attrs_above(lines, op)
    for (mrepo, mrev, prefix), reason in MODULE_GATES.items():
        if (repo, rev) == (mrepo, mrev) and path.startswith(prefix):
            cfgs.append(reason)
    seen_cfg = []
    for c in cfgs:
        if c not in seen_cfg:
            seen_cfg.append(c)
    if seen_cfg:
        why.append('cfg-gated ' + ' + '.join(seen_cfg))
    if empty_block_at(lines, line):
        why.append('empty-body')
    if why:
        flagged += 1
        key = (repo, rev, path, line)
        if key in ACK_COMPILED:
            tag, note = 'ok  ', f'  [COMPILED: {ACK_COMPILED[key]}]'
        elif key in ACK:
            tag, note = 'ack ', '  [acknowledged as NOT compiled]'
        else:
            tag, note = 'FAIL', '  [unacknowledged - label it or replace it]'
            unack += 1
        print(f"  {tag}  {rev} {path}:{line}  <- {'; '.join(why)}{note}")

print(f"  -> flagged: {flagged}   acknowledged: {flagged - unack}   UNACKNOWLEDGED: {unack}")
sys.exit(1 if unack else 0)
PY
[ $? -eq 0 ] || FAIL=1
rm -f "$CITED_FULL"

echo
echo "########## 2. ledger structural completeness ##########"
L=$DOCS/P0.3-parity-ledger.md
# The expected row count is DERIVED, never hardcoded: the index table is the truth, and 4's
# self-reported "Rows total" must agree with it.  A literal in the script would have to be edited in
# three places every time a row is added or retired, and would either fail on a correct new row or
# keep passing while under-checking it.
idx=$(grep -cE '^\| (A0|B0|C0|D0|E0|S0|R0)[0-9] \|' "$L")
declared=$(grep -E '^\| Rows total \|' "$L" | grep -oE '[0-9]+' | head -1)
echo "  index rows                 : $idx"
printf '  4 self-reported total      : %s\n' "${declared:-<none>}"
[ -n "$declared" ] || { echo "     FAIL  4 does not state a row total"; FAIL=1; }
[ "${declared:-0}" -eq "$idx" ] || { echo "     FAIL  4 says ${declared}, index has $idx"; FAIL=1; }
for f in "Reason:" "Citation:" "Validation:" "Provenance:" ".NET realization:" "Closure evidence:" "Status:"; do
  c=$(grep -cF "**$f**" "$L")
  printf '  rows carrying %-20s: %s\n' "$f" "$c"
  # R rows use "Declared oracle:" in place of a separate Reason line; one per index row for the rest
  [ "$c" -lt "$idx" ] && { echo "     FAIL  expected $idx"; FAIL=1; }
done
det=$(grep -cE '^\*\*(A|B|C|D|E|S|R)0[0-9] — ' "$L")
echo "  detail blocks              : $det"
[ "$idx" -eq "$det" ] || { echo "  FAIL index/detail mismatch ($idx vs $det)"; FAIL=1; }

# A retired identifier must not dangle: the roadmap and later phases cite row IDs externally, so
# every ID retired in 2.1 needs a redirect block, a struck index row, and a live target row.
echo
echo "  retired identifiers redirect rather than dangle:"
retired=$(grep -oE '^\| ~~[A-ESR]0[0-9]~~' "$L" | grep -oE '[A-ESR]0[0-9]' | sort -u)
if [ -z "$retired" ]; then
  echo "    none"
else
  for r in $retired; do
    tgt=$(grep -E "^\| $r \(" "$L" | grep -oE '\*\*[A-ESR]0[0-9]\*\*' | tr -d '*' | head -1)
    blk=$(grep -cE "^\*\*Retired — $r \(" "$L")
    row=0
    [ -n "$tgt" ] && row=$(grep -cE "^\| $tgt \|" "$L")
    printf '    %s -> %-4s redirect block: %s   target index row: %s\n' \
           "$r" "${tgt:-?}" "$blk" "$row"
    { [ "$blk" -eq 1 ] && [ "$row" -eq 1 ]; } || { echo "     FAIL  $r dangles"; FAIL=1; }
  done
fi

echo
echo "  every index row has a provenance tag:"
bad=$(grep -E '^\| (A0|B0|C0|D0|E0|S0|R0)[0-9] \|' "$L" | grep -vcE '\[(obs-oracle|obs-override|read-only)\]')
echo "    rows missing a tag       : $bad"; [ "$bad" -eq 0 ] || FAIL=1
echo "  every index row has a group and a declared oracle:"
bad=$(grep -E '^\| (A0|B0|C0|D0|E0|S0|R0)[0-9] \|' "$L" | awk -F'|' '$4 !~ /[A-ES R]/ || $5 !~ /(PLDI|HEAD|none)/' | wc -l)
echo "    rows missing either      : $bad"; [ "$bad" -eq 0 ] || FAIL=1

echo
echo "  one declared oracle per coupled group:"
for g in A B C D E; do
  o=$(grep -E "^\| ${g}0[0-9] \|" "$L" | awk -F'|' '{gsub(/ /,"",$5); print $5}' | sort -u | tr '\n' ',')
  cnt=$(grep -E "^\| ${g}0[0-9] \|" "$L" | awk -F'|' '{gsub(/ /,"",$5); print $5}' | sort -u | wc -l)
  printf '    group %s -> %-12s (%s distinct)\n' "$g" "$o" "$cnt"
  [ "$cnt" -eq 1 ] || { echo "      FAIL group $g has $cnt declared oracles"; FAIL=1; }
done

echo
echo "########## 3. markdown hygiene ##########"
for f in "$DOCS"/P0.3-*.md "$DOCS"/README.md; do
  t=$(grep -cE ' +$' "$f")
  printf '  %-28s trailing-whitespace lines: %s\n' "$(basename "$f")" "$t"
  [ "$t" -eq 0 ] || FAIL=1
done

echo
[ $FAIL -eq 0 ] && echo "VERIFY: PASS" || echo "VERIFY: FAIL"
exit $FAIL
