using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using NetworkUtil;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace SSController
{
    public class Controller
    {
        // Create way to manage the differnt clients - a client is a ID and a color
        // This file will use the tank wars networking lib
        // Alan will add sending of name on connection

        // This is the socket that the connection is made through
        public SocketState socketState;

        private string UserName;
        private string FilesString;
        private string FileName;
        private int UserID = -1;
        private bool InHandshake = false;
        private string SearchingData;
        private string UnprocessedData;
        private bool Connected = false;
        private Dictionary<int, string> userMap;

        // Sending methods

        /// <summary>
        /// Sends the information necessary on first connect
        /// </summary>
        /// <param name="userName"> The ID # of the user </param>
        public void sendConnectRequest(string userName, string hostName, int port)
        {
            UserName = userName;
            Networking.ConnectToServer(onConnect, hostName, port);
        }

        /// <summary>
        /// First connection method, once connected we send our username and prepare to receieve file names. 
        /// </summary>
        /// <param name="ss"></param>
        private void onConnect(SocketState ss)
        {
            if (ss.ErrorOccured)
            {
                Connected = false;
                internalError(ss.ErrorMessage);
            }
            socketState = ss;

            Connected = true;
            InHandshake = true;
            FilesString = "";
            ss.OnNetworkAction = receiveFileNames;
            Networking.Send(ss.TheSocket, UserName + "\n");
            Networking.GetData(ss);
        }

        /// <summary>
        /// Inital on network action, recieves file Names.
        /// </summary>
        /// <param name="ss"></param>
        private void receiveFileNames(SocketState ss)
        {
            if (ss.ErrorOccured)
            {
                Connected = false;
                internalError(ss.ErrorMessage);
            }

            ControllerEventArgs eventArguments;

            String currentData = ss.GetData();
            ss.RemoveData(0, currentData.Length);
            FilesString += currentData;
            String[] filesArr = Regex.Split(FilesString, @"(?<=[\n])");
            //To ensure that we have all files, we first check the total data we are working with ends with a 
            //newline and the final element of the array we split up is empty
            if (currentData.EndsWith("\n") && filesArr[filesArr.Length - 1].Length == 0)
            {
                eventArguments = new ControllerEventArgs(filesArr, this);
                onFilesSent(eventArguments);

                //Set this to on network action, loops to find full Json
                SearchingData = "";
                UnprocessedData = "";
                ss.OnNetworkAction = receiveData;
                Networking.GetData(ss);
            }
        }

        /// <summary>
        /// Sends a request to the server to open the spreadsheet with the specified name
        /// </summary>
        /// <param name="nameOfSpreadsheet"> The name of the spreadsheet to be opened </param>
        public void sendOpenSpreadsheetRequest(string nameOfSpreadsheet)
        {
            string toSend = nameOfSpreadsheet + "\n";
            Networking.Send(socketState.TheSocket, toSend);
        }

        /// <summary>
        /// Sends a request to the server telling it that the client is selecting a cell
        /// </summary>
        /// <param name="cellName"> The name of the cell that is selected </param>
        public void sendSelectRequest(string cellName)
        {
            string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";

            Networking.Send(socketState.TheSocket, toSend);
        }

        public void setFileName(string name)
        {
            FileName = name;
        }

        public string getFileName()
        {
            return FileName;
        }

        /// <summary>
        /// Sends a request to the server to edit a cell
        /// </summary>
        /// <param name="cellName"> The name of the cell that will be edited </param>
        /// <param name="contents"> The contents that will be put into the cell </param>
        public void sendEditRequest(string cellName, string contents)
        {
            string toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\", \"contents\": \"" + contents + "\"}\n";

            Networking.Send(socketState.TheSocket, toSend);
        }

        /// <summary>
        /// Sends a request to the server to undo the most recent change in the spreadsheet
        /// </summary>
        public void sendUndoRequest()
        {
            string toSend = "{\"requestType\": \"undo\"}\n";

            Networking.Send(socketState.TheSocket, toSend);
        }

        /// <summary>
        /// Sends a request to the server to revert the most recent change to a specified cell
        /// </summary>
        /// <param name="cellName"> The name of the cell to revert </param>
        public void sendRevertRequest(string cellName)
        {
            string toSend = "{\"requestType\": \"revertCell\", \"cellName\": \"" + cellName + "\"}\n";

            Networking.Send(socketState.TheSocket, toSend);
        }

        /// <summary>
        /// Sends a disconnect message to the server, notifying it that this user is not connected
        /// and sets the connected field to false.
        /// </summary>
        public void sendDisconnect()
        {
            Connected = false;
        }


        // Event handlers
        public event ControllerEventHandler onCellUpdate;
        public event ControllerEventHandler onCellSelection;
        public event ControllerEventHandler onDisconnection;
        public event ControllerEventHandler onRequestError;
        public event ControllerEventHandler onFilesSent;
        public event ControllerEventHandler onErrorOccured;

        /// <summary>
        /// On network action meant to loop through recieved data and add it to growing string
        /// of data, searcing for full Json and passing that to parse message.
        /// </summary>
        /// <param name="ss"></param>
        private void receiveData(SocketState ss)
        {
            if (ss.ErrorOccured)
            {
                Connected = false;
                internalError(ss.ErrorMessage);
            }
            SearchingData = ss.GetData();
            ss.RemoveData(0, SearchingData.Length);
            SearchingData = UnprocessedData + SearchingData;

            String[] tokens = Regex.Split(SearchingData, "\n");
            List<JObject> jTokens = new List<JObject>();

            foreach (string s in tokens)
            {
                string token = s.Trim();
                if (token == "")
                    continue;
                try
                {
                    JObject j = JObject.Parse(token);
                    if (!(j is null))
                        jTokens.Add(j);
                }
                catch
                {
                    if (InHandshake)
                    {
                        //If we are still in the handshake we will try to parse the garbage recieved here to an int
                        //if we can, we leave the recieved int out of Unprocessed data and leave the handshake,
                        //but if we cannot we continue with the normal attempt at fixing the data.
                        if (InHandshake = !Int32.TryParse(token, out UserID))
                        {
                            UnprocessedData += token;
                        }
                        else
                        {
                            userMap = new Dictionary<int, string>();
                            userMap.Add(UserID, UserName);
                        }
                    }
                    else
                        UnprocessedData += token;
                    // if this catches, then the server must have sent a cell containing a '}' character.
                    // or the server has sent garbage to us. Either way we add it to this list and try
                    // using it one more time. 
                }
            }
            foreach (JObject obj in jTokens)
                parseMessage(obj);
            Thread.Sleep(10);
            Networking.GetData(ss);
        }

        /// <summary>
        /// Given a jObject representing an action from the server, parses the data from said object and calls
        /// events which tells the spreadsheet what to do. 
        /// </summary>
        /// <param name="obj"></param>
        public void parseMessage(JObject obj)
        {
            ControllerEventArgs eventArguments;

            switch (obj.GetValue("messageType").ToString())
            {
                case "cellUpdated":
                    string changedCellName = obj.GetValue("cellName").ToString();
                    string contents = obj.GetValue("contents").ToString();
                    eventArguments = new ControllerEventArgs(changedCellName, contents);
                    onCellUpdate(eventArguments);
                    break;
                case "cellSelected":
                    string selectedCellName = obj.GetValue("cellName").ToString();
                    int selector = obj.GetValue("selector").ToObject<int>();
                    string selectorName = obj.GetValue("selectorName").ToString();
                    eventArguments = new ControllerEventArgs(selectedCellName, selector, selectorName);
                    if (!userMap.ContainsKey(selector))
                        userMap.Add(selector, selectorName);
                    onCellSelection(eventArguments);
                    break;
                case "disconnected":
                    int userID = obj.GetValue("user").ToObject<int>();
                    eventArguments = new ControllerEventArgs(userID);
                    if (userMap.ContainsKey(userID))
                        userMap.Remove(userID);
                    onDisconnection(eventArguments);
                    break;
                case "requestError":
                    string cellNameOnError = obj.GetValue("cellName").ToString();
                    string errorMessage = obj.GetValue("message").ToString();

                    // The first argument is used to dismbiguate between cellUpdate and errorMessage constructors for the ControllerEventArgs
                    eventArguments = new ControllerEventArgs(1, cellNameOnError, errorMessage);
                    onRequestError(eventArguments);
                    break;
                case "serverError":
                    string serverErrorMessage = obj.GetValue("message").ToString();
                    eventArguments = new ControllerEventArgs(serverErrorMessage);
                    Connected = false;
                    onErrorOccured(eventArguments);
                    // Needs its own event
                    break;
                default:
                    Console.WriteLine("Defualt case reached with : " + obj.GetValue("messageType").ToString());
                    break;
            }

        }

        public void networkingGetData()
        {
            //Networking.GetData(socketState);
        }

        /// <summary>
        /// A method to call when there is an error with the socket state or a server error occurs.
        /// </summary>
        /// <param name="ErrorMessage"></param>
        private void internalError(string ErrorMessage)
        {
            Connected = false;
            ControllerEventArgs args = new ControllerEventArgs(ErrorMessage);
            onErrorOccured(args);
        }

        /// <summary>
        /// Returns weather or not the client is in the hanshake.
        /// </summary>
        /// <returns></returns>
        public bool inHanshake()
        {
            return InHandshake;
        }

        /// <summary>
        /// returns weather or not the client is connected to the server.
        /// </summary>
        /// <returns></returns>
        public bool getConnected()
        {
            return Connected;
        }

        /// <summary>
        /// set the connected value
        /// </summary>
        /// <param name="c"></param>
        public void setConnected(bool c)
        {
            Connected = c;
        }

        /// <summary>
        /// returns the users id.
        /// </summary>
        /// <returns></returns>
        public int getUserID()
        {
            return UserID;
        }

        /// <summary>
        /// return the map containing the users connected to the spreadsheet and their ids.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> getUserMap()
        {
            return userMap;
        }
    }


    public delegate void ControllerEventHandler(ControllerEventArgs e);

    public class ControllerEventArgs : EventArgs
    {
        private string cellContents;
        private string cellName;
        private string clientName;
        private string errorMessage;
        private int clientID;
        private string[] files;
        private Controller controller;

        public ControllerEventArgs(string cellName, string cellContents)
        {
            this.cellName = cellName;
            this.cellContents = cellContents;
        }

        public ControllerEventArgs(string[] files, Controller ctrl)
        {
            this.files = files;
            this.controller = ctrl;
        }

        public ControllerEventArgs(int clientID)
        {
            this.clientID = clientID;
        }

        public ControllerEventArgs(string cellName, int clientID, string clientName)
        {
            this.cellName = cellName;
            this.clientID = clientID;
            this.clientName = clientName;
        }

        public ControllerEventArgs(string serverErrorMessage)
        {
            this.errorMessage = serverErrorMessage;
        }

        public ControllerEventArgs(int unused, string cellName, string errorMessage)
        {
            this.cellName = cellName;
            this.errorMessage = errorMessage;
        }

        public string getErrorMessage()
        {
            return this.errorMessage;
        }

        public int getClientID()
        {
            return this.clientID;
        }

        public string getClientName()
        {
            return this.clientName;
        }

        public string getCellName()
        {
            return this.cellName;
        }

        public string getCellContents()
        {
            return this.cellContents;
        }

        public string[] getFiles()
        {
            return this.files;
        }

        public Controller getController()
        {
            return controller;
        }
    }

}