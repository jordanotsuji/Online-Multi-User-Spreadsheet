#ifndef SPREADSHEETLOGIC_ABSTRCT_SPREADSHEET_H
#define SPREADSHEETLOGIC_ABSTRCT_SPREADSHEET_H

#include <iostream>
#include <string>
#include <list>
#include <vector>
#include <unordered_set>

#include "Formula/Formula.h"

// IEnumberable becomes std::vector
// ISet becomes unordered set
// IList just becomes a list
// LinkedList became std::vector<std::string>

class AbstractSpreadsheet {

private:
	// Private member variables
    bool changed;
    std::string version;
    
    void Visit(std::string start, std::string name, std::unordered_set<std::string> visited, std::vector<std::string> changed);

protected:
    void setVersion(std::string version);
    void setChanged(bool changed);
	virtual std::vector<std::string> GetDirectDependents(std::string name) = 0;
    std::vector<std::string> GetCellsToRecalculate(std::unordered_set<std::string> names);
    std::vector<std::string> GetCellsToRecalculate(std::string name);

    virtual std::vector<std::string> SetCellContents(std::string name, double number) = 0;
    virtual std::vector<std::string> SetCellContents(std::string name, std::string text) = 0;
    virtual std::vector<std::string> SetCellContents(std::string name, Formula formula) = 0;

public:
	// Getters for private variables
    std::string getVersion();
    bool getChanged();
	
	// isValid and Normalize delegates
    bool(*isValid)(std::string);
	std::string(*normalize)(std::string);

	// The only constructor
    AbstractSpreadsheet(bool(*isValid)(std::string), std::string (*normalize)(std::string), std::string version);
    
    virtual std::string GetSavedVersion(std::string filename) = 0;
    virtual void Save(std::string filename) = 0;
    virtual Result GetCellValue(std::string name) = 0;
    virtual std::vector<std::string> GetNamesOfAllNonemptyCells() = 0;
    virtual Result GetCellContents(std::string name) = 0;
	virtual std::vector<std::string> SetContentsOfCell(std::string name, std::string content) = 0;
};


// TODO define the different exceptions
struct CircularException : public std::exception
{
    const char * what () const throw ()
    {
        return "Entry created a circular dependancy";
    }
};



#endif //SPREADSHEETLOGIC_ABSTRCT_SPREADSHEET_H
