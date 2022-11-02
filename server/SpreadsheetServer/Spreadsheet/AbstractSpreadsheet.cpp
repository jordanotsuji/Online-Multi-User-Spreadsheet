#ifndef SPREADSHEETLOGIC_ABSTRCT_SPREADSHEET_CPP
#define SPREADSHEETLOGIC_ABSTRCT_SPREADSHEET_CPP

#include <iostream>
#include <string>

#include "AbstractSpreadsheet.h"

AbstractSpreadsheet::AbstractSpreadsheet(bool (*isValid)(std::string),
                                         std::string (*normalize)(std::string),
                                         std::string version) {
  this->isValid = isValid;
  this->normalize = normalize;
  this->version = version;
}

void AbstractSpreadsheet::setChanged(bool changed) { this->changed = changed; }

bool AbstractSpreadsheet::getChanged() { return this->changed; }

void AbstractSpreadsheet::setVersion(std::string version) {
  this->version = version;
}

std::string AbstractSpreadsheet::getVersion() { return this->version; }

std::vector<std::string> AbstractSpreadsheet::GetCellsToRecalculate(
    std::unordered_set<std::string> names) {
  std::vector<std::string> changed;
  std::unordered_set<std::string> visited;
  for (const auto &element : names) {
    std::unordered_set<std::string>::const_iterator got = visited.find(element);

    if (got == visited.end()) // Means that the token was not found
    {
      Visit(element, element, visited, changed);
    }
  }

  return changed;
}

std::vector<std::string>
AbstractSpreadsheet::GetCellsToRecalculate(std::string name) {
  std::unordered_set<std::string> toSend = {name};
  return GetCellsToRecalculate(toSend);
}

void AbstractSpreadsheet::Visit(std::string start, std::string name,
                                std::unordered_set<std::string> visited,
                                std::vector<std::string> changed) {
  visited.insert(name);
  for (const auto &element : GetDirectDependents(name)) {
    if (element == start) {
      throw CircularException();
    } else if (visited.find(element) == visited.end()) {
      Visit(start, element, visited, changed);
    }
  }

  // This prepends name to the vector
  std::vector<std::string>::iterator it;
  it = changed.begin();
  changed.insert(it, name);
}

#endif
