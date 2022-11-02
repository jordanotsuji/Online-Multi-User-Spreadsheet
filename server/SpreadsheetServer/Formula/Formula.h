//
// Created by Jordan Otsuji on 4/7/2021.
//

#ifndef SPREADSHEETLOGIC_FORMULA_H
#define SPREADSHEETLOGIC_FORMULA_H

#include <iostream>
#include <vector>
#include <string>
#include <stack>
#include <unordered_set>
#include <functional>


struct Result;

class Formula {

private:
    std::vector<std::string> expression;
    std::unordered_set<std::string> variables;
    static std::vector<std::string> GetTokens(const std::string & formula);
    bool NextTokenInvalid(int i);
    static void ComputeTopOfStack(std::stack<double>& valueStack, std::stack<char>& operatorStack,
        int switchInt, double currentDouble);
    static bool IsValidVariable(const std::string var);
    bool Compute(std::stack<char>& oper, std::stack<double>& values, double& num);
    std::string GetEqualString(const std::string& s);
    bool TryParse(const std::string& s, double& num);

public:
    Formula();
    Formula(const std::string& formula);
    Formula(const std::string& formula, std::string (*normalize)(std::string), bool(*isValid)(std::string));

    // TODO: if stcut is correct here
    Result Evaluate(std::function<double(std::string)> lookup);
    std::vector<std::string> GetVariables();
    std::string ToString();
    bool Equals(Result struc);
    const bool operator==(const Formula & rhs);
    Formula & operator=(const Formula & rhs);
    const bool operator!=(const Formula & rhs);
    int GetHashCode();

};

struct Result{

    int type; // Type 1 = formula, Type 2 = Number, Type 3 = Error

    Formula formula;
    double number;
    std::string reason;

    Result() :
        type{3},
        reason{""}
    {
    }

    Result(const Result &other) :
        type{other.type},
        number{other.number},
        reason{other.reason},
        formula{other.formula}
    {
    }

    Result operator=(const Result &other)
    {
        this->type = other.type;
        this->number = other.number;
        this->reason = other.reason;
        this->formula = other.formula;
        return *this;
    }

    std::string ToString()
    {
        switch (type)
        {
            case 1:
                return "=" + formula.ToString();
                break;
            case 2:
                return std::to_string(number);
            case 3:
            default:
                return reason;
        }
    }
};

#endif //SPREADSHEETLOGIC_FORMULA_H
