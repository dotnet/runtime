# Generating the Parser using Bison

- Bison for Windows: https://github.com/lexxmark/winflexbison
- Other platforms: https://www.gnu.org/software/bison

To generate `asmparse.cpp`, run either of following:
- Unix: `yacc asmparse.y -o asmparse.cpp`
- Windows: `win_bison asmparse.y -o asmparse.cpp`

