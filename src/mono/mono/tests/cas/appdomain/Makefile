RUNTIME = mono --debug --security
CSCOMPILE = mcs -debug
PROFILE = net_1_1

all:	makepol.exe internet.xml intranet.xml \
	sandbox.exe whoami.exe 

aot:	makepol.exe.so sandbox.exe.so whoami.exe.so

TEST_FILES = whoami.exe sandbox.exe "sandbox.exe whoami.exe"
TEST_POLICIES = fulltrust intranet internet execution nothing

run: all
	@for p in $(TEST_POLICIES); do	\
		for i in $(TEST_FILES); do	\
			echo -e "TESTING: $(RUNTIME) sandbox.exe $$p.xml $$i";	\
			$(RUNTIME) sandbox.exe $$p.xml $$i;	\
		done; \
	done;

test: run
# todo

clean:
	rm -f *.exe* *.dll* *.snk

%.xml: makepol.exe
	$(RUNTIME) makepol.exe

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

makepol.exe: makepol.cs
	$(CSCOMPILE) $^ /out:$@ -r:System.dll -r:System.Drawing.dll

%.exe.so: %.exe
	$(RUNTIME) --aot $^
