---
applyTo: "src/native/minipal/**"
---

# minipal

Also apply `native` and `conventions`.

- **Document every public declaration with a Doxygen-style comment.** Use a `/** */` block in the
  header: a one-line summary, then `@param` for each parameter and `@return` for non-void functions
  (see `utf8.h`). Describe platform differences (for example "Does nothing on Android") and error
  reporting (`errno`, return codes).
- **Keep the docs in sync with the implementation.** When behavior, flags or platforms change in the
  `.c` file, update the header comment in the same change.
- **Name include guards `HAVE_MINIPAL_<FILE>_H`** and close them with `#endif // HAVE_MINIPAL_<FILE>_H`.
