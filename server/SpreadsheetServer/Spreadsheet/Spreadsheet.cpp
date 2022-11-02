#ifndef SPREADSHEETLOGIC_SPREADSHEET_CPP
#define SPREADSHEETLOGIC_SPREADSHEET_CPP

#include <stdio.h>
#include <regex>

#include "AbstractSpreadsheet.h"
#include "Spreadsheet.h"

Spreadsheet::Spreadsheet()
    : AbstractSpreadsheet(
          [](std::string s) { return true; },
          [](std::string s) {
            std::transform(s.begin(), s.end(), s.begin(),
                           [](unsigned char c) { return std::toupper(c); });
            return s;
          },
          "version") {
  internalChanged = false;
}

Spreadsheet::Spreadsheet(bool (*isValid)(std::string),
                         std::string (*normalize)(std::string),
                         std::string version)
    : AbstractSpreadsheet(isValid, normalize, version) {
  internalChanged = false;
}

Spreadsheet::Spreadsheet(std::string filePath, bool (*isValid)(std::string),
                         std::string (*normalize)(std::string),
                         std::string version)
    : AbstractSpreadsheet(isValid, normalize, version)

{
  internalChanged = false;

  tinyxml2::XMLDocument doc;
  doc.LoadFile(filePath.c_str());

  tinyxml2::XMLNode *spreadsheet = doc.FirstChildElement("spreadsheet");

  if (spreadsheet == nullptr) {
    std::cerr << "spreadsheet is null\n";
    return;
  }

  tinyxml2::XMLNode *cellState = spreadsheet->FirstChildElement("cellState");

  while (cellState != nullptr) {
    std::string name = cellState->FirstChildElement("name")->GetText();
    std::string contents = "";
    if (cellState->FirstChildElement("result")->GetText() != nullptr)
      contents = cellState->FirstChildElement("result")->GetText();

    SetContentsOfCell(name, contents);

    bool fromRevert;
    cellState->FirstChildElement("fromRevert")->QueryBoolText(&fromRevert);

    if (fromRevert) {
      int type;
      Result r;
      cellState->FirstChildElement("result")->QueryIntAttribute("type", &type);
      r.type = type;
      if (r.type == 1) {
        Formula f(contents);
        r.formula = f;
      } else if (r.type == 2) {
        double number;
        TryParse(contents, number);
        r.number = number;
      } else {
        r.reason = contents;
      }

      revert_map[name].push(r);
    }

    cellState = cellState->NextSiblingElement("cellState");
  }
}

std::string Spreadsheet::GetSavedVersion(std::string filename) { return ""; }

void Spreadsheet::Save(std::string filename) {
  std::string filePath = "/spreadsheets/" + filename;
  tinyxml2::XMLDocument doc;

  tinyxml2::XMLNode *spreadsheet = doc.NewElement("spreadsheet");
  doc.InsertFirstChild(spreadsheet);

  std::stack<cell_states> temp(undo_stack);
  std::stack<cell_states> reversed;

  while (!temp.empty()) {
    reversed.push(temp.top());
    temp.pop();
  }

  while (!reversed.empty()) {
    tinyxml2::XMLElement *cellState = doc.NewElement("cellState");
    cell_states cs = reversed.top();
    reversed.pop();

    tinyxml2::XMLElement *name = doc.NewElement("name");
    tinyxml2::XMLElement *result = doc.NewElement("result");
    tinyxml2::XMLElement *fromRevert = doc.NewElement("fromRevert");

    name->SetText(cs.cell_name.c_str());
    result->SetAttribute("type", cs.cell_contents.type);

    result->SetText(cs.cell_contents.ToString().c_str());

    fromRevert->SetText(cs.from_revert);

    cellState->InsertEndChild(name);
    cellState->InsertEndChild(result);
    cellState->InsertEndChild(fromRevert);

    spreadsheet->InsertEndChild(cellState);
  }
  tinyxml2::XMLError eResult = doc.SaveFile(filePath.c_str());
}

std::vector<std::string> Spreadsheet::GetNamesOfAllNonemptyCells() {
  // Rather than using yield, if we want to use this method, we should first
  // call it, store the values in a list, and then iterate through those values
  std::vector<std::string> keys;

  for (std::unordered_map<std::string, Cell>::iterator iterator =
           cellMap.begin();
       iterator != cellMap.end(); ++iterator) {
    keys.push_back(iterator->first); // Add the key
  }

  return keys;
}

std::vector<std::string> Spreadsheet::SetCellContents(std::string name,
                                                      double number) {
  Result r;
  r.type = 2;
  r.number = number;
  return SystemSetAnyCellContents(name, std::move(r), true, true, false);
}

std::vector<std::string> Spreadsheet::SetCellContents(std::string name,
                                                      std::string text) {
  Result r;
  r.type = 3;
  r.reason = text;
  return SystemSetAnyCellContents(name, std::move(r), true, true, false);
}

std::vector<std::string> Spreadsheet::SetCellContents(std::string name,
                                                      Formula formula) {
  Result r;
  r.type = 1;
  r.formula = formula;
  return SystemSetAnyCellContents(name, std::move(r), true, true, false);
}

std::vector<std::string> Spreadsheet::SystemSetAnyCellContents(
    std::string name, Result contents, bool pushToUndoStack,
    bool pushToRevertStack, bool is_from_revert) {
  if (!CellNameValid(name)) {
    throw "Cell name invalid";
  }

  Result oldContents;

  std::vector<std::string> oldDependents;

  if (cellMap.count(name) > 0) {
    oldContents = cellMap.at(name).contents;

    if (contents.type == 1) {
      oldDependents = depGraph.GetDependents(name);
    }
    std::function<double(std::string)> f = std::bind(
        &Spreadsheet::SystemLookupForCells, this, std::placeholders::_1);
    Cell c(contents, f);
    cellMap.at(name) = c;
  } else {
    std::function<double(std::string)> f = std::bind(
        &Spreadsheet::SystemLookupForCells, this, std::placeholders::_1);
    Cell c(contents, f);
    std::pair<std::string, Cell> pair(name, c);
    cellMap.insert(pair);
  }

  /*
      if(contents.type == 3 && contents.ToString() == "")
      {
              cellMap.erase(name);
      }
  */ //Removed because while optimizes non server spreadhseet, breaks undo here.
  if (contents.type == 1) {
    depGraph.ReplaceDependents(name, contents.formula.GetVariables());
  } else {
    std::vector<std::string> v;
    depGraph.ReplaceDependents(name, v);
  }

  try {
    if (cellMap.count(name)) {
      RecalculateCells(GetCellsToRecalculate(name));
    }

    cell_states cs;
    cs.cell_name = name;
    cs.cell_contents = contents;
    cs.old_contents = oldContents;
    cs.from_revert = is_from_revert;

    if (pushToUndoStack) {
      undo_stack.push(cs);
    }
    if (pushToRevertStack) {
      // If the cell is not in the revert map, then add it in
      if (revert_map.find(name) == revert_map.end()) {
        std::stack<Result> resultsStack;
        revert_map.emplace(name, resultsStack);

        // Push the old contents of this cell onto the revert stack
        revert_map[name].push(oldContents);
      }
      // We assume that the cell already
      // exits in the revert map
      else {
        revert_map[name].push(oldContents);
      }
    }

    return GetCellsToRecalculate(name);
  } catch (const std::exception &e) {
    std::function<double(std::string)> f = std::bind(
        &Spreadsheet::SystemLookupForCells, this, std::placeholders::_1);
    Cell c(oldContents, f);
    cellMap.at(name) = c;

    if (oldContents.type == 1) {
      depGraph.ReplaceDependents(name, oldContents.formula.GetVariables());
    } else {
      std::vector<std::string> v;
      depGraph.ReplaceDependents(name, v);
    }
    throw CircularException();
  }
}

// The vector contains the most recent edit at the end, and the first edit at
// the beginning
std::vector<cell_states> Spreadsheet::GetCellStatesFromUndoStack() {
  std::vector<cell_states> states;
  std::stack<cell_states> temp(undo_stack);
  std::stack<cell_states> reversed;

  while (!temp.empty()) {
    reversed.push(temp.top());
    temp.pop();
  }

  while (!reversed.empty()) {
    states.push_back(reversed.top());
    reversed.pop();
  }

  return states;
}

std::vector<std::string> Spreadsheet::GetDirectDependents(std::string name) {
  return depGraph.GetDependees(name);
}

Result Spreadsheet::GetCellValue(std::string name) {
  // changed from if(!isValid(name))
  if (!CellNameValid(name)) {
    throw "Name invalid via isValid delegate";
  }

  name = normalize(name);

  if (cellMap.count(name)) {
    return cellMap.at(name).value;
  }

  Result blank;
  return blank;
}

Result Spreadsheet::GetCellContents(std::string name) {
  if (!CellNameValid(name) /* || name == NULL*/) {
    throw "Invalid cell name";
  }

  name = normalize(name);

  if (cellMap.count(
          name)) // basically contains - a set can only have one of any key
  {
    return cellMap.at(name).contents;
  } else {
    return Result();
  }
}

bool Spreadsheet::CellNameValid(std::string cellName) {
  return std::regex_match(cellName, std::regex("^[a-zA-Z]+[0-9]+$"));
}

void Spreadsheet::RecalculateCells(std::vector<std::string> names) {
  for (int i = 0; i < names.size(); i++) {
    std::function<double(std::string)> f = std::bind(
        &Spreadsheet::SystemLookupForCells, this, std::placeholders::_1);
    cellMap.at(names[i]).Recalculate(f);
  }
}

double Spreadsheet::SystemLookupForCells(std::string name) {
  if (cellMap.count(name) && cellMap.at(name).value.type == 2) {
    return cellMap.at(name).value.number;
  }

  throw "This cell is not a number";

  return 0;
}

std::vector<std::string> Spreadsheet::SetContentsOfCell(std::string name,
                                                        std::string content) {
  internalChanged = true;

  double num = 0;

  Result contentResult;
  if (content != "" && content[0] == '=') {
    if (content.length() == 1)
      throw std::domain_error("invalid formula, formula must conatin values");
    Formula f(content.substr(1, content.length() - 1), normalize, isValid);
    contentResult.type = 1;
    contentResult.formula = f;

  } else if (TryParse(content, num)) {
    contentResult.type = 2;
    contentResult.number = num;
  } else {
    contentResult.type = 3;
    contentResult.reason = content;
  }

  return SystemSetAnyCellContents(normalize(name), contentResult, true, true,
                                  false);
}

bool Spreadsheet::TryParse(std::string &s, double &num) {
  try {
    double d = std::stod(s);
    num = d;
    return true;
  } catch (const std::exception &e) {
    return false;
  }
}

bool Spreadsheet::getChanged() { return internalChanged; }

void Spreadsheet::setChanged(bool changed) { internalChanged = changed; }

Cell::Cell(Result newContents, std::function<double(std::string)> lookup) {
  contents = newContents;
  switch (contents.type) {
  case 1:
    // value = contents.formula.Evaluate(name, lookup);
    break;
  case 2:
    value.type = 2;
    value.number = contents.number;
    break;
  case 3:
    value.type = 3;
    value.reason = contents.reason;
    break;
  }
}

void Cell::Recalculate(std::function<double(std::string)> lookup) {
  switch (contents.type) {
  case 1:
    // value = contents.formula.Evaluate(name, lookup);
    break;
  case 2:
    value.type = 2;
    value.number = contents.number;
    break;
  case 3:
    value.type = 3;
    value.reason = contents.reason;
    break;
  }
}

std::tuple<std::string, Result> Spreadsheet::undo() {
  if (!undo_stack.empty()) {
    // Get the latest change
    cell_states undo_to = undo_stack.top();

    // Save the current content of the cell that will be affected
    Result current_contents = cellMap.at(undo_to.cell_name).contents;

    // Apply the undo to the appropriate cell
    SystemSetAnyCellContents(undo_to.cell_name, undo_to.old_contents, false,
                             false, false);

    // If the undo is on a revert, then we can push the current contents onto
    // the revert stack
    if (undo_to.from_revert) {
      revert_map[undo_to.cell_name].push(current_contents);
    } else {
      // Assuming the undo action is not undoing a revert, we pop from its
      // revert stack
      if (!revert_map[undo_to.cell_name].empty())
        revert_map[undo_to.cell_name].pop();
    }

    // Pop the stack after the operation
    undo_stack.pop();

    return std::make_tuple(undo_to.cell_name, undo_to.old_contents);
  }

  Result fake;

  // Return a tuple with an empty cell name and a fake result (since we will not
  // be using it)
  return std::make_tuple("", fake);
}

std::tuple<std::string, Result>
Spreadsheet::revert(const std::string &name_of_cell) {
  if (revert_map.find(name_of_cell) != revert_map.end() &&
      !revert_map[name_of_cell].empty()) {
    // Grab the contents that is currently in the cell so we can put it on the
    // undo stack
    Result current_contents = cellMap.at(name_of_cell).contents;

    // Grab the last change for the particular cell
    Result revert_contents = revert_map[name_of_cell].top();

    try {

      // Revert the cell to the previous contents
      SystemSetAnyCellContents(name_of_cell, revert_contents, true, false,
                               true);
    }
    // Catch circular exceptions and return a revert error
    catch (const std::exception &e) {
      Result r;
      r.type = 4;
      r.reason = " this revert would cause a circular dependency.";
      return std::make_tuple("", r);
    }

    revert_map[name_of_cell].pop();

    // undo_stack.push(current_state);

    return std::make_tuple(name_of_cell, revert_contents);
  }
  // else if(cellMap.count(name_of_cell))
  //  {
  // If no operation happens, return a tuple with an empty cell name and the
  // current_contents of the cell
  //	    return std::make_tuple("", cellMap.at(name_of_cell).contents);
  // }
  else {
    Result r;
    r.type = 4;
    r.reason = " the revert stack is empty for this cell.";
    return std::make_tuple("", r);
  }
}

#endif
