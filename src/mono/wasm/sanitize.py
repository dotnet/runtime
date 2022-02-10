import sys, json, os, shutil

emsdk_path = sys.argv[1]
emscripten_path = os.path.join(emsdk_path, "upstream/emscripten")

def glob(path):
    return [os.path.join(path, filename) for filename in os.listdir(path)]

def remove(*paths):
    for path in paths:
        path = os.path.abspath(path)
        try:
            if os.path.isdir(path):
                shutil.rmtree(path)
            else:
                os.remove(path)
        except OSError as error:
            print(error)

def rewrite_package_json(path):
    package = open(path,"rb+")
    settings = json.load(package)
    settings["devDependencies"] = {}
    package.seek(0)
    package.truncate()
    json.dump(settings, package, indent=4)
    package.close()

os.chdir(emscripten_path)
rewrite_package_json("package.json")

try:
    os.system("npm prune --production")
except:
    print("npm prune failed")

remove("tests",
    "node_modules/google-closure-compiler",
    "node_modules/google-closure-compiler-java",
    "node_modules/google-closure-compiler-osx",
    "node_modules/google-closure-compiler-windows",
    "node_modules/google-closure-compiler-linux",
    "third_party/closure-compiler",
    "third_party/jni",
    "third_party/ply",
    "third_party/uglify-js",
    "third_party/websockify")

for node_path in glob(os.path.join(emsdk_path, "node")):
    os.chdir(node_path)
    remove("bin/npx",
        "bin/npm",
        "bin/node_modules",
        "include",
        "lib",
        "share")