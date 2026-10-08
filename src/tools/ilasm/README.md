# ILAssembler Build Workflow

This directory contains the ILAssembler tool and its build instructions.

## Build Instructions

### Regular Builds
For everyday development and regular builds, simply run:

```
./dotnet.sh build src/tools/ilasm/src/ILAssembler
```

### Updating Generated Files
If you modify any `.g4` grammar files (rare), you must regenerate the parser and related files:

```
./dotnet.sh build src/tools/ilasm/src/ILAssembler/gen
```

This will update the generated files before building the main project.

## Debug information (PDB)

ilasm writes debug information as a separate Portable PDB file beside the output, as native ilasm does for an output with an extension, and the image gets the same debug directory entry types in the same order. The PDB's content is narrower than native ilasm's: it records only the documents and sequence points given by `.line` directives (native ilasm also records the `.il` source file and its lines) and has no local scopes.

- **Which switches produce a PDB.** `--debug` (`-DEBUG`), `--debug-mode impl|opt` (`-DEBUG=IMPL`, `-DEBUG=OPT`) and `--pdb` (`-PDB`). Without one of them there is no PDB and no debug directory: `.line` directives are still parsed and checked, but their sequence points are not emitted.
- **Where it goes.** The PDB is a separate file named after the output with its extension replaced by `.pdb`, in the same directory (`Min.pdb` for `Min.dll` or `Min`). For an output without an extension in a directory whose name has a dot, native ilasm differs: it cuts the whole path at its last dot, so `-OUTPUT=a.b/Min` gives it `a.pdb` beside the `a.b` directory, where ilasm writes `a.b/Min.pdb`. The PDB is not embedded in the image. ilasm writes and closes the image first, then writes the PDB to a new temporary file in the same directory and renames it to `<output>.pdb`, so `<output>.pdb` never holds a partial PDB. If the PDB cannot be written, the run fails and the new image sits beside the previous `<output>.pdb`, if there was one; ilasm tries to delete the temporary file, which can remain if that fails or if the process ends before the rename.
- **An output named like its PDB.** If the output path itself ends in `.pdb` (`-OUTPUT=Min.pdb`) and a PDB is requested, the PDB would overwrite the image, so ilasm reports an error and writes nothing. The comparison ignores case on Windows and macOS.
- **What the image records.** The image's debug directory has these entries, in this order: a CodeView entry that names the PDB file and carries the PDB's id; a PdbChecksum entry with the SHA-256 hash of the PDB file with its 20-byte id zeroed; and, with `--deterministic`, a Reproducible entry. Debuggers and symbol tools use them to find the PDB and to check that it matches the image. Without `--deterministic`, the CodeView entry names the full path of the PDB file, as native ilasm does. With `--deterministic`, it names only the PDB's file name and extension (`Min.pdb`), because a deterministic image must not depend on the directory it is written to. Debuggers look for a PDB of that name beside the image, symbol servers look it up by that name and the PDB id, and the PdbChecksum entry verifies that the PDB found matches the image. Here ilasm deliberately differs from native ilasm, which records the full path in both modes, and follows the native linker's `/PDBALTPATH:%_PDB%` convention instead. The PDB file itself is written beside the output in both modes. When no PDB is produced, the image has no debug directory, with or without `--deterministic`.
- **`--pdb` compared with `--debug`.** `--pdb` produces the PDB but does not itself add a `DebuggableAttribute`, so on its own it does not change how the JIT compiles the assembly; with `--debug` as well, the attribute is still added. `--debug` adds a `DebuggableAttribute` that disables JIT optimization (`0x101`). `--debug-mode impl` (`-DEBUG=IMPL`) also tells the JIT to use implicit sequence points instead of those in the PDB (`0x103`), and `--debug-mode opt` (`-DEBUG=OPT`) keeps JIT optimization enabled with implicit sequence points (`0x3`). These are native ilasm's values. The attribute is added only when the source declares an assembly (`.assembly`).
- **Stale PDBs.** When ilasm writes an image without a PDB, because none was requested, it then deletes an existing `<output>.pdb` only if that PDB belongs to the image being replaced: the image previously at the output path names the PDB's id in its CodeView entry. This keeps a PDB from an earlier build from sitting beside the new image. Any other file at that path is kept: a PDB beside an output that did not exist before, the PDB of another image, or a file that is not a Portable PDB. An output that is itself named like its PDB (`-OUTPUT=Min.pdb`) is the new image and is not deleted. A PDB that cannot be deleted is left in place without failing the run. When the assembly fails and produces no output, ilasm writes nothing and deletes nothing; with `--error` (`-ERR`) an image is written despite the errors, and its PDB is handled as for a successful assembly. If writing the image itself fails, the existing PDB is left as it was, and the image may be partial.
- **`--deterministic`.** The PDB id and the image's MVID and timestamp are derived from content hashes, so the same input assembled to an output with the same file name gives byte-identical image and PDB files, in any directory: the CodeView entry records only the PDB's file name.

---

For more details, see the main repository README or contact the maintainers.
