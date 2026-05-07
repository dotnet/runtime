# .NET Metadata Overview

## What is Metadata?

Metadata is binary information that describes a program's structure. When .NET code is compiled into a portable executable (PE) file, metadata is inserted into one section of the file, while the code is converted to Common Intermediate Language (CIL) and placed in another. Every type, method, field, and member reference in the program is described within metadata.

In simple terms, metadata is the "table of contents" of a .NET assembly — it tells the runtime (and other tools like compilers, debuggers, and the AOT compiler) what types exist, what methods they have, what their signatures are, and how they relate to each other.

## Why Does Metadata Exist?

Before .NET, using a component written in one language from another language was difficult. C++ programs needed header files, COM components needed Interface Definition Language (IDL) files, and there was no universal way for compiled binaries to describe themselves.

Metadata solves this by making .NET assemblies **self-describing**. A compiled module contains everything needed to understand and interact with it, without external files. This enables:

- **Runtime services:** The garbage collector, JIT compiler, debugger, and reflection all rely on metadata to understand the program's structure.
- **Tooling:** Tools like the IL Disassembler (`ildasm`), the AOT compiler (ILC), and Visual Studio IntelliSense all consume metadata to provide their functionality.

## Metadata Tables and Heaps

Metadata is essentially a set of **tables** and **heaps**:

- **Tables** hold structured rows of information. Each table describes a specific kind of element. Some of the key tables include:

  | Table | Describes |
  |-------|-----------|
  | `TypeDef` | Types defined in the module |
  | `TypeRef` | Types referenced from other modules |
  | `MethodDef` | Methods defined in the module |
  | `Field` | Fields defined in the module |
  | `MemberRef` | References to methods/fields in other modules |
  | `AssemblyRef` | Referenced assemblies |
  | `InterfaceImpl` | Which interfaces a type implements |
  | `CustomAttribute` | Custom attributes applied to any element |

  For example, the `MethodDef` table has the following structure:

  | Column | Description |
  |--------|-------------|
  | RVA | Relative Virtual Address pointing to the method's CIL body |
  | ImplFlags | Implementation flags (e.g., IL, native, managed, unmanaged) |
  | Flags | Method attributes (e.g., public, private, static, virtual) |
  | Name | Index into the string heap for the method's name |
  | Signature | Index into the blob heap for the method's signature (return type, parameter types) |
  | ParamList | Index into the `Param` table for the method's parameters |

  Each row in this table describes one method. For instance, a simple `public static float ComputeArea(int width, int height)` method would appear as a single row with its RVA pointing to the CIL instructions, flags indicating `Public Static`, its name (`ComputeArea`) stored in the string heap, and its signature (`float, int, int`) stored in the blob heap.

  Similarly, each metadata table has its own structure, with columns that often point to other tables or into the heaps. This creates a web of cross-references — for example, a `TypeDef` row points into the `MethodDef` table to list its methods, and each `MethodDef` row points into the string heap for its name.

- **Heaps** store variable-length data that tables point into:
  - **String heap:** names of types, methods, fields, etc.
  - **Blob heap:** method signatures, field signatures, and other binary data.
  - **GUID heap:** unique identifiers.
  - **User string heap:** string literals used in code.

## Metadata Tokens

Each row in a metadata table is identified by a **metadata token** — a four-byte value where the top byte identifies the table and the lower three bytes identify the row. For example, the token `0x06000003` refers to the third row of the `MethodDef` table (table `0x06`). CIL instructions use these tokens to reference types, methods, and fields.

## Putting It All Together

Consider the following C# code:

```csharp
public class Person
{
    public string Name;
    public int[] Scores;

    public float GetAverageScore()
    {
        int sum = 0;
        for (int i = 0; i < Scores.Length; i++)
            sum += Scores[i];
        return (float)sum / Scores.Length;
    }
}
```

When the compiler produces metadata for this code, it creates entries in several tables:

- **`TypeDef`** — one row for `Person`.
- **`Field`** — two rows: one for `Name`, one for `Scores`.
- **`MethodDef`** — two rows: one for `GetAverageScore`, and one for the implicit `.ctor` (constructor) that the compiler generates.
- **`TypeRef`** — rows referencing external types used in the code: `System.Object` (the base class), `System.String`, `System.Int32`, `System.Single`, and `System.Array`.
- **`MemberRef`** — a row referencing `System.Array.get_Length`, since the code calls `Scores.Length`.

Each of these rows has an implicit metadata token. For instance, if `GetAverageScore` is the first method defined in the module, its token would be `0x06000001` (`0x06` for the `MethodDef` table, `000001` for the first row). CIL instructions within other methods that call `GetAverageScore` would reference it by this token.

Notice that `int[]` itself does not get a `TypeDef` row — array types are constructed by the runtime (and the type system) on demand, and their methods like `Get`, `Set`, and `Address` are synthesized rather than stored in metadata.

## Further Reading

- [Metadata and Self-Describing Components](https://learn.microsoft.com/dotnet/standard/metadata-and-self-describing-components) on Microsoft Learn — an approachable introduction with worked examples.
- [ECMA-335 specification](https://www.ecma-international.org/publications-and-standards/standards/ecma-335) — the formal specification. Partition II covers metadata definition and semantics in full detail.
