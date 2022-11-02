#include <string>
#include <unordered_set>
#include <unordered_map>
#include "DependencyGraph.h"

DependencyGraph::DependencyGraph() { size = 0; }

int DependencyGraph::Size() { return size; }

const int DependencyGraph::operator[](const std::string s) const {
  if (backwardDictionary.find(s) != backwardDictionary.end()) {
    return backwardDictionary.at(s).size();
  } else {
    return 0;
  }
}

bool DependencyGraph::HasDependents(std::string s) const {
  if (forwardDictionary.find(s) != forwardDictionary.end()) {
    return forwardDictionary.at(s).size() > 0;
  }

  return false;
}

bool DependencyGraph::HasDependees(std::string s) const {
  if (backwardDictionary.find(s) != backwardDictionary.end()) {
    return backwardDictionary.at(s).size() > 0;
  }
  return false;
}

std::vector<std::string> DependencyGraph::GetDependents(std::string s) const {
  std::vector<std::string> toReturn;
  if (forwardDictionary.find(s) != forwardDictionary.end()) {
    std::unordered_set<std::string> dependents = forwardDictionary.at(s);
    for (const std::string &s : dependents) {
      toReturn.push_back(s);
    }
  }

  return toReturn;
}

std::vector<std::string> DependencyGraph::GetDependees(std::string s) const {
  std::vector<std::string> toReturn;
  if (backwardDictionary.find(s) != backwardDictionary.end()) {
    std::unordered_set<std::string> dependents = backwardDictionary.at(s);
    for (const std::string &s : dependents) {
      toReturn.push_back(s);
    }
  }

  return toReturn;
}

void DependencyGraph::AddDependency(std::string s, std::string t) {
  if (forwardDictionary.find(s) == forwardDictionary.end()) {
    size++;

    std::unordered_set<std::string> dependents;
    dependents.insert(t);

    forwardDictionary[s] = dependents;
    if (backwardDictionary.find(t) == backwardDictionary.end()) {
      std::unordered_set<std::string> dependees;
      dependees.insert(s);
      backwardDictionary[t] = dependees;
    } else {
      backwardDictionary[t].insert(s);
    }
  } else if (forwardDictionary[s].find(t) == forwardDictionary[s].end()) {
    size++;
    forwardDictionary[s].insert(t);
    if (backwardDictionary.find(t) == backwardDictionary.end()) {
      std::unordered_set<std::string> dependees;
      dependees.insert(s);
      backwardDictionary[t] = dependees;
    } else {
      backwardDictionary[t].insert(s);
    }
  }
}

void DependencyGraph::RemoveDependency(std::string s, std::string t) {
  if (forwardDictionary.find(s) != forwardDictionary.end()) {
    if (forwardDictionary[s].find(t) != forwardDictionary[s].end()) {
      forwardDictionary[s].erase(t);
      backwardDictionary[t].erase(s);
      size--;
    }
  }
}

void DependencyGraph::ReplaceDependents(
    std::string s, std::vector<std::string> newDependents) {
  std::vector<std::string> toRemove = GetDependents(s);
  for (const std::string &r : toRemove) {
    RemoveDependency(s, r);
  }
  for (const std::string &t : newDependents) {
    AddDependency(s, t);
  }
}

void DependencyGraph::ReplaceDependees(std::string s,
                                       std::vector<std::string> newDependees) {
  if (backwardDictionary.find(s) != backwardDictionary.end()) {
    std::vector<std::string> toRemove = GetDependees(s);
    for (const std::string &t : toRemove) {
      RemoveDependency(t, s);
    }
  }

  for (const std::string &t : newDependees) {
    AddDependency(t, s);
  }
}
