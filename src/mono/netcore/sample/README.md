## This is simple Hello World sample running Mono within .NET Core

## Steps required

### Build Mono

```
./configure --with-core=yes
make
```

This will fail but that's ok at this point

### Prepare dependencies

```
make prepare
```

### Run the sample

```
make run
```
