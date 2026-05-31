#!/usr/bin/env python3
"""
Port Bazel BUILD.bazel files under src/tests/ to BXL BUILD.dsc.

Reads a tree of BUILD.bazel files (e.g. extracted from `bazel-main` branch)
and emits a BUILD.dsc for each into a target source tree (typically the
working checkout). Only `coreclr_test` rules are emitted; `il_coreclr_test`,
`coreclr_merged_test`, `live_csharp_library`, and `csharp_library` are
silently skipped (the BXL macro layer doesn't support them yet).

Dependencies that the BXL `coreclr_test` macro already adds implicitly
(TestLibrary, the xunit packages, framework refs from CORECLR_TEST_COMMON_DEPS)
are dropped. Local in-package `:foo` deps are unresolvable without porting
sibling rules, so any test relying on them is skipped with a comment.

Targets marked `target_compatible_with` for non-Linux platforms only are
skipped entirely. Targets tagged "manual" are emitted but with `run: false`.
"""
from __future__ import annotations

import argparse
import ast
import os
import re
import sys
from pathlib import Path

# Deps the BXL coreclr_test macro adds implicitly — drop these if seen.
_IMPLICIT_DEP_PREFIXES = (
    "//src/tests/Common:TestLibrary",
    "//src/tests/Common:XUnitWrapperLibrary",
    "//src/tests/Common:XUnitWrapperGenerator",
    "@paket.main//microsoft.dotnet.xunitextensions",
    "@paket.main//microsoft.dotnet.xunitassert",
    "@paket.main//xunit.abstractions",
    "@paket.main//xunit.extensibility.core",
)

# Library framework refs are already in Defs.CORECLR_TEST_COMMON_DEPS for BXL.
_LIBRARY_REF_RE = re.compile(r"^//src/libraries/[^:]+:ref_")

# Only build on Linux today.
_HOST_PLATFORM_LABELS = {
    "@platforms//os:linux",
}

_SUPPORTED_RULES = {"coreclr_test", "il_coreclr_test"}

# Cross-package source labels mapped to filegroup artifact spreads.
# These labels are no longer allowed in resolveLabel(); instead the
# owning package exports a filegroup and consumers spread its .srcs.
_FILEGROUP_MAP = {
    "//src/tests/async:RuntimeAsyncMethodGenerationAttribute.cs":
        "...asyncSharedSrcs.srcs",
    "//src/tests/JIT/Directed/nullabletypes/Desktop:StructDefinitions.cs":
        "...nullabletypesDesktopSharedSrcs.srcs",
    "//src/tests/JIT/HardwareIntrinsics/General/Shared:Program.cs":
        "...hwintGeneralSharedSrcs.srcs",
    "//src/tests/JIT/HardwareIntrinsics/X86/Shared:Program.cs":
        "...x86SharedProgram.srcs",
    "//src/tests/JIT/HardwareIntrinsics/X86/Shared:SimpleBinOpTest_DataTable.cs":
        "...x86SharedSimpleBinOpDataTable.srcs",
    "//src/tests/JIT/HardwareIntrinsics/X86/Shared:SimpleUnOpTest_DataTable.cs":
        "...x86SharedSimpleUnOpDataTable.srcs",
}

# Per-rule macro name to emit on the BXL side.
_BXL_MACRO = {
    "coreclr_test": "coreclr_test",
    "il_coreclr_test": "il_coreclr_test",
}

# Per-rule whitelist of bazel attrs that we know how to forward.
_KNOWN_ATTRS_BY_RULE = {
    "coreclr_test": {
        "name", "srcs", "optimize", "allow_unsafe_blocks", "defines",
        "nowarn", "env", "pri", "size", "debug_type", "tags",
        "target_compatible_with", "compiler_options", "test_deps",
        "async", "flaky", "nullable", "visibility", "deps",
    },
    "il_coreclr_test": {
        "name", "srcs", "debug_type", "optimize", "env", "pri", "size",
        "tags", "target_compatible_with", "flaky", "visibility", "deps",
    },
}


class SkipTarget(Exception):
    """Raised when a target cannot be ported and should be skipped."""


def _classify_dep(dep: str, rule: str) -> str:
    """Return 'drop', 'keep', or 'unsupported' for a bazel dep label."""
    if dep.startswith(_IMPLICIT_DEP_PREFIXES):
        return "drop"
    if _LIBRARY_REF_RE.match(dep):
        return "drop"
    # IL tests use deps as runtime DLLs symlinked next to the test binary.
    # //src/tests/Common:TestLibrary is already in Core_Root, so it resolves
    # at runtime via corerun's TPA — drop it for IL tests instead of failing.
    if rule == "il_coreclr_test" and dep == "//src/tests/Common:TestLibrary":
        return "drop"
    if dep.startswith(":"):
        return "unsupported"
    return "unsupported"


def _parse_call_attrs(text: str) -> dict:
    """Parse a single rule call body into a kwarg dict using Python's AST."""
    expr = "f(" + text + ")"
    tree = ast.parse(expr, mode="eval")
    call = tree.body
    if not isinstance(call, ast.Call):
        raise SkipTarget("not a call")
    attrs = {}
    for kw in call.keywords:
        attrs[kw.arg] = kw.value
    return attrs


def _eval_literal(node, *, pkg_dir: Path):
    """Evaluate an AST node into a Python value, supporting glob([...])."""
    if isinstance(node, ast.Constant):
        return node.value
    if isinstance(node, (ast.List, ast.Tuple)):
        return [_eval_literal(e, pkg_dir=pkg_dir) for e in node.elts]
    if isinstance(node, ast.Dict):
        return {
            _eval_literal(k, pkg_dir=pkg_dir): _eval_literal(v, pkg_dir=pkg_dir)
            for k, v in zip(node.keys, node.values)
        }
    if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.USub):
        return -_eval_literal(node.operand, pkg_dir=pkg_dir)
    if isinstance(node, ast.BinOp) and isinstance(node.op, ast.Add):
        return _eval_literal(node.left, pkg_dir=pkg_dir) + _eval_literal(node.right, pkg_dir=pkg_dir)
    if isinstance(node, ast.Call):
        func_name = getattr(node.func, "id", None)
        if func_name == "glob":
            patterns = _eval_literal(node.args[0], pkg_dir=pkg_dir)
            ae = False
            for kw in node.keywords:
                if kw.arg == "allow_empty":
                    ae = _eval_literal(kw.value, pkg_dir=pkg_dir)
            return _expand_glob(patterns, pkg_dir, ae)
    raise SkipTarget(f"unsupported expression: {ast.dump(node)}")


def _expand_glob(patterns, pkg_dir: Path, allow_empty: bool) -> list:
    results = []
    seen = set()
    for pat in patterns:
        if "**" in pat:
            prefix, suffix = pat.split("**", 1)
            suffix = suffix.lstrip("/")
            for path in pkg_dir.rglob(suffix or "*"):
                if path.is_file():
                    rel = path.relative_to(pkg_dir).as_posix()
                    if rel not in seen:
                        seen.add(rel)
                        results.append(rel)
        else:
            for path in sorted(pkg_dir.glob(pat)):
                if path.is_file():
                    rel = path.relative_to(pkg_dir).as_posix()
                    if rel not in seen:
                        seen.add(rel)
                        results.append(rel)
    if not results and not allow_empty:
        raise SkipTarget(f"glob expanded empty: {patterns}")
    return sorted(results)


# Map bazel attr name -> BXL macro attr name (camelCase).
_ATTR_MAP = {
    "name": "name",
    "srcs": "srcs",
    "optimize": "optimize",
    "allow_unsafe_blocks": "allowUnsafe",
    "defines": "defines",
    "nowarn": "nowarn",
    "env": "env",
    "pri": "pri",
    "size": "size",
    "debug_type": "debugType",
    "tags": "tags",
    "target_compatible_with": "targetCompatibleWith",
    "compiler_options": "compilerOptions",
    "test_deps": "testDeps",
    "async": "async_",
    "flaky": "flaky",
    "nullable": "nullable",
    "visibility": "visibility",
}

_KNOWN_BAZEL_ATTRS = set(_ATTR_MAP) | {"deps"}


class _RawExpr:
    """A raw DScript expression that should not be quoted."""
    def __init__(self, expr: str):
        self.expr = expr

def _emit_value(val) -> str:
    if val is None:
        return "undefined"
    if isinstance(val, _RawExpr):
        return val.expr
    if isinstance(val, bool):
        return "true" if val else "false"
    if isinstance(val, (int, float)):
        return repr(val)
    if isinstance(val, str):
        return '"' + val.replace("\\", "\\\\").replace('"', '\\"') + '"'
    if isinstance(val, list):
        return "[" + ", ".join(_emit_value(v) for v in val) + "]"
    if isinstance(val, dict):
        parts = ", ".join(f"{k}: {_emit_value(v)}" for k, v in val.items())
        return "{" + parts + "}"
    raise SkipTarget(f"can't emit value type {type(val).__name__}: {val!r}")


def _emit_env(env_val):
    if isinstance(env_val, list):
        return env_val
    if isinstance(env_val, dict):
        return [{"name": k, "value": v} for k, v in env_val.items()]
    raise SkipTarget(f"unsupported env shape: {env_val!r}")


_RULE_HEAD_RE = re.compile(r"^([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", re.MULTILINE)


def _iter_rules(text: str):
    """Yield (rule_name, body_text) for each top-level rule call."""
    pos = 0
    while True:
        m = _RULE_HEAD_RE.search(text, pos)
        if not m:
            return
        depth = 1
        start = m.end()
        i = start
        in_str = None
        emitted = False
        while i < len(text):
            c = text[i]
            if in_str:
                if c == "\\":
                    i += 2
                    continue
                if c == in_str:
                    in_str = None
            else:
                if c in ('"', "'"):
                    in_str = c
                elif c == "(":
                    depth += 1
                elif c == ")":
                    depth -= 1
                    if depth == 0:
                        yield m.group(1), text[start:i]
                        pos = i + 1
                        emitted = True
                        break
            i += 1
        if not emitted:
            return


_IDENT_RE = re.compile(r"[^A-Za-z0-9_]")

# DScript / TypeScript / strict-mode JS reserved words. Avoid emitting these
# as `export const <ident>` bindings; suffix with `_` if hit.
_RESERVED = frozenset("""
abstract any as async await boolean break byte case catch char class const
continue debugger declare default delete do double else enum export extends
false final finally float for from function get goto if implements import
in instanceof int interface is keyof let long module namespace native never
new null number of package private protected public readonly require return
set short static string super switch symbol synchronized this throw throws
transient true try type typeof undefined unique unknown var void volatile
while with yield
""".split())


def _identifier(name: str) -> str:
    s = _IDENT_RE.sub("_", name)
    if not s:
        s = "test"
    if s[0].isdigit():
        s = "_" + s
    s = s[0].lower() + s[1:]
    if s in _RESERVED:
        s = s + "_"
    return s


def _unique_identifier(name: str, pkg_rel: str) -> str:
    """
    Generate a module-globally-unique export identifier.

    The Tests module uses `implicitProjectReferences` semantics, which
    means every `export const` lives in the module's shared namespace.
    Mechanically porting thousands of files inevitably produces colliding
    short names like `test`, `case1`, `basic`, etc., so we prefix every
    identifier with a sanitized version of the package path.
    """
    prefix = _IDENT_RE.sub("_", pkg_rel).strip("_")
    base = _identifier(name)
    if not prefix:
        return base
    # Lowercase first letter of joined form.
    joined = f"{prefix}_{base}"
    return joined[0].lower() + joined[1:]


_EXISTING_DISABLED_RE = re.compile(
    r"export\s+const\s+\w+\s*=\s*CoreClr\.(?:coreclr_test|il_coreclr_test)\(\s*\{"
    r"(?P<body>(?:[^{}]|\{[^{}]*\})*)"
    r"\}\s*\)",
    re.MULTILINE,
)


def _existing_disabled_names(target_dir: Path) -> set:
    """
    Return the set of test names in an existing generated BUILD.dsc that
    carry `run: false`. These were added by `disable_failed_tests.py` to
    suppress failing tests and must be preserved across re-generation.
    """
    out_path = target_dir / "BUILD.dsc"
    if not out_path.exists():
        return set()
    text = out_path.read_text()
    if "Generated by eng/bxl/port_bazel_tests.py" not in text:
        return set()
    disabled: set = set()
    for m in _EXISTING_DISABLED_RE.finditer(text):
        body = m.group("body")
        if not re.search(r"\brun\s*:\s*false\b", body):
            continue
        nm = re.search(r'\bname\s*:\s*"([^"]+)"', body)
        if nm:
            disabled.add(nm.group(1))
    return disabled


def convert_file(src_build: Path, target_dir: Path, *, pkg_rel: str = "", workspace_root: Path) -> tuple[int, int, list]:
    text = src_build.read_text()
    emitted = 0
    skipped = 0
    notes: list = []
    out_lines: list = []
    used_ids: set = set()
    preserved_disabled = _existing_disabled_names(target_dir)

    for rule, body in _iter_rules(text):
        if rule == "load":
            continue
        if rule not in _SUPPORTED_RULES:
            if rule in ("coreclr_merged_test",
                       "live_csharp_library", "csharp_library"):
                notes.append(f"skipped {rule}")
                skipped += 1
            continue
        try:
            attrs_ast = _parse_call_attrs(body)
            name_node = attrs_ast.get("name")
            if name_node is None:
                raise SkipTarget("no name")
            name = _eval_literal(name_node, pkg_dir=target_dir)

            if "target_compatible_with" in attrs_ast:
                tcw = _eval_literal(attrs_ast["target_compatible_with"], pkg_dir=target_dir)
                if not any(label in _HOST_PLATFORM_LABELS for label in tcw):
                    notes.append(f"{name}: incompatible target_compatible_with {tcw}")
                    skipped += 1
                    continue

            kept_deps: list = []
            if "deps" in attrs_ast:
                deps_val = _eval_literal(attrs_ast["deps"], pkg_dir=target_dir)
                for dep in deps_val:
                    c = _classify_dep(dep, rule)
                    if c == "drop":
                        continue
                    if c == "unsupported":
                        raise SkipTarget(f"unsupported dep {dep!r}")
                    kept_deps.append(dep)
                if kept_deps:
                    raise SkipTarget(f"non-implicit deps remain: {kept_deps}")

            known_attrs = _KNOWN_ATTRS_BY_RULE[rule]
            out_attrs: dict = {}
            for k, v_node in attrs_ast.items():
                if k == "deps":
                    continue
                if k not in known_attrs:
                    raise SkipTarget(f"unknown attr {k!r} for rule {rule}")
                val = _eval_literal(v_node, pkg_dir=target_dir)
                if k == "env":
                    val = _emit_env(val)
                if k == "srcs":
                    # Cross-package srcs ("//pkg:file.cs") are mapped to
                    # filegroup artifact spreads. Unknown cross-package labels
                    # are rejected — add them to _FILEGROUP_MAP first.
                    # External-package labels ("@pkg//...") aren't valid as
                    # srcs here and are skipped.
                    resolved = []
                    for s in val:
                        if not isinstance(s, str):
                            raise SkipTarget(f"non-string src: {s!r}")
                        if s.startswith("@"):
                            raise SkipTarget(f"external-package src: {s!r}")
                        if s.startswith("//"):
                            if s in _FILEGROUP_MAP:
                                resolved.append(_RawExpr(_FILEGROUP_MAP[s]))
                            else:
                                raise SkipTarget(f"unmapped cross-package src: {s!r}")
                        elif s.startswith(":"):
                            if not (target_dir / s[1:]).exists():
                                raise SkipTarget(f"missing local src: {s!r}")
                            resolved.append(s)
                        else:
                            if not (target_dir / s).exists():
                                raise SkipTarget(f"missing local src: {s!r}")
                            resolved.append(s)
                    val = resolved
                out_attrs[_ATTR_MAP[k]] = val

            export_id = _unique_identifier(name, pkg_rel)
            # Disambiguate identifiers within a file.
            base_id = export_id
            n = 2
            while export_id in used_ids:
                export_id = f"{base_id}_{n}"
                n += 1
            used_ids.add(export_id)

            attr_lines = [f"    name: {_emit_value(name)},"]
            if "srcs" in out_attrs:
                attr_lines.append(f"    srcs: {_emit_value(out_attrs.pop('srcs'))},")
            out_attrs.pop("name", None)
            # Preserve `run: false` markers added by disable_failed_tests.py.
            if name in preserved_disabled and "run" not in out_attrs:
                out_attrs["run"] = False
            for k in sorted(out_attrs.keys()):
                attr_lines.append(f"    {k}: {_emit_value(out_attrs[k])},")
            macro = _BXL_MACRO[rule]
            out_lines.append(
                f"@@public\nexport const {export_id} = {macro}({{\n"
                + "\n".join(attr_lines)
                + "\n});\n"
            )
            emitted += 1
        except SkipTarget as e:
            notes.append(f"skipped target: {e}")
            skipped += 1

    if emitted == 0:
        return 0, skipped, notes

    header = (
        "// Copyright (c) Microsoft Corporation.\n"
        "// Licensed under the MIT License.\n"
        "//\n"
        "// Generated by eng/bxl/port_bazel_tests.py from BUILD.bazel.\n"
        "// Hand-edit only if you also remove the generator from this dir.\n"
        "\n"
    )
    target_dir.mkdir(parents=True, exist_ok=True)
    out_path = target_dir / "BUILD.dsc"
    # Don't clobber hand-curated specs. They are identified by the absence of
    # the generator marker in their header.
    if out_path.exists():
        existing = out_path.read_text()
        if "Generated by eng/bxl/port_bazel_tests.py" not in existing:
            return 0, skipped, notes + [f"preserved hand-curated {out_path}"]
    out_path.write_text(header + "\n".join(out_lines))
    return emitted, skipped, notes


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--source-tree", required=True)
    p.add_argument("--target-tree", required=True)
    p.add_argument("--subdir", default="src/tests")
    p.add_argument("--verbose", action="store_true")
    args = p.parse_args()

    src_root = Path(args.source_tree) / args.subdir
    tgt_root = Path(args.target_tree) / args.subdir
    tot_emitted = 0
    tot_skipped = 0
    files_written = 0
    files_seen = 0
    fully_skipped = 0
    for build_path in sorted(src_root.rglob("BUILD.bazel")):
        rel = build_path.parent.relative_to(src_root)
        files_seen += 1
        if rel.parts[:1] in (("Common",), ("coreclr_test",)):
            continue
        target_dir = tgt_root / rel
        # Only emit if the target source dir already exists in the checkout.
        if not target_dir.exists():
            if args.verbose:
                print(f"  skip (no target dir): {rel}")
            continue
        emitted, skipped, notes = convert_file(build_path, target_dir, pkg_rel=str(rel), workspace_root=Path(args.target_tree))
        tot_emitted += emitted
        tot_skipped += skipped
        if emitted:
            files_written += 1
        else:
            fully_skipped += 1
        if args.verbose and (emitted or skipped):
            print(f"{rel}: emit={emitted} skip={skipped}")
            for n in notes:
                print(f"  {n}")

    print(f"BUILD.bazel files scanned: {files_seen}")
    print(f"BUILD.dsc files written:   {files_written}")
    print(f"Source files with no emit: {fully_skipped}")
    print(f"Targets emitted:           {tot_emitted}")
    print(f"Targets skipped:           {tot_skipped}")


if __name__ == "__main__":
    sys.exit(main() or 0)
