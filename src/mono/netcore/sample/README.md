## This is simple Hello World sample running Mono within .NET Core

## Steps required

### Build Mono

```
./configure --with-core=only
make
```

### Prepare dependencies

```
make prepare
```

### Run the sample

```
make run
```
