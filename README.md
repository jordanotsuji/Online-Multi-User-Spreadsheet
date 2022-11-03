# Online-Multi-User-Spreadsheet

Excel clone Written by Jordan Otsuji, Kyle Charlton, Joseph Rodman, Alan Bird, and Kai Zheng  

Features:  
- Formula Evaluation: Users can enter a mathematical formula into a cell and it will display the evaluated answer
- Cell Dependencies: Cell formulas can reference other cells for dynamic values. Circular dependencies will result in an error being shown in the cell
- Tested for high performance with up to 5 concurrent users

Instructions:
- Run the server located at server/SpreadsheetServer/main.cpp
- Run the spreadsheet executable (located at client_executable/SpreadsheetGUI) and connect to the server host's address 
