//Written by Annabel Hatfield and Alan Bird for CS3500
//Updated and edited for CS3505 by the Binary Bois
//
// (Version 1.0) - Added basic cell functionality.
// (Version 1.1) - Implemented File menu, saving, and opening spreadsheets.
// (Version 1.2) - Finished main functions, added text box selector, help menu and auto confirm
//                     Enter key now works for Confirm button, fonts and layout beginnings, exception handling
// (Version 1.3) - Added extra feature documentation, exception handling and general cleaning up of code. 
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SS;
using SpreadsheetUtilities;
using System.IO;
using SSController;
using System.Threading;

namespace SpreadsheetGUI
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// Contains the information in the spreadsheet.
        /// </summary>
        private Spreadsheet spreadsheet;

        /// <summary>
        /// Determines if variable name is valid. 
        /// </summary>
        private Func<string, bool> IsValid;

        /// <summary>
        /// Normalizes variable names. 
        /// </summary>
        private Func<string, string> Normalizer;

        /// <summary>
        /// Version information. 
        /// </summary>
        private string version;

        /// <summary>
        /// Stores current collunm information.
        /// </summary>
        private int currentCol;

        /// <summary>
        /// Stores current row information
        /// </summary>
        private int currentRow;

        /// <summary>
        /// The server controller that connects this client with the server.
        /// </summary>
        private Controller controller;

        /// <summary>
        /// Initalizes components and fields. 
        /// </summary>
        public Form1(Controller controller)
        {
            IsValid = s => true;
            Normalizer = s => s.ToUpper();
            version = "ps6";
            spreadsheet = new Spreadsheet(IsValid, Normalizer, version);
            InitializeComponent();
            SpreadsheetPanel.SelectionChanged += WhenSelectionChanges;
            currentCol = 0;
            currentRow = 0;
            this.controller = controller;
            this.Text = controller.getFileName();
            controller.onCellUpdate += new ControllerEventHandler(updateCell);
            controller.onCellSelection += new ControllerEventHandler(cellSelection);
            controller.onDisconnection += new ControllerEventHandler(disconnection);
            controller.onRequestError += new ControllerEventHandler(requestError);
            controller.onErrorOccured += new ControllerEventHandler(handleControllerError); //Main method takes care of these errors
            FormClosing += new FormClosingEventHandler(onClose);
        }

        public void updateCell(ControllerEventArgs e)
        {
            string cellName = e.getCellName();
            string cellContents = e.getCellContents();
            // parse the cell name into row/col
            ConvertNameToNum(cellName, out int col, out int row);
            setContentsFromServer(cellContents, col, row);
            //SpreadsheetPanel.SetValue(col, row, cellContents);
        }

        private void setContentsFromServer(string contents, int col, int row) 
        {
            string cellName = ConvertNumToName(col, row);
            IEnumerable<string> CellsToUpdate = new List<string>();
            try
            {
                CellsToUpdate = spreadsheet.SetContentsOfCell(cellName, contents);
            }
            catch (CircularException)
            {
                OpenHelpMessage("Circular dependency not allowed.", "ERROR");
            }
            catch (FormulaFormatException)
            {
                OpenHelpMessage("This is not a valid formula. Please try again.", "Error");
            }
            updateCell(CellsToUpdate);
        }

        public void cellSelection(ControllerEventArgs e)
        {
            int row, col;
            string selectedCellName = e.getCellName();
            int selector = e.getClientID();
            string selectorName = e.getClientName();
            ConvertNameToNum(selectedCellName, out col, out row);
            if(selector == controller.getUserID())
                UpdateTextBox();
            SpreadsheetPanel.setUserSelection(selector, col, row);
            SpreadsheetPanel.Invalidate();
            Invalidate();
        }

        public void disconnection(ControllerEventArgs e)
        {
            int clientID = e.getClientID();
            SpreadsheetPanel.removeUser(clientID);
        }

        public void requestError(ControllerEventArgs e)
        {
            string cellName = e.getCellName();
            string errorMessage = e.getErrorMessage();
            OpenHelpMessage(errorMessage, "Cell " + cellName + " Error");
        }

        public void handleControllerError(ControllerEventArgs e)
        {
            if (!controller.getConnected())
                this.Invoke(new MethodInvoker(()=> this.Close()));
        }


        /// <summary>
        /// Listner for selection changed event within the spreadsheet panel. 
        /// </summary>
        /// <param name="panel">The spreadsheet panel of the Spreadsheet.</param>
        private void WhenSelectionChanges(SpreadsheetPanel panel)
        {
            //Removed functionality because this could overload server
            //Cell can now only be set by enter button and clicking confirm

            //SetCell(ContentsBox.Text, currentCol, currentRow);
            //UpdateTextBox();
            UpdateCellLabel();
            controller.sendSelectRequest(ConvertNumToName(currentCol, currentRow));
        }

        /// <summary>
        /// Stores the current cell in col and row fields so when selecting a new cell the previous cell can still be updated. 
        /// </summary>
        private void UpdateCellLabel()
        {
            SpreadsheetPanel.GetSelection(out int col, out int row);
            CellNameLabel.Text = ConvertNumToName(col, row);

            currentCol = col;
            currentRow = row;
        }

        /// <summary>
        /// Listner for when Confirm buttion is clicked. 
        /// </summary>
        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            UpdateContents(ContentsBox.Text);
        }

        /// <summary>
        /// When called, updates the contents text box to the contents of the selected cell. 
        /// </summary>
        private void UpdateTextBox()
        {
            SpreadsheetPanel.GetSelection(out int col, out int row);
            string cellName = ConvertNumToName(col, row);
            object contents = spreadsheet.GetCellContents(cellName);
            string contentsString;
            if (typeof(Formula).IsInstanceOfType(contents))
            {
                contentsString = "=" + contents.ToString();
            }
            else
            {
                contentsString = contents.ToString();
            }
            ContentsBox.Invoke(new MethodInvoker(delegate { ContentsBox.Text = contentsString; }));
            
            ContentsBox.Invoke(new MethodInvoker(delegate{ ContentsBox.Focus(); }));
        }


        /// <summary>
        /// Updates the contents of the selected cell to the given input. 
        /// </summary>
        private void UpdateContents(string Contents)
        {
            SpreadsheetPanel.GetSelection(out int col, out int row);
            SetCell(Contents, col, row);
        }

        /// <summary>
        /// Sets the contents of a Cell in a spreadsheetPanel.
        /// </summary>
        /// <param name="Contents"> contents to go in cell </param>
        /// <param name="col"> column of cell </param>
        /// <param name="row"> row of cell </param>
        private void SetCell(string Contents, int col, int row)
        {
            controller.sendEditRequest(ConvertNumToName(col, row), Contents);


            //This only updates the local spreadSheet
            //moved to updateContentsFromServer

            //string cellName = ConvertNumToName(col, row);
            //IEnumerable<string> CellsToUpdate = new List<string>();
            //try
            //{
            //    CellsToUpdate = spreadsheet.SetContentsOfCell(cellName, Contents);
            //}
            //catch (CircularException)
            //{
            //    OpenHelpMessage("Circular dependency not allowed.", "ERROR");
            //}
            //catch (FormulaFormatException)
            //{
            //    OpenHelpMessage("This is not a valid formula. Please try again.", "Error");
            //}
            //updateCell(CellsToUpdate);

        }

        /// <summary>
        /// Takes the numbers representing a cell's row and colunm and returns a string representation of it. 
        /// </summary>
        private string ConvertNumToName(int col, int row)
        {
            char charCol = (char)(col + 65);
            return charCol + (row + 1).ToString();

        }

        /// <summary>
        /// converts the string representaiton of a cell and sets col and row to those values respectivly. 
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="col"></param>
        /// <param name="row"></param>
        private void ConvertNameToNum(string cell, out int col, out int row)
        {
            col = (int)(char.ToUpper(cell[0]) - 65);
            row = Int32.Parse(cell.Remove(0, 1)) - 1;
            
        }

        /// <summary>
        /// Listner for when Open buttion is clicked. 
        /// </summary>
        private void openButton_Click(object sender, EventArgs e)
        {
            openSpreadsheet();
        }

        /// <summary>
        /// Listner for when Save buttion is clicked. 
        /// </summary>
        private void saveButton_Click(object sender, EventArgs e)
        {
            saveSpreadsheet();
        }

        /// <summary>
        /// Call this each time the form is closing
        /// </summary>
        private void onClose(Object sender, FormClosingEventArgs e) 
        {
            controller.sendDisconnect();
        }

        /// <summary>
        /// saves a spreadsheet to a file.
        /// </summary>
        private void saveSpreadsheet()
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "All files (*.*)|*.*|sprd files (*.sprd)|*.sprd";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    // Code to write the stream goes here.
                    myStream.Close();
                }
                spreadsheet.Save(saveFileDialog1.FileName);
            }
        }

        /// <summary>
        /// opens a spreadsheet from a file, replacing the current spreadsheet.
        /// </summary>
        private void openSpreadsheet()
        {
            string filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "All files (*.*)|*.*|sprd files (*.sprd)|*.sprd";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;

                    try
                    {
                        spreadsheet = new Spreadsheet(filePath, IsValid, Normalizer, version);
                    }
                    catch
                    {
                        OpenHelpMessage("There was a problem opening the spreadsheet.", "ERROR");
                    }
                    SpreadsheetPanel.Clear();
                    IEnumerable<string> cells = spreadsheet.GetNamesOfAllNonemptyCells();
                    updateCell(cells);
                }
            }
        }

        /// <summary>
        /// For any number of cells updates the value of the given cells, the value of any dependants of that cell
        /// and updates the visuals on the spreadsheet panel. 
        /// </summary>
        /// <param name="cells"></param>
        private void updateCell(IEnumerable<string> cells)
        {
            foreach (string cell in cells)
            {
                char col = '!';
                string row = "";

                foreach (char c in cell)
                {
                    if (char.IsLetter(c))
                    {
                        col = c;
                    }
                    else
                    {
                        row += c;
                    }
                }
                string cellValue;
                if (typeof(FormulaError).IsInstanceOfType(spreadsheet.GetCellValue(cell)))
                {
                    cellValue = "Error";
                }
                else
                    cellValue = spreadsheet.GetCellValue(cell).ToString();
                SpreadsheetPanel.SetValue(col - 65, int.Parse(row) - 1, cellValue);
            }
        }

        /// <summary>
        /// Listner for when Close buttion is clicked. 
        /// </summary>
        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Displays the given message in a message box, with the given caption. 
        /// </summary>
        /// <param name="message">Main body of the message box.</param>
        /// <param name="caption">Caption of the message box.</param>
        private void OpenHelpMessage(string message, string caption)
        {
            DialogResult result = MessageBox.Show(message, caption);
        }

        /// <summary>
        /// Listner for when CellSelection buttion is clicked. Sends spesific help message.  
        /// </summary>
        private void CellSelectionHelpButton_Click(object sender, EventArgs e)
        {
            OpenHelpMessage("Click any cell to select it.", "How to select a cell");
        }

        /// <summary>
        /// Listner for when ChangeContents buttion is clicked. Sends spesific help message.  
        /// </summary>
        private void ChangeContentsHelpButton_Click(object sender, EventArgs e)
        {
            OpenHelpMessage("Begin typing with a cell selected, or type in the Cell Contents: text box to set contents. " +
                "to Confirm contents into a cell, click the confirm button, press the Enter key, or simply select another cell.",
                "How to change contents");
        }

        /// <summary>
        /// Listner for when SpecialFeature buttion is clicked. Sends spesific help message.  
        /// </summary>
        private void SpecialFeatureHelpButton_Click(object sender, EventArgs e)
        {
            OpenHelpMessage("If you set any cell's contents to a *, followed by a color OR an html hex code (ex. formatted " +
                "\"#ffffff\" for white) rather than the value of the cell, the cell will fill in that color!" +
                "accepted colors (not hex) are blue, red, green, yellow, violet, orange, black, and grey." +
                "\n \n For example, *blue or *#00ccff work as color contents.", "Special Feature!");
        }

        /// <summary>
        /// Sends a request to undo to server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Undo_Click(object sender, EventArgs e)
        {
            controller.sendUndoRequest();
        }

        /// <summary>
        /// Sends a request to revert the currently selected cell to the server. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Revert_Click(object sender, EventArgs e)
        {
            controller.sendRevertRequest(ConvertNumToName(currentCol, currentRow));
        }


        /// <summary>
        /// Shows popup box with each users username and color.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void seeUsers_Click(object sender, EventArgs e)
        {
            string toSend = "The current users connected and highlighting cells are: \n";
            toSend += "Username\t\tID\t\tColor\n";
            Dictionary<int, string> userMap = controller.getUserMap();
            foreach (int user in userMap.Keys)
            {
                toSend += userMap[user] + "\t\t\t" + user + "\t\t";
                switch (user % 7)
                {
                    case 0:
                        toSend += "Blue\n";
                        break;
                    case 1:
                        toSend += "Red\n";
                        break;
                    case 2:
                        toSend += "Green\n";
                        break;
                    case 3:
                        toSend += "Yellow\n";
                        break;
                    case 4:
                        toSend += "Violet\n";
                        break;
                    case 5:
                        toSend += "Orange\n";
                        break;
                    case 6:
                        toSend += "Pink\n";
                        break;
                    case 7:
                        toSend += "Gray\n";
                        break;
                }
            }

            toSend += "\n\nYou see your own color as black but others see your selection as defined above.";
            OpenHelpMessage(toSend,"user colors");
        }
    }
}
