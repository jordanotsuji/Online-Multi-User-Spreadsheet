#include <string>
#include <vector>
#include "Spreadsheet.h"

int main()
{
    //Spreadsheet s = Spreadsheet("/spreadsheets/saved_spreadsheet.xml", [](std::string s) {return true;}, [](std::string s) {return s;}, "version");
    //s.SetContentsOfCell("C1", "60");
    Spreadsheet s;
    s.SetContentsOfCell("A1", "=A2");
    s.SetContentsOfCell("A2", "=a1");
    s.Save("saved_spreadsheet3.xml");
}
