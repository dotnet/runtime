#!/usr/bin/env python3
"""Verify the document's quantified prose claims against the CSVs shipped beside them.

Why this exists. The gate verified data against data — checkpoint against raw CSV, published means
against re-derived means, row counts against files — and 30 checks passed over a sentence in section
6.5 that said "all four throughput ratios ... drift by less than the 8.35% floor" while the CSV
sitting beside it published `long-lived-cache` at 8.744%. Three of four, not four. Rule 25 said a
gate auditing an artifact against itself cannot catch the artifact lying about the code; this is its
twin, that the artifact can also lie about its own data.

Design constraint. Every expected value is DERIVED FROM THE CSV at run time and never written as a
constant here. A checker holding its own copy of the number is a second place to be wrong, and it
would agree with a stale document as happily as with a correct one. This file holds only the CSV
column names, the predicate, and a regex locating the sentence.

Every claim prints the derived value, the located sentence, and the comparison, per rule 26 — a bare
pass/verdict hides a check that ran against the wrong thing.
"""
import argparse
import csv
import fractions
import json
import math
import os
import re
import statistics
import sys

FLOOR_PERCENT = 8.35
WORDS = {0: "zero", 1: "one", 2: "two", 3: "three", 4: "four", 5: "five", 6: "six",
         7: "seven", 8: "eight", 9: "nine", 10: "ten", 11: "eleven", 12: "twelve"}


def count_word_forms(n):
    """A count may be written as a digit or a word; accept either, case-insensitively."""
    forms = [str(n)]
    if n in WORDS:
        forms.append(WORDS[n])
    return forms


def n_of_m_forms(n, m):
    """Renderings of an 'N of M' claim, binding the count to its total.

    Checking a bare count is not safe. A perturbation that changed "Three of the four" to "All four
    of the four" passed the first version of this checker, because the expected form "3" matched the
    "3.3x" in the following sentence. Requiring the pair cannot be satisfied by a stray decimal.
    """
    forms = []
    for a in count_word_forms(n):
        for b in count_word_forms(m):
            forms.append("%s of %s" % (a, b))
            forms.append("%s of the %s" % (a, b))
    return forms


def all_n_forms(n):
    """Renderings of an 'all N' claim.

    "All 8 throughput comparisons agree" conveys both the total and the count with one phrase, so
    demanding two occurrences of "8" would fail a document that is correct. Binding the count to the
    universal quantifier is the honest expectation here: stricter than a bare count, satisfiable
    once.
    """
    return ["all %s" % form for form in count_word_forms(n)]


def pattern_for(form):
    pattern = re.escape(form)
    if form[:1].isalnum() or form[:1] == "_":
        pattern = r"\b" + pattern
    if form[-1:].isalnum() or form[-1:] == "_":
        pattern = pattern + r"\b"
    return pattern


def matches(form, sentence):
    """Word-boundary match, but only where a boundary is meaningful.

    Many expected forms begin or end with a non-word character — `` `long-lived-cache` ``,
    ``**8.74%**``, ``14.48%``. A blind `\\b` on those sides never matches, so the boundary is applied
    only where the form's own edge is a word character. A bare `\\b3\\b` still finds the 3 in "3.3x",
    which is why counts with a total go through n_of_m_forms rather than through here alone.
    """
    return re.search(pattern_for(form), sentence, re.IGNORECASE) is not None


def occurrence_count(forms, text):
    """How many distinct places in the text satisfy any of these forms.

    Presence is not enough when one claim asserts two facts that happen to share a value. The
    section 5.1 claim asserts both that the heap-limit formula holds in 30 of 30 rows and that srv
    is the binding arm in 30 of 30 rows. A checker asking only whether "30 of 30" appears is
    satisfied by either sentence alone, so falsifying one of them passes — which is exactly what the
    first version of this check did when the binding-arm count was perturbed to 29 of 30.

    This is the same defect as expecting "3" and matching the "3" in "3.3x": asking whether a string
    is present where the real question is how many times. Counting non-overlapping occurrences and
    requiring one per expectation is the general form of the fix.
    """
    pattern = "|".join(pattern_for(f) for f in forms)
    return len(list(re.finditer(pattern, text, re.IGNORECASE)))


def rows(path):
    with open(path, newline="", encoding="utf-8") as handle:
        return list(csv.DictReader(handle))


def is_true(value):
    return value.strip().lower() == "true"


class Claim:
    """One quantified prose claim: where it lives, and what the data says it must contain."""

    def __init__(self, claim_id, anchor, derive, description):
        self.claim_id = claim_id
        self.anchor = anchor
        self.derive = derive
        self.description = description


def find_anchor(doc_lines, anchor):
    """Locate a claim's sentence, ignoring blockquotes.

    Blockquotes in this document are rule 22 corrections: text quoted precisely because it is
    false. Binding a claim check to one would test the wrong sentence and, worse, would keep
    passing after the live claim was edited. The floor claim did exactly this on first run — the
    anchor matched the quoted false sentence four lines above the corrected one.
    """
    pattern = re.compile(anchor, re.IGNORECASE)
    for index, line in enumerate(doc_lines):
        if line.lstrip().startswith(">"):
            continue
        if pattern.search(line):
            # Claims routinely wrap across a line break; give the predicate the neighbourhood,
            # excluding any quoted-and-corrected text that happens to sit beside it.
            start = max(0, index - 1)
            window = [l for l in doc_lines[start:index + 3] if not l.lstrip().startswith(">")]
            return index + 1, " ".join(l.strip() for l in window)
    return None, None


# ---------------------------------------------------------------------------------------------
# Derivations. Each returns (list of (label, [acceptable strings]), list of printable evidence).
# ---------------------------------------------------------------------------------------------

def derive_repro_cells(paths):
    data = rows(paths["repro"])
    exceeding = [r for r in data if is_true(r["exceedsBothSpreadAndFloor"])]
    # Median over ALL cells, not over the disagreeing subset. Both readings of the sentence are
    # grammatical and they differ materially (14.48% over 16 against 17.94% over the 12), which is
    # why the prose was made explicit about its population rather than left to be inferred.
    diffs = [abs(float(r["differencePercent"])) for r in data]
    subset = [abs(float(r["differencePercent"])) for r in exceeding]
    return (
        [("disagreeing cells bound to the total", n_of_m_forms(len(exceeding), len(data))),
         ("median difference over all cells", ["%.2f%%" % statistics.median(diffs)]),
         ("max difference", ["%.2f%%" % max(diffs)])],
        ["cells=%d exceeding=%d median(all)=%.2f%% median(exceeding)=%.2f%% max=%.2f%%"
         % (len(data), len(exceeding), statistics.median(diffs), statistics.median(subset),
            max(diffs))],
    )


def derive_offered_load(paths):
    data = rows(paths["repro"])
    differing = [r for r in data if not is_true(r["sameOfferedLoad"])]
    return (
        [("cells with a different offered load", count_word_forms(len(differing)))],
        ["total=%d sameOfferedLoad=false -> %d (difference %d)"
         % (len(data), len(differing), len(data) - len(differing))],
    )


def derive_comparable(paths):
    data = rows(paths["repro"])
    comparable = [r for r in data if is_true(r["sameOfferedLoad"])]
    disagreeing = [r for r in comparable if is_true(r["exceedsBothSpreadAndFloor"])]
    # As above: median over all comparable cells, not over the disagreeing subset.
    diffs = [abs(float(r["differencePercent"])) for r in comparable]
    subset = [abs(float(r["differencePercent"])) for r in disagreeing]
    return (
        [("comparable cells", count_word_forms(len(comparable))),
         ("still disagreeing", count_word_forms(len(disagreeing))),
         ("median over all comparable", ["%.2f%%" % statistics.median(diffs)])],
        ["comparable=%d disagreeing=%d median(comparable)=%.2f%% median(disagreeing)=%.2f%%"
         % (len(comparable), len(disagreeing), statistics.median(diffs),
            statistics.median(subset))],
    )


def derive_direction(paths):
    data = [r for r in rows(paths["repro"]) if r["metric"] == "operationsPerSecond"]
    faster = [r for r in data if float(r["otherMean"]) > float(r["baselineMean"])]
    # The prose only gets to say "all" while the counts agree; if they ever diverge the expected
    # form changes with them rather than the checker quietly accepting the weaker phrasing.
    forms = all_n_forms(len(data)) if len(faster) == len(data) else n_of_m_forms(len(faster), len(data))
    return (
        [("throughput comparisons all moving the same way", forms)],
        ["throughput comparisons=%d s3-faster=%d s3-slower=%d"
         % (len(data), len(faster), len(data) - len(faster))],
    )


def _ratio_stability(path):
    data = rows(path)
    ratio = [float(r["ratioDriftPercent"]) for r in data]
    absolute = [float(r["largestAbsoluteDriftPercent"]) for r in data]
    more_stable = [r for r in data if is_true(r["ratioMoreStableThanAbsolute"])]
    return data, statistics.median(ratio), statistics.median(absolute), len(more_stable)


def derive_stability_s3(paths):
    data, med_ratio, med_abs, more = _ratio_stability(paths["ratio_s3"])
    return (
        [("median ratio drift", ["**%.2f%%**" % med_ratio]),
         ("median absolute drift", ["%.2f%%" % med_abs]),
         ("more stable", ["%d of %d" % (more, len(data))])],
        ["rows=%d medianRatio=%.2f%% medianAbs=%.2f%% moreStable=%d"
         % (len(data), med_ratio, med_abs, more)],
    )


def derive_stability_s4(paths):
    data, med_ratio, med_abs, more = _ratio_stability(paths["ratio_s4"])
    return (
        [("median ratio drift", ["**%.2f%%**" % med_ratio]),
         ("median absolute drift", ["%.2f%%" % med_abs]),
         ("more stable", ["%d of %d" % (more, len(data))])],
        ["rows=%d medianRatio=%.2f%% medianAbs=%.2f%% moreStable=%d"
         % (len(data), med_ratio, med_abs, more)],
    )


def derive_floor_claim(paths):
    """The claim that was false. Both counts are derived and both must appear."""
    data = [r for r in rows(paths["ratio_s3"]) if r["metric"] == "operationsPerSecond"]
    below = [r for r in data if float(r["ratioDriftPercent"]) < FLOOR_PERCENT]
    above = [r for r in data if float(r["ratioDriftPercent"]) >= FLOOR_PERCENT]
    checks = [("throughput ratios below the floor, bound to the total",
               n_of_m_forms(len(below), len(data)))]
    evidence = ["throughput rows=%d below %.2f%%=%d at-or-above=%d (difference %d)"
                % (len(data), FLOOR_PERCENT, len(below), len(above), len(data) - len(below))]
    for r in above:
        # A row above the floor must be named, with its drift, so it cannot be quietly averaged in.
        checks.append(("named above-floor scenario", ["`%s`" % r["scenario"]]))
        checks.append(("its ratio drift", ["**%.2f%%**" % float(r["ratioDriftPercent"])]))
        checks.append(("its absolute drift", ["**%.2f%%**" % float(r["largestAbsoluteDriftPercent"])]))
        evidence.append("above floor: %s ratio=%.3f%% absolute=%.3f%%"
                        % (r["scenario"], float(r["ratioDriftPercent"]),
                           float(r["largestAbsoluteDriftPercent"])))
    return checks, evidence


def _heap_facts(paths):
    """Heap-limit arithmetic, with "binds" defined the way the harness defines it.

    `HeapBaseline.SharedMinimumMb` is `Math.Max(wks, srv)`, so an arm binds when its minimum
    ATTAINS that maximum. An earlier version of this function counted srv as binding only where it
    STRICTLY EXCEEDED wks. The two definitions agree on this data, and they agree only because no
    scenario ties.

    On a tie they diverge, and in the direction that rejects a correct document: the shared minimum
    still equals the Server minimum, so "srv is the binding arm" stays true, while strict inequality
    would report 29 of 30 and fail true text. That is the "All 8 throughput comparisons" failure
    mode again - a checker refusing correct prose - present but invisible because the data hides it.

    It will not stay hidden. P0.6 takes the maximum over three arms, where ties are materially more
    likely than over two, and the first tie would have read as a prose defect rather than a
    definitional one. Ties are therefore counted and published rather than assumed away, and both
    arms' attainment counts are returned so the document can state which arm binds without the word
    "binds" having to carry an unstated definition.
    """
    data = rows(paths["achieved"])
    formula_ok = sum(
        1 for r in data
        if int(r["heapLimitMb"]) == math.ceil(int(r["sharedMinimumMb"]) * float(r["nominalFactor"])))
    shared_ok = sum(
        1 for r in data
        if int(r["sharedMinimumMb"]) == max(int(r["wksMinimumMb"]), int(r["srvMinimumMb"])))
    srv_binds = sum(1 for r in data if int(r["sharedMinimumMb"]) == int(r["srvMinimumMb"]))
    wks_binds = sum(1 for r in data if int(r["sharedMinimumMb"]) == int(r["wksMinimumMb"]))
    ties = sum(1 for r in data if int(r["srvMinimumMb"]) == int(r["wksMinimumMb"]))
    return len(data), formula_ok, shared_ok, srv_binds, wks_binds, ties


def derive_heap_formula(paths):
    """Section 5.1's forward argument rests on arithmetic; it is derived here, not asserted there."""
    total, formula_ok = _heap_facts(paths)[:2]
    mismatches = total - formula_ok
    forms = ["%s rows, %d %s" % (f, mismatches, word)
             for f in n_of_m_forms(formula_ok, total)
             for word in ("mismatch", "mismatches")]
    return (
        [("rows for which the limit is ceil(shared x factor)", forms)],
        ["rows=%d, formula holds=%d, mismatches=%d" % (total, formula_ok, mismatches)],
    )


def derive_heap_binding(paths):
    """Which arm sets the shared minimum. The whole forward claim turns on this being srv.

    Both facts here are '30 of 30', and they sit in one sentence. An expectation of a bare "30 of
    30" is satisfied by whichever of them the prose still states truthfully, so falsifying the other
    passes - which is what happened when the binding count was first perturbed to 29 of 30. Each
    count is therefore bound to the words that state it.

    The Workstation attainment count is bound too. Without it the document's "Workstation never
    binds" is unchecked prose sitting beside two checked figures, and it is the half of the sentence
    that a tie falsifies first.
    """
    total, _, shared_ok, srv_binds, wks_binds, ties = _heap_facts(paths)
    return (
        [("rows where the shared minimum is max(wks, srv)",
          ["max(wks, srv)` in %s" % f for f in n_of_m_forms(shared_ok, total)]),
         ("rows where srv attains the shared minimum",
          ["binding arm in %s" % f for f in n_of_m_forms(srv_binds, total)]),
         ("rows where wks attains it",
          ["attains it in %s" % f for f in n_of_m_forms(wks_binds, total)]),
         ("rows where the two arms tie",
          ["tie in %s" % f for f in n_of_m_forms(ties, total)])],
        ["rows=%d, shared == max(wks,srv)=%d" % (total, shared_ok),
         "attains the shared minimum: srv=%d wks=%d, ties=%d" % (srv_binds, wks_binds, ties),
         "binding is attainment (Math.Max), not strict inequality; strict would give srv=%d"
         % sum(1 for r in rows(paths["achieved"])
               if int(r["srvMinimumMb"]) > int(r["wksMinimumMb"]))],
    )


def derive_calibration(paths):
    with open(paths["calibration"], encoding="utf-8") as handle:
        baselines = json.load(handle)["baselines"]
    provisional = [b for b in baselines if b.get("provisional")]
    return (
        [("calibrated scenarios", count_word_forms(len(baselines)))],
        ["baselines=%d provisional=%d converged=%d"
         % (len(baselines), len(provisional), len(baselines) - len(provisional))],
    )


def _arm_minima(paths):
    """Per-scenario workstation and server minima, deduplicated by scenario.

    The achieved CSV carries one row per (scenario, factor), so the minima repeat three times each.
    Deduplicating by scenario is what makes "5 distinct values" and "4 of 10 scenarios" mean what the
    prose says; counting rows would give 15 and 12 and read as a defect in the document.
    """
    by_scenario = {}
    for r in rows(paths["achieved"]):
        by_scenario[r["scenario"]] = (int(r["wksMinimumMb"]), int(r["srvMinimumMb"]))
    wks = sorted({v[0] for v in by_scenario.values()})
    srv = [v[1] for v in by_scenario.values()]
    smallest = min(wks)
    at_smallest = [s for s, v in by_scenario.items() if v[0] == smallest]
    return by_scenario, wks, srv, smallest, at_smallest


def derive_heap_arm_spread(paths):
    """The corrected replacement for a sentence that described a four-row subset as the matrix.

    The false version read "Its minimum is 4 MiB where Server needs 35-43". Both numbers describe
    only the scenarios where wks is at its smallest value; over the whole matrix wks takes five
    values and srv spans a far wider range. Every figure below is derived, so the corrected sentence
    cannot drift back.

    Two of these expectations were weaker than they looked, and a tie probe exposed both. The count
    of distinct values was a bare "5", which the "4" heading the value list satisfies on its own -
    the expected-3-matching-3.3x defect once more. And the value list was a plain substring, so
    "4, 11, 19, 35, 99" is satisfied by a document reading "3, 4, 11, 19, 35, 99": a SUPERSET passes
    a check for the set. Both are now bound to the words that introduce them.
    """
    by_scenario, wks, srv, smallest, at_smallest = _arm_minima(paths)
    values = ", ".join(str(v) for v in wks)
    return (
        [("distinct workstation minima",
          ["%s distinct values" % f for f in count_word_forms(len(wks))]),
         ("the workstation value set, bound to the dash that introduces it so a superset cannot pass",
          ["\u2014 %s MiB" % values, "- %s MiB" % values]),
         ("scenarios at the smallest workstation minimum",
          ["%s" % f for f in n_of_m_forms(len(at_smallest), len(by_scenario))]),
         ("the server range", ["%d\u2013%d" % (min(srv), max(srv)),
                               "%d-%d" % (min(srv), max(srv))])],
        ["wks distinct=%s" % values,
         "wks smallest=%d in %d of %d scenarios (%s)"
         % (smallest, len(at_smallest), len(by_scenario), ", ".join(sorted(at_smallest))),
         "srv range=%d-%d" % (min(srv), max(srv))],
    )


def _ceiling_divisor(paths):
    """The divisor integrality requires, derived from the factors rather than written as 10.

    A factor p/q in lowest terms leaves a remainder unless q divides the minimum; x1.3 is x13/10, so
    q is 10. Writing 10 here beside a derived count would be the same literal-next-to-derived defect
    the document was corrected for - the checker would then hold its own copy of a fact the data
    already states, and would agree with a stale document as happily as with a correct one.

    Returns every non-integral denominator present. The prose's shape assumes exactly one; if the
    matrix ever carries two, that is reported rather than silently reduced to the first.
    """
    denominators = {fractions.Fraction(r["nominalFactor"]).denominator for r in rows(paths["achieved"])}
    return sorted(d for d in denominators if d != 1)


def derive_heap_divisibility(paths):
    """Why exact multiplication fails at 1.3x, stated as the mechanism rather than a correlate.

    x1.3 is x13/10, so an integral product requires the minimum to be divisible by 10. An earlier
    version of the prose said the minima are "odd", which is true of all ten here but is not the
    governing property - 36 is even and 36 x 1.3 = 46.8. Deriving both the count and the divisor
    keeps the document on the mechanism.
    """
    non_integral = _ceiling_divisor(paths)
    if len(non_integral) != 1:
        # A refusal, deliberately unsatisfiable: the sentence names one divisor, so a matrix with
        # none or several means the prose's shape is wrong and no edit to its numbers repairs it.
        return (
            [("exactly one non-integral heap factor", ["<no such divisor: %d found>" % len(non_integral)])],
            ["non-integral factor denominators: %s"
             % (", ".join(str(d) for d in non_integral) if non_integral else "none")],
        )
    divisor = non_integral[0]
    by_scenario = _arm_minima(paths)[0]
    minima = {s: max(v) for s, v in by_scenario.items()}
    divisible = [s for s, m in minima.items() if m % divisor == 0]
    return (
        [("minima divisible by the required divisor",
          ["%s minima are divisible" % f for f in n_of_m_forms(len(divisible), len(minima))]
          + ["`%s` minima are divisible" % f for f in n_of_m_forms(len(divisible), len(minima))]),
         ("the divisor itself, and not a correlate of it",
          ["divisible by %d" % divisor, "divisibility by %d" % divisor])],
        ["divisible by %d: %d of %d" % (divisor, len(divisible), len(minima)),
         "divisor derived from the non-integral factor: %s"
         % ", ".join("%s = %s" % (f, fractions.Fraction(f))
                     for f in sorted({r["nominalFactor"] for r in rows(paths["achieved"])})),
         "shared minima: %s" % ", ".join(str(m) for m in sorted(set(minima.values())))],
    )


CLAIMS = [
    # An anchor locates a sentence; it must not also assert a value. An anchor carrying a derived
    # number - `All ten converged`, `take **five distinct values` - is a second copy of that number
    # inside the checker built to stop the document holding one. It fails in the worse direction:
    # correcting the document as the data moves makes the anchor stop matching, and the claim is
    # then reported as deleted rather than as changed. Every anchor below is literal-free.
    Claim("repro-cells", r"cells disagree with s2 by more than both", derive_repro_cells,
          "section 6.5 headline: N of M cells disagree, with median and max"),
    Claim("offered-load", r"cells had a different offered load", derive_offered_load,
          "section 6.5 decomposition 1: cells excluded for differing offered load"),
    Claim("comparable", r"genuinely comparable cells", derive_comparable,
          "section 6.5 decomposition 2: comparable cells still disagreeing"),
    Claim("direction", r"throughput comparisons moved the same way", derive_direction,
          "section 6.5 decomposition 3: sign test over throughput comparisons"),
    Claim("stability-s3", r"^\| s2 vs s3 ", derive_stability_s3,
          "section 6.5 ratio-stability table, s2 vs s3 row"),
    Claim("stability-s4", r"^\| s2 vs s4sdk ", derive_stability_s4,
          "section 6.5 ratio-stability table, s2 vs s4sdk row"),
    Claim("floor-claim", r"throughput ratios in the s2/s3 comparison drift by less than",
          derive_floor_claim,
          "section 6.5: how many throughput ratios clear the resolution floor"),
    Claim("calibration", r"converged and every entry is", derive_calibration,
          "section 5: every calibrated scenario converged"),
    Claim("heap-formula", r"`heapLimitMb == ceil", derive_heap_formula,
          "section 5.1: every cell's limit is ceil(shared minimum x nominal factor)"),
    Claim("heap-binding", r"is the binding arm in", derive_heap_binding,
          "section 5.1: which arm attains the shared minimum, and how often they tie"),
    Claim("heap-arm-spread", r"minima take \*\*", derive_heap_arm_spread,
          "section 5.1 correction: the workstation value set and the server range"),
    Claim("heap-divisibility", r"minima are divisible by", derive_heap_divisibility,
          "section 5.1: divisibility by the factor's denominator is what integrality requires"),
]


def main():
    # The document is UTF-8 and this script prints its sentences back. On a cp1252 console that
    # raises UnicodeEncodeError on the first arrow or dash, which would abort the gate mid-run and
    # report nothing rather than reporting a result - a checker that dies on the characters of the
    # thing it checks. Degrade the rendering, never the run.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass
    parser = argparse.ArgumentParser()
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(here)
    parser.add_argument("--doc", default=os.path.join(os.path.dirname(root), "P0.5-baselines.md"))
    parser.add_argument("--results", default=root)
    args = parser.parse_args()

    paths = {
        "repro": os.path.join(args.results, "raw", "reproducibility-s2-vs-s3.csv"),
        "ratio_s3": os.path.join(args.results, "raw", "arm-ratio-stability-s2-vs-s3.csv"),
        "ratio_s4": os.path.join(args.results, "raw", "arm-ratio-stability-s2-vs-s4sdk.csv"),
        "calibration": os.path.join(args.results, "calibration.json"),
        "achieved": os.path.join(args.results, "raw", "achieved-heap-factors.csv"),
    }
    missing = [p for p in paths.values() if not os.path.isfile(p)]
    if missing:
        for p in missing:
            print("FAIL missing input: %s" % p)
        return 1
    if not os.path.isfile(args.doc):
        print("FAIL missing document: %s" % args.doc)
        return 1

    with open(args.doc, encoding="utf-8") as handle:
        doc_lines = handle.read().splitlines()

    failures = 0
    for claim in CLAIMS:
        print("claim %-14s %s" % (claim.claim_id, claim.description))
        line_number, sentence = find_anchor(doc_lines, claim.anchor)
        if sentence is None:
            # A claim whose sentence has been deleted must fail, not silently pass.
            print("  FAIL  no sentence in the document matches /%s/" % claim.anchor)
            failures += 1
            print()
            continue
        checks, evidence = claim.derive(paths)
        for line in evidence:
            print("  data   %s" % line)
        print("  doc    :%d %s" % (line_number, sentence[:150]))
        bad = []
        groups = []
        for label, forms in checks:
            key = tuple(forms)
            for existing in groups:
                if existing[0] == key:
                    existing[1].append(label)
                    break
            else:
                groups.append((key, [label]))
        for forms, labels in groups:
            found = occurrence_count(forms, sentence)
            if found < len(labels):
                bad.append("%s (expected %d occurrence(s) of %s, found %d)"
                           % (" and ".join(labels), len(labels), " / ".join(forms), found))
        if bad:
            for b in bad:
                print("  FAIL  prose does not carry %s" % b)
            failures += 1
        else:
            print("  ok     all %d derived figures present in the prose" % len(checks))
        print()

    checked = len(CLAIMS)
    if failures:
        print("RESULT: FAIL (%d of %d prose claims disagree with the data)" % (failures, checked))
        return 1
    print("RESULT: PASS (%d prose claims re-derived from the shipped CSVs)" % checked)
    return 0


if __name__ == "__main__":
    sys.exit(main())
