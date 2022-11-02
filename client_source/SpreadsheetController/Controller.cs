using Newtonsoft.Json;
using System;

namespace SpreadsheetController
{
    public class Controller
    {

        // Sending methods


        // Receving methods
        public void parseMessage(String message)
        {

            dynamic messageObj = JsonConvert.DeserializeObject(message);
            // using newtonsoft.json

            switch (messageObj.messageType)
            {
                case "Files": // TODO ask about the protocol for this
                    
                    break;
                case "cellUpdated":
                    string changedCellName = messageObj.cellName;
                    string contents = messageObj.contents;
                    break;
                case "cellSelected":
                    string selectedCellName = messageObj.cellName;
                    int selector = messageObj.selector;
                    string selectorName = messageObj.selectorName;
                    break;
                case "disconnected":
                    int userID = messageObj.user;
                    break;
                case "requestError":
                    string cellNameOnError = messageObj.cellName;
                    string errorMessage = messageObj.message;
                    break;
                case "serverError":
                    string serverErrorMessage = messageObj.message;
                    break;
                default:
                    Console.WriteLine("Defualt case reached with : " + messageObj.messageType);
                    break;
            }


        }

    }
}
