#ifndef SPREADSHEETLOGIC_DEPENDENCYGRAPH_H
#define SPREADSHEETLOGIC_DEPENDENCYGRAPH_H

#include <iostream>
#include <vector>
#include <string>
#include <stack>
#include <unordered_set>
#include <unordered_map>

class DependencyGraph {
private:
  std::unordered_map<std::string, std::unordered_set<std::string>>
      forwardDictionary;
  std::unordered_map<std::string, std::unordered_set<std::string>>
      backwardDictionary;
  int size;

public:
  DependencyGraph();
  int Size();
  const int operator[](const std::string s) const;
  bool HasDependents(std::string s) const;
  bool HasDependees(std::string s) const;
  std::vector<std::string> GetDependents(std::string s) const;
  std::vector<std::string> GetDependees(std::string s) const;
  void AddDependency(std::string s, std::string t);
  void RemoveDependency(std::string s, std::string t);
  void ReplaceDependents(std::string s, std::vector<std::string> newDependents);
  void ReplaceDependees(std::string s, std::vector<std::string> newDependees);
};

#endif // SPREADSHEETLOGIC_DEPENDENCYGRAPH_H
