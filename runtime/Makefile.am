# hack to prevent 'check' from depending on 'all'
AUTOMAKE_OPTIONS = cygnus

tmpinst = _tmpinst

noinst_SCRIPTS = mono-wrapper monodis-wrapper

etctmp = etc
symlinks = etc/mono/1.0/machine.config etc/mono/2.0/machine.config etc/mono/2.0/web.config etc/mono/browscap.ini etc/mono/2.0/Browsers/Compat.browser

if INSTALL_4_0
symlinks += etc/mono/4.0/machine.config etc/mono/4.0/web.config
endif

etc/mono/2.0/machine.config: $(top_srcdir)/data/net_2_0/machine.config
etc/mono/2.0/web.config: $(top_srcdir)/data/net_2_0/web.config
etc/mono/browscap.ini: $(top_srcdir)/data/browscap.ini
etc/mono/2.0/Browsers/Compat.browser: $(top_srcdir)/data/net_2_0/Browsers/Compat.browser
etc/mono/4.0/machine.config: $(top_srcdir)/data/net_4_0/machine.config
etc/mono/4.0/web.config: $(top_srcdir)/data/net_4_0/web.config

$(symlinks):
	cd $(top_builddir) && $(SHELL) ./config.status $(subdir)/$@

SUPPORT_FILES = $(symlinks) mono-wrapper etc/mono/config

if ONLY_MOONLIGHT
build_profiles = moonlight_raw
test_profiles = moonlight_raw
else

build_profiles = net_2_0 net_3_5
test_profiles = $(build_profiles)

if MOONLIGHT
build_profiles += moonlight_raw
test_profiles += moonlight_raw

moon-do-build: $(SUPPORT_FILES)
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='moonlight_raw' CC='$(CC)' all-profiles

moon-do-clean:
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='moonlight_raw' CC='$(CC)' clean-profiles

endif

if INSTALL_4_0
build_profiles += net_4_0
#test_profiles += net_4_0
endif

if INSTALL_MONODROID
build_profiles += monodroid
endif

if INSTALL_MONOTOUCH
build_profiles += monotouch
endif

endif

if BUILD_MCS

# The write check is to foil 'make distcheck'
all-local: $(SUPPORT_FILES) $(TEST_SUPPORT_FILES)
	if test -w $(mcs_topdir); then :; else chmod -R +w $(mcs_topdir); fi
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(build_profiles)' CC='$(CC)' all-profiles

# override automake
install: install-exec install-data

# override automake
install-exec: $(SUPPORT_FILES) $(TEST_SUPPORT_FILES)
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(build_profiles)' RUNTIME_HAS_CONSISTENT_GACDIR=yes prefix=$(prefix) install-profiles

# override automake
install-data:
	@:

# override automake
uninstall:
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(build_profiles)' RUNTIME_HAS_CONSISTENT_GACDIR=yes prefix=$(prefix) uninstall-profiles

## mono --wapi=semdel will probably not delete the semaphore if someone is crazy enough to do a 'make -j distclean' :-)
clean-local:
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(build_profiles)' clean-profiles
	-./mono-wrapper --wapi=semdel
	-rm -fr $(etctmp) $(tmpinst) .wapi

endif BUILD_MCS

TEST_SUPPORT_FILES = $(tmpinst)/bin/mono $(tmpinst)/bin/ilasm $(tmpinst)/bin/gmcs $(tmpinst)/bin/dmcs $(tmpinst)/bin/al2

# now a misnomer, but it'll go away soon enough.
if ENABLE_NUNIT_TESTS
test_select =
else
test_select = ONLY_CENTUM_TESTS=yes
endif

mcs-do-test-profiles:
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(test_profiles)' test-profiles

mcs-do-run-test-profiles: test-support-files
	cd $(mcs_topdir) && $(MAKE) NO_DIR_CHECK=1 PROFILES='$(test_profiles)' run-test-profiles

if HOST_WIN32
if CROSS_COMPILING
cur_dir_cmd = pwd
PLATFORM_PATH_SEPARATOR = :
else
cur_dir_cmd = cygpath -w -a .
PLATFORM_PATH_SEPARATOR = ;
endif
else
cur_dir_cmd = pwd
PLATFORM_PATH_SEPARATOR = :
endif

# Compile all assemblies with the verifier turned on. Code must be valid but not verifiable.
# TODO it would be nice to split assemblies without unsafe code to use the verifier with verifiable mode.
# Skip net 2.1 assemblies for now because of visibility problems
mcs-compileall: mono-wrapper etc/mono/config
	save_MONO_PATH=$$MONO_PATH; mcs_topdir=`cd $(mcs_topdir) && $(cur_dir_cmd)`; ok=:; \
	for profile in $(test_profiles); do \
	  if [ "moonlight" = "$$profile" ]; then \
          break; \
      fi; \
	  if [ "net_3_5" = "$$profile" ]; then \
		  MONO_PATH="$$mcs_topdir/class/lib/$$profile$(PLATFORM_PATH_SEPARATOR)$$mcs_topdir/class/lib/net_2_0$(PLATFORM_PATH_SEPARATOR)$$save_MONO_PATH"; \
	  else \
		  MONO_PATH="$$mcs_topdir/class/lib/$$profile$(PLATFORM_PATH_SEPARATOR)$$save_MONO_PATH"; \
	  fi; \
	  export MONO_PATH; \
	  for i in $(mcs_topdir)/class/lib/$$profile/*.{dll,exe}; do \
		if [ ! -f $$i ] ; then \
			continue ; \
		fi ;  \
	    if ./mono-wrapper --compile-all --verify-all --security=validil $$i; then \
	      echo $$i verified OK; \
	    else \
	      echo $$i verification failed; ok=false; \
	    fi; done; done; \
	$$ok

check-local: mcs-compileall mcs-do-test-profiles
	$(MAKE) $(test_select) mcs-do-run-test-profiles

CLEANFILES = etc/mono/config

# depend on $(symlinks) to ensure 'etc/mono' directory exists
etc/mono/config: ../data/config Makefile $(symlinks)
	d=`cd ../support && pwd`; \
	sed 's,target="libMonoPosixHelper[^"]*",target="'$$d/libMonoPosixHelper.la'",' ../data/config > $@t
	if test -z "$(libgdiplus_loc)"; then :; else \
	  sed 's,<configuration>,& <dllmap dll="gdiplus.dll" target="$(libgdiplus_loc)" />,' $@t > $@tt; \
	  mv -f $@tt $@t; fi
	mv -f $@t $@

$(tmpinst)/bin/mono: mono-wrapper etc/mono/config
	$(mkinstalldirs) $(tmpinst)/bin
	cp mono-wrapper $@

$(tmpinst)/bin/gmcs: $(tmpinst)/bin/mono Makefile
	echo '#! /bin/sh' > $@ ; \
	r=`pwd`; m=`cd $(mcs_topdir) && pwd`; \
	echo 'exec "'"$$r/$(tmpinst)/bin/mono"'" "'"$$m/class/lib/net_2_0/gmcs.exe"'" "$$@"' >> $@ ; \
	chmod +x $@

$(tmpinst)/bin/dmcs: $(tmpinst)/bin/mono Makefile
	echo '#! /bin/sh' > $@ ; \
	r=`pwd`; m=`cd $(mcs_topdir) && pwd`; \
	echo 'exec "'"$$r/$(tmpinst)/bin/mono"'" "'"$$m/class/lib/net_4_0/dmcs.exe"'" "$$@"' >> $@ ; \
	chmod +x $@

$(tmpinst)/bin/ilasm: $(tmpinst)/bin/mono Makefile
	echo '#! /bin/sh' > $@ ; \
	r=`pwd`; m=`cd $(mcs_topdir) && pwd`; \
	echo 'exec "'"$$r/$(tmpinst)/bin/mono"'" "'"$$m/ilasm/ilasm.exe"'" "$$@"' >> $@ ; \
	chmod +x $@

$(tmpinst)/bin/al2: $(tmpinst)/bin/mono Makefile
	echo '#! /bin/sh' > $@ ; \
	r=`pwd`; m=`cd $(mcs_topdir) && pwd`; \
	echo 'exec "'"$$r/$(tmpinst)/bin/mono"'" "'"$$m/class/lib/net_2_0/al.exe"'" "$$@"' >> $@ ; \
	chmod +x $@

test-support-files: $(TEST_SUPPORT_FILES)
	@:

# the 'cygnus' option also disables the default 'distdir:' target, which we _do_ want
MYDISTFILES = $(DIST_COMMON)
distdir: $(MYDISTFILES)
	rm -fr $(distdir)
	mkdir $(distdir)
	test -z '$(MYDISTFILES)' || for file in ''$(MYDISTFILES); do \
	  cp -p $$file $(distdir) ; done
	find $(distdir) -type f -exec chmod a+r {} ';'
