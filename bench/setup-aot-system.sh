mono --aot -O=all ../bin/Debug/Calctus.exe

sudo mono --aot -O=all /usr/local/lib/mono/4.5/mscorlib.dll
for i in /usr/local/lib/mono/gac/*/*/*.dll; do sudo mono --aot -O=all $i; done
