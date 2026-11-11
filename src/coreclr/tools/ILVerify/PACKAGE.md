## About

ILVerify is a cross-platform command-line tool for verifying .NET intermediate language (IL) against the ECMA-335 specification.

## How to Use

Install ILVerify as a global .NET tool:

```shell
dotnet tool install --global dotnet-ilverify
```

Run `ilverify --help` for usage information. All dependencies of the assembly being verified must be specified using the `--reference` option.
