*****README.TXT*****

Authors: Alan G Bird, Annabel R Hatfield
Secondary authors: The Binary Bois, Alan Bird, Kyle Charlton, Jordan Otsuji, Kai Zheng, Joseph Rodman. 

Features of Spreadsheet:
Saving - A chunk of the code here comes directly out of the Microsoft API for OpenFileDialoug. 
https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.openfiledialog?view=netcore-3.1

New - This code also comes mostly from the Microsoft API. 
https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.savefiledialog?view=netcore-3.1

Design - The font is mainly Bahnschrift Light Condensed. The form is on a black background with white text with
light blue accents. The cells are white with Bahnschrift Regular black text. The content text box widens as the
Form widens.

Changing cells - There are three methods for confirming the change of contents in a cell, Switcihing to a 
diffrent cell, pressing the confirm button or pressing enter. The method of switching cells is a good middle
ground between updating every time the text box changed (which would cause many updated cells when entering a
formula) and only having the confirm button. 

Message Boxes -  The error messages and help menues all appear in the form of message boxes, and the creation of
which comes mostly from the Microsoft API.
https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.messagebox?view=netcore-3.1

Handling Multipe Open Spreadsheets - The code to handle multiple open spreadsheets came entirely from the 
CS 3500 Examples repository. 

ADDITIONAL FEATURE:
Color content - SpreadsheetPanel recognises strings beginning in * as colors. It fills cells with string contents starting with * as a color.
Acceptable color strings are the colors of the rainbow, black, and gray. (ex. *red) This is case sensitive,
strings should be formatted in all lower case. It also accepts html hex codes, (ex. *#ffffff).
Unacceptable strings beginning with * set the cell's color to white (ex. *1234 sets to white).

This original spreadsheet application was updated for CS3505 to connect with any server created according to the JAKKPOT protocol. 