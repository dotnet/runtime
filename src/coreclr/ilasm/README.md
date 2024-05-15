# Generating the Parser using Bison

- Bison for Windows: https://github.com/lexxmark/winflexbison
- Other platforms: https://www.gnu.org/software/bison

To generate `asmparse.cpp`, run either of following:
- Unix: `yacc asmparse.y -o prebuilt/asmparse.cpp`
- Windows: `win_bison asmparse.y -o prebuilt\asmparse.cpp`

## Docker
```bash
$ cd runtime

# run a throw-away-after-exit container with --rm
$ docker run --rm -v$(pwd):/runtime -w /runtime/src/coreclr/ilasm alpine \
    sh -c 'apk add bison && yacc asmparse.y -o prebuilt/asmparse.cpp'
```

