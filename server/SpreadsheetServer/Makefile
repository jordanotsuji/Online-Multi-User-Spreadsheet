CXX		:= g++
LINKER	:= g++

INCLUDE_DIRS = -I.
CXX_FLAGS    = -std=c++11 -ggdb # -Wall -Wextra
LDFLAGS		 = $(CXX_FLAGS) -flto -lsfml-network -lsfml-system -lstdc++fs -ltinyxml2 -lboost_regex -lpthread

BUILD_DIR	= build

SOURCES = $(wildcard *.cpp) $(wildcard */*.cpp) #TODO Figure out how to make use of this to shorten the Makefile

OBJECTS 	:= MessageHandler.o Spreadsheet.o AbstractSpreadsheet.o Formula.o DependencyGraph.o

.PHONY: all clean

$(info $(shell mkdir -p $(BUILD_DIR)))

$(BUILD_DIR)/server: main.cpp $(OBJECTS:%=$(BUILD_DIR)/%)
	$(LINKER) main.cpp $(OBJECTS:%=$(BUILD_DIR)/%) $(LDFLAGS) $(INCLUDE_DIRS) -o $(BUILD_DIR)/server

$(BUILD_DIR)/MessageHandler.o: MessageHandler/MessageHandler.cpp MessageHandler/MessageHandler.h
	$(CXX) -c MessageHandler/MessageHandler.cpp $(CXX_FLAGS) $(INCLUDE_DIRS) -o $(BUILD_DIR)/MessageHandler.o

$(BUILD_DIR)/Spreadsheet.o: Spreadsheet/Spreadsheet.cpp Spreadsheet/Spreadsheet.h Spreadsheet/AbstractSpreadsheet.h Formula/Formula.h
	$(CXX) -c Spreadsheet/Spreadsheet.cpp $(CXX_FLAGS) $(INCLUDE_DIRS) -o $(BUILD_DIR)/Spreadsheet.o

$(BUILD_DIR)/AbstractSpreadsheet.o: Spreadsheet/AbstractSpreadsheet.cpp Spreadsheet/AbstractSpreadsheet.h Formula/Formula.h 
	$(CXX) -c Spreadsheet/AbstractSpreadsheet.cpp $(CXX_FLAGS) $(INCLUDE_DIRS) -o $(BUILD_DIR)/AbstractSpreadsheet.o

$(BUILD_DIR)/Formula.o: Formula/Formula.cpp Formula/Formula.h DependencyGraph/DependencyGraph.h
	$(CXX) -c Formula/Formula.cpp $(CXX_FLAGS) $(INCLUDE_DIRS) -o $(BUILD_DIR)/Formula.o

$(BUILD_DIR)/DependencyGraph.o: DependencyGraph/DependencyGraph.cpp DependencyGraph/DependencyGraph.h
	$(CXX) -c DependencyGraph/DependencyGraph.cpp $(CXX_FLAGS) $(INCLUDE_DIRS) -o $(BUILD_DIR)/DependencyGraph.o

clean:
	rm -rf $(BUILD_DIR)/*
