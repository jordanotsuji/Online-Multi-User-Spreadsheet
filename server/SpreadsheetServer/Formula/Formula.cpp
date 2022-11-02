#include <string>
#include <unordered_set>
#include <boost/regex.hpp>
#include <iostream>
#include "Formula.h"
#include <boost/tokenizer.hpp>

/*
 * Default constructor, puts a 1 into the value spot,
 * normalize doesn't modify anything, and isValid always returns true
 */
Formula::Formula()
    : Formula(
          "", [](std::string s) { return s; },
          [](std::string s) { return true; }) {}
/*
 * Formula constructor, accepts a formula as a string
 * and uses default normalize and isValid
 */
Formula::Formula(const std::string &formula)
    : Formula(
          formula, [](std::string s) { return s; },
          [](std::string s) { return true; }) {}

Formula &Formula::operator=(const Formula &rhs) {
  this->expression = std::vector<std::string>(rhs.expression);
  this->variables = std::unordered_set<std::string>(rhs.variables);

  return *this;
}

/*
 * Full Formula constructor
 */
Formula::Formula(const std::string &formula,
                 std::string (*normalize)(std::string),
                 bool (*isValid)(std::string)) {
  std::string normalizedFormula = normalize(formula);
  /*  if(formula == NULL)
    {
        throw "provided formula is null";
    }*/

  if (normalizedFormula == "") {
    return;
  }

  expression = GetTokens(normalizedFormula);

  int leftParen = 0;
  int rightParen = 0;

  if (expression.size() == 0) {
    throw std::domain_error("at least one too many operators");
  }

  bool mustBeOperator = false;
  int openParenCount = 0;
  int closeParenCount = 0;

  // For loop to check if the formatting of the formula is correct
  for (int i = 0; i < expression.size(); i++) {
    std::string s = expression[i];
    double d;

    if (s == "+" || s == "-" || s == "*" || s == "/") {
      if (!mustBeOperator) {
        std::cout << "at least one too many operators" << std::endl;
        throw std::domain_error("at least one too many operators");
      }
      mustBeOperator = false;
    }

    else if (s == "(") {
      if (mustBeOperator) {
        std::cout << "There is a ( symbol right after a variable or number"
                  << std::endl;
        throw std::domain_error(
            "There is a ( symbol right after a variable or number");
      }
      openParenCount++;
    }

    else if (s == ")") {
      if (!mustBeOperator) {
        std::cout << "There is a ) symbol following an operator" << std::endl;
        throw std::domain_error("There is a ) symbol following an operator");
      }
      closeParenCount++;

      if (closeParenCount > openParenCount) {
        std::cout << "There are two values not separated by an operator"
                  << std::endl;
        throw std::domain_error("There are too many ) symbols");
      }
    }

    else if (TryParse(s, d)) {
      if (mustBeOperator) {
        std::cout << "There are two values not separated by an operator, try "
                     "adding another operator"
                  << std::endl;
        throw std::domain_error("");
      }
      mustBeOperator = true;
    }

    else {
      if (mustBeOperator) {
        std::cout << "There are two values not separated by an operator"
                  << std::endl;
        throw std::domain_error(
            "There are two values not separated by an operator");
      }
      if (!isValid(normalize(s)) || !isValid(s)) {
        std::cout << "The validator did not identify a token as valid"
                  << std::endl;
        throw std::domain_error(
            "The validator did not identify a token as valid");
      }
      if (!IsValidVariable(normalize(s))) {
        std::cout
            << "At least one token is not a valid token after normalization"
            << std::endl;
        throw std::domain_error(
            "At least one token is not a valid token after normalization");
      }
      mustBeOperator = true;
    }
  }

  if (!mustBeOperator) {
    std::cout << "The final token of the expression cannot be an operator"
              << std::endl;
    throw std::domain_error(
        "The final token of the expression cannot be an operator");
  }
  if (openParenCount != closeParenCount) {
    std::cout << "The number of ( and ) symbols don't match" << std::endl;
    throw std::domain_error("The number of ( and ) symbols don't match");
  }
}

Result Formula::Evaluate(std::function<double(std::string)> lookup) {
  double num = 0;
  std::stack<double> values;
  std::stack<char> oper;
  Result returnResult;

  for (int i = 0; i < expression.size(); i++) {
    std::string s = expression[i];
    bool usingVar = false;

    if (s.length() >= 2 && TryParse(s, num)) {
      try {
        num = lookup(s);
      } catch (const std::exception &e) {
        returnResult.type = 3;
        returnResult.reason =
            "The variabble " + s + " is undefined with the given lookup method";
        return returnResult;
      }
      usingVar = true;
    }

    if (usingVar || TryParse(s, num)) {
      if (oper.top() == *"*" || oper.top() == *"/") {
        values.push(num);
        if (!Compute(oper, values, num)) {
          returnResult.type = 3;
          returnResult.reason = "Divide by 0 error";
          return returnResult;
        }
        values.push(num);
      } else {
        values.push(num);
      }
    }

    else if (s == "+" || s == "-") {
      if (oper.top() == *"+" || oper.top() == *"-") {
        Compute(oper, values, num);
        values.push(num);
      }
      oper.push(s[0]);
    }

    else if (s == "*" || s == "/" || s == "(") {
      oper.push(s[0]);
    }

    else if (s == ")") {
      if (oper.top() == *"-" || oper.top() == *"+") {
        Compute(oper, values, num);
        values.push(num);
      }

      if (oper.top() == *"(") {
        oper.pop();
      }

      if (oper.top() == *"*" || oper.top() == *"/") {
        if (!Compute(oper, values, num)) {
          returnResult.type = 3;
          returnResult.reason = "Divide by 0 error";
          return returnResult;
        }
        values.push(num);
      }
    }
  }

  if (oper.size() == 0) {
    returnResult.type = 2;
    returnResult.number = values.top();
    values.pop();
    return returnResult;
  } else {
    Compute(oper, values, num);
    returnResult.type = 2;
    returnResult.number = num;
    return returnResult;
  }
}

std::vector<std::string> Formula::GetVariables() {
  std::vector<std::string> vars;
  for (int i = 0; i < expression.size(); i++) {
    std::string token = expression[0];
    if (token[0] == *"_" || isalpha(token[0])) {
      vars.push_back(token);
    }
  }

  return vars;
}

std::string Formula::ToString() {
  std::stringstream toReturn;
  for (auto &token : expression) {
    toReturn << GetEqualString(token);
  }
  return toReturn.str();
}

std::string Formula::GetEqualString(const std::string &s) {
  try {
    double d;
    d = std::stod(s);
    return std::to_string(d);
  } catch (const std::exception &e) {
    return s;
  }
}

bool Formula::Equals(Result r) {
  // TODO: Check *r.formula
  if (r.type == 3 || (r.type == 1 && &r.formula == nullptr)) {
    return false;
  }

  std::vector<std::string> rhs = GetTokens(r.formula.ToString());

  if (rhs.size() != expression.size()) {
    return false;
  }
  for (int i = 0; i < expression.size(); i++) {
    if (GetEqualString(rhs[i]) != GetEqualString(expression[i])) {
      return false;
    }
  }

  return true;
}

const bool Formula::operator==(const Formula &rhs) {
  Result r;
  r.type = 1;
  r.formula = rhs;
  return this->Equals(r);
}

const bool Formula::operator!=(const Formula &rhs) {
  Result r;
  r.type = 1;
  r.formula = rhs;
  return !this->Equals(r);
}

int Formula::GetHashCode() {
  // Is this needed?
  return 0;
}

bool Formula::IsValidVariable(const std::string var) {
  /*
  if(var[0] == '_' || isalpha(var[0]))
  {
      for(char c : var)
      {
          if(c != '_' && isalnum(c))
          {
              return false;
          }
      }
  }
  else
  {
      return false;
  }
  */

  boost::regex rgx(R"(^[A-Za-z]+[0-9]+$)");

  return boost::regex_match(var, rgx);
}

std::vector<std::string> Formula::GetTokens(const std::string &f) {
  std::string formula = f;
  std::vector<std::string> tokens;

  formula = boost::regex_replace(formula, boost::regex("\\s+"), "");

  // Regex piece of shit. Don't question it.
  boost::regex re(
      R"(([a-zA-Z_](?:[a-zA-Z_]|\d)*)|((?:\d+\.\d*|\d*\.\d+|\d+)(?:[eE][\+-]?\d+)?)|([\+\-*/])|(\()|((\))))");

  boost::sregex_token_iterator tokenIterator(formula.begin(), formula.end(), re,
                                             0);
  boost::sregex_token_iterator end;

  while (tokenIterator != end) {
    std::string token(tokenIterator->first, tokenIterator->second);
    tokens.push_back(token);
    tokenIterator++;
  }

  return tokens;
}

bool Formula::Compute(std::stack<char> &oper, std::stack<double> &values,
                      double &num) {
  char op = oper.top();
  oper.pop();
  double val1 = values.top();
  values.pop();
  double val2 = values.top();
  values.pop();

  if (op == '*') {
    val1 = val1 * val2;
  } else if (op == '/') {
    if (val1 == 0) {
      num = 0;
      return false;
    }
    val1 = val2 / val1;
  } else if (op = '+') {
    val1 = val1 + val2;
  } else if (op = '-') {
    val1 = val2 - val1;
  }

  num = val1;
  return true;
}

bool Formula::TryParse(const std::string &s, double &num) {
  try {
    double d = std::stod(s);
    num = d;
    return true;
  } catch (const std::exception &e) {
    return false;
  }
}

