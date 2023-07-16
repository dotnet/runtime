## About

The package is obsolete and not necessary. It should NOT be used. If you use it anyway (e.g. due to dependencies out of your control), it won't cause you any harm.

It is a result of failed attempt from 2016 to deliver value to .NET Framework customers outside of official .NET Framework updates. Later we realized it causes more troubles we didn't foresee, therefore we stopped shipping it for .NET Core entirely (the package will just forward to the in-box implementation) and on .NET Framework we made it exact copy of in-box code. As a result, using it does not bring any value, but does not cause any harm either.
For more details, see [comment on GitHub issue dotnet/runtime#20777](https://github.com/dotnet/runtime/issues/20777#issuecomment-338418610)
