Building LLDB
=============

1. Clone the llvm, clang, and lldb repos like this:

        llvm
        |
        `-- tools
            |
            +-- clang
            |
            `-- lldb

   ```
	cd $HOME
	git clone http://llvm.org/git/llvm.git
	cd $HOME/llvm/tools
	git clone http://llvm.org/git/clang.git 
	git clone http://llvm.org/git/lldb.git 
   ```

2. Checkout the "release_39" branches in llvm/clang/lldb:

   ```
   cd $HOME/llvm
   git checkout release_39
   cd $HOME/llvm/tools/clang
   git checkout release_39
   cd $HOME/llvm/tools/lldb
   git checkout release_39 
   ```

3. Install the prerequisites:

   For Linux (Debian or Ubuntu):
   ```
   sudo apt-get install build-essential subversion swig python2.7-dev libedit-dev libncurses5-dev
   ```
   
   For OSX, the latest Xcode needs to be installed and I use Homebrew to install the rest:
   ```     
   brew install python swig doxygen ocaml
   ```
   
   There may be more prerequisites required, when building the cmake files it should let
   you know if there are any I missed.

   See http://lldb.llvm.org/build.html for more details on these preliminaries.

4. If building on OSX, carefully following the signing directions (before you build) 
   here: $HOME/llvm/tools/lldb/docs/code-signing.txt. Even though those build directions
   say to use Xcode to build lldb, I never got it to work, but cmake/make works.

5. Building the cmake files (you can build either debug or release or both).

   For debug:
   ```
   mkdir -p $HOME/build/debug    
   cd $HOME/build/debug
   cmake -DCMAKE_BUILD_TYPE=debug $HOME/llvm
   ```
   For release:
   ```
   mkdir -p $HOME/build/release    
   cd $HOME/build/release
   cmake -DCMAKE_BUILD_TYPE=release $HOME/llvm
   ```
6. Build lldb (release was picked in this example, but can be replaced with "debug"):
   ```
   cd $HOME/build/release/tools/lldb
   make -j16
   ```
   When you build with -j16 (parallel build with 16 jobs), sometimes it fails. Just start again with just make.

   For OS X, building in remote ssh shell won't sign properly, use a terminal window on the machine itself.

7. To use the newly built lldb and to build the coreclr SOS plugin for it, set these environment variables in your .profile:
   ```
   export LLDB_INCLUDE_DIR=$HOME/llvm/tools/lldb/include
   export LLDB_LIB_DIR=$HOME/build/release/lib
   PATH=$HOME/build/release/bin:$PATH
   ```
   For OS X also set:
   ```
   export LLDB_DEBUGSERVER_PATH=$HOME/build/release/bin/debugserver
   ```
   It also seems to be necessary to run lldb as superuser e.g. `sudo -E $HOME/build/release/bin/lldb` (the -E is necessary so the above debug server environment variable is passed) if using a remote ssh, but it isn't necessary if run it in a local terminal session.
