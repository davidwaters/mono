thisdir = class/System.Json
SUBDIRS = 
include ../../build/rules.make

LIBRARY = System.Json.dll
LIB_MCS_FLAGS = \
	        /r:System.dll \
	        /r:System.Xml.dll \
	        /r:System.Core.dll \
		/r:System.ServiceModel.Web.dll

TEST_MCS_FLAGS = $(LIB_MCS_FLAGS)

EXTRA_DISTFILES =

ifneq (2.1, $(FRAMEWORK_VERSION))
LIBRARY_NAME = dummy-System.Json.dll
NO_INSTALL = yes
NO_SIGN_ASSEMBLY = yes
NO_TEST = yes
endif

include ../../build/library.make
