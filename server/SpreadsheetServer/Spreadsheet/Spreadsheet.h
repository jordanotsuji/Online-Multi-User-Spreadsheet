#ifndef SPREADSHEET_H
#define SPREADSHEET_H

#include <iostream>
#include <vector>
#include <string>
#include <unordered_map>
#include <map>
#include <functional>
#include <tinyxml2.h>
#include <sstream>


#include "AbstractSpreadsheet.h"
#include "DependencyGraph/DependencyGraph.h"

class Cell
{
    public:
	    Cell(Result newContents, std::function<double(std::string)> lookup);
        void Recalculate(std::function<double(std::string)> lookup);
	    
        Result contents;
	    Result value;
};

// A struct that is used for the undo stack to keep track of the past states of a cell
struct cell_states
{
    std::string cell_name;
    Result cell_contents; // TODO: Need to find out how to handle a Result
    Result old_contents = Result();

    // True, if this particular state is from a revert operation
    bool from_revert;
};

class Spreadsheet : public AbstractSpreadsheet
{

private:
	bool internalChanged;
	DependencyGraph depGraph;

	std::unordered_map<std::string, Cell> cellMap;

    // Contains cell_states, which will have the cell name and contents that we can undo to
    std::stack<cell_states> undo_stack;

    // This map will take a cell name as the key and have a stack that corresponds to its history
    std::map<std::string, std::stack<Result>> revert_map; // TODO: Need to find out how to handle a Result

    //std::list<std::string> SetCellContents(std::string name, std::string text);
    std::vector<std::string> SetCellContents(std::string name, std::string text);
    std::vector<std::string> GetDirectDependents(std::string name);

    std::vector<std::string> SetCellContents(std::string name, double number);
    std::vector<std::string> SetCellContents(std::string name, Formula formula);

    std::vector<std::string> SystemSetAnyCellContents(std::string name, Result contents, bool pushToUndoStack, bool pushToRevertStack, bool is_from_revert);
	void RecalculateCells(std::vector<std::string> names);
    double SystemLookupForCells(std::string name);
    bool TryParse(std::string& s, double& d);

protected:

public:
	Spreadsheet();
	Spreadsheet(bool(*isValid)(std::string), std::string (*normalize)(std::string), std::string version);
	Spreadsheet(std::string filePath, bool(*isValid)(std::string), std::string (*normalize)(std::string), std::string version);

    std::string GetSavedVersion(std::string filename);
    
	bool getChanged();
	void setChanged(bool changed);
    bool CellNameValid(std::string cellName);
    
	void Save(std::string filename);
    std::vector<std::string> GetNamesOfAllNonemptyCells();
    Result GetCellValue(std::string name);
    Result GetCellContents(std::string name);
	//std::list<std::string> SetContentsOfCell(std::string name, std::string content);
    std::vector<std::string> SetContentsOfCell(std::string name, std::string content);

    // Maybe it should return the cell name and contents for clarification? undecided
    std::tuple<std::string, Result> undo();
    std::tuple<std::string, Result> revert(const std::string & name_of_cell);
    std::vector<cell_states> GetCellStatesFromUndoStack();
};


#endif // SPREADSHEET_H
