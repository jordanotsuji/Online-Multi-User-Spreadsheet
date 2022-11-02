CXX		:= g++
LINKER	:= g++

INCLUDE_DIRS = -I.
CXX_FLAGS    = -std=c++11 -ggdb # -Wall -Wextra
LDFLAGS		 = $(CXX_FLAGS) -flto -lsfml-network -lsfml-system -lstdc++fs -ltinyxml2 -lboost_regex -lpthread

BUILD_DIR	= build

.PHONY: clean

$(info $(shell mkdir -p $(BUILD_DIR)))

$(BUILD_DIR)/spreadsheet_tester: spreadsheet_tester.cpp
	$(LINKER) spreadsheet_tester.cpp $(LDFLAGS) $(INCLUDE_DIRS) -o $(BUILD_DIR)/spreadsheet_tester

clean:
	rm -rf $(BUILD_DIR)/*
