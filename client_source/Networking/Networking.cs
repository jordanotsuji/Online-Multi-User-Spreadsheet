using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkUtil
{
    /// <summary>
    /// Class that represents the operations of a client and server for establishing a connection, sending and receiving data, and error handling
    /// @Author Kai Zheng and Daniel Stoney
    /// </summary>
    public static class Networking
    {
        /////////////////////////////////////////////////////////////////////////////////////////
        // Server-Side Code
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
        /// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
        /// AcceptNewClient will continue the event-loop.
        /// </summary>
        /// <param name="toCall">The method to call when a new connection is made</param>
        /// <param name="port">The the port to listen on</param>
        public static TcpListener StartServer(Action<SocketState> toCall, int port)
        {
            // New listener that will take any IP, and whatever port is provided StartServer
            TcpListener tcpListener = new TcpListener(IPAddress.Any, port);

            tcpListener.Start();

            Tuple<Action<SocketState>, TcpListener> serverState = new Tuple<Action<SocketState>, TcpListener>(toCall, tcpListener);

            // Begin our loop with the callback as specified and the state with our action and listener
            tcpListener.BeginAcceptSocket(AcceptNewClient, serverState);

            return tcpListener;
        }

        /// <summary>
        /// To be used as the callback for accepting a new client that was initiated by StartServer, and 
        /// continues an event-loop to accept additional clients.
        ///
        /// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
        /// OnNetworkAction should be set to the delegate that was passed to StartServer.
        /// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
        /// 
        /// If anything goes wrong during the connection process (such as the server being stopped externally), 
        /// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccured flag set to true 
        /// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
        /// an error occurs.
        ///
        /// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
        /// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
        /// </summary>
        /// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
        /// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
        private static void AcceptNewClient(IAsyncResult ar)
        {
            // Need to create another tuple because we can't cast ar to an Action<SocketState>
            // (Item1 = Action<SocketState> and Item2 = TcpListener)
            Tuple<Action<SocketState>, TcpListener> serverState = (Tuple<Action<SocketState>, TcpListener>) ar.AsyncState;

            // Attempt to complete the connection process
            try
            {
                // Finalize the connection using the tuple
                Socket newClient = serverState.Item2.EndAcceptSocket(ar);

                // Create the new socket state here and set the OnNetworkAction to the one provided from the StartServer method
                SocketState serverSocketState = new SocketState(serverState.Item1, newClient)
                {
                    OnNetworkAction = serverState.Item1
                };

                // Invoke the delegate with the new SocketState
                serverSocketState.OnNetworkAction.Invoke(serverSocketState);
            }
            catch(Exception e)
            {
                // When an error occurs, we create a new SocketState and invoke the OnNetworkAction with it
                SocketState errorSocketState = new SocketState(serverState.Item1, null)
                {
                    ErrorOccured = true,
                    ErrorMessage = "An error occurred while trying to finalize the connection in AcceptNewClient: " + e.Message
                };

                errorSocketState.OnNetworkAction.Invoke(errorSocketState);

                // Stop the loop here 
                return;
            }

            // If no errors occurred, begin the event loop, passing in this method as the callback and the tuple as it's state 
            serverState.Item2.BeginAcceptSocket(AcceptNewClient, serverState);
        }

        /// <summary>
        /// Stops the given TcpListener.
        /// </summary>
        public static void StopServer(TcpListener listener)
        {
            listener.Stop();
        }

        /////////////////////////////////////////////////////////////////////////////////////////
        // Client-Side Code
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Begins the asynchronous process of connecting to a server via BeginConnect, 
        /// and using ConnectedCallback as the method to finalize the connection once it's made.
        /// 
        /// If anything goes wrong during the connection process, toCall should be invoked 
        /// with a new SocketState with its ErrorOccured flag set to true and an appropriate message 
        /// placed in its ErrorMessage field. Between this method and ConnectedCallback, toCall should 
        /// only be invoked once on error.
        ///
        /// This connection process should timeout and produce an error (as discussed above) 
        /// if a connection can't be established within 3 seconds of starting BeginConnect.
        /// 
        /// </summary>
        /// <param name="toCall">The action to take once the connection is open or an error occurs</param>
        /// <param name="hostName">The server to connect to</param>
        /// <param name="port">The port on which the server is listening</param>
        public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port)
        {
            // Establish the remote endpoint for the socket.
            IPHostEntry ipHostInfo;
            IPAddress ipAddress = IPAddress.None;

            // Create the state object (will be used for errors)
            SocketState errorState = new SocketState(toCall, null);

            // Determine if the server address is a URL or an IP
            try
            {
                ipHostInfo = Dns.GetHostEntry(hostName);
                bool foundIPV4 = false;
                foreach (IPAddress addr in ipHostInfo.AddressList)
                    if (addr.AddressFamily != AddressFamily.InterNetworkV6)
                    {
                        foundIPV4 = true;
                        ipAddress = addr;
                        break;
                    }
                // Didn't find any IPV4 addresses
                if (!foundIPV4)
                {
                    // Set the error and invoke toCall
                    errorState.ErrorOccured = true;
                    errorState.ErrorMessage = "Unable to find IPV4 address";
                    errorState.OnNetworkAction.Invoke(errorState);

                    // Putting a return after errors so it doesn't continue with invalid parameters
                    return; 
                }
            }
            catch (Exception)
            {
                // see if host name is a valid ipaddress
                try
                {
                    ipAddress = IPAddress.Parse(hostName);
                }
                catch (Exception)
                {
                    // We catch an error involving parsing of the ipaddress here, and invoke the toCall action here
                    // Either the IPV4 bool check with cause an invoke or this will, not both
                    errorState.ErrorOccured = true;
                    errorState.ErrorMessage = "Could not resolve hostname.";
                    errorState.OnNetworkAction.Invoke(errorState);
                }
            }

            // Create a TCP/IP socket.
            Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // This disables Nagle's algorithm (google if curious!)
            // Nagle's algorithm can cause problems for a latency-sensitive 
            // game like ours will be 
            socket.NoDelay = true;

            try
            {
                // Begin connect with a new socket state using the newly created socket
                IAsyncResult connection = socket.BeginConnect(ipAddress, port, ConnectedCallback, new SocketState(toCall, socket));

                // Set a boolean flag to see if a connection is established within three seconds
                bool connectedWithinThreeSeconds = connection.AsyncWaitHandle.WaitOne((3000));

                // If no connection was made in 3 seconds, close the socket
                if (!connectedWithinThreeSeconds)
                {
                    socket.Close();
                }
            }
            catch (Exception e)
            {
                // Set the error properties if one occurs
                errorState.ErrorOccured = true;
                errorState.ErrorMessage = "An error occurred while beginning the connection process in ConnectToServer: " + e.Message;
            }
            
        }

        /// <summary>
        /// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
        ///
        /// Uses EndConnect to finalize the connection.
        /// 
        /// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
        /// either this method or ConnectToServer (not both) should indicate the error appropriately.
        /// 
        /// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
        /// with a new SocketState representing the new connection.
        /// 
        /// </summary>
        /// <param name="ar">The object asynchronously passed via BeginConnect</param>
        private static void ConnectedCallback(IAsyncResult ar)
        {
            // Create the socket
            SocketState state = (SocketState)ar.AsyncState;
            try
            {
                // Complete the connection
                state.TheSocket.EndConnect(ar);
                
                // Need to create a new SocketState with the provided toCall and socket, then we can invoke
                SocketState callBackSocketState = new SocketState(state.OnNetworkAction, state.TheSocket);

                state.OnNetworkAction.Invoke(callBackSocketState);
                
            }
            catch(Exception e)
            {
                state.ErrorOccured = true;
                state.ErrorMessage = "An error occurred within ConnectedCallback: "  + e.Message;

                // Invoke the error here
                state.OnNetworkAction.Invoke(state);
            }
        }


        /////////////////////////////////////////////////////////////////////////////////////////
        // Server and Client Common Code
        /////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
        /// as the callback to finalize the receive and store data once it has arrived.
        /// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
        /// 
        /// If anything goes wrong during the receive process, the SocketState's ErrorOccured flag should 
        /// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
        /// OnNetworkAction should be invoked. Between this method and ReceiveCallback, OnNetworkAction should only be 
        /// invoked once on error.
        /// 
        /// </summary>
        /// <param name="state">The SocketState to begin receiving</param>
        public static void GetData(SocketState state)
        {
            try
            {
                // Begin the receive process, use the length of the given buffer as the size 
                state.TheSocket.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, ReceiveCallback, state);                
            }
            catch(Exception e)
            {
                // Set the flags and message for any error that occurs during this method
                state.ErrorOccured = true;
                state.ErrorMessage = "An error occurred while attempting to start the receive process in GetData: " + e.Message;
            }
        }

        /// <summary>
        /// To be used as the callback for finalizing a receive operation that was initiated by GetData.
        /// 
        /// Uses EndReceive to finalize the receive.
        ///
        /// As stated in the GetData documentation, if an error occurs during the receive process,
        /// either this method or GetData (not both) should indicate the error appropriately.
        /// 
        /// If data is successfully received:
        ///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
        ///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
        ///      string builder.
        ///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
        /// </summary>
        /// <param name="ar"> 
        /// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
        /// </param>
        private static void ReceiveCallback(IAsyncResult ar)
        {
            // Pull the SocketState from ar
            SocketState state = (SocketState)ar.AsyncState;
            try
            {
                // Read the data
                int bytesRead = state.TheSocket.EndReceive(ar);

                // If we have data
                if (bytesRead > 0)
                {
                    // Store in the data buffer (1)
                    lock (state)
                    {
                        // There might be more data, so store the data
                        state.data.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                    }

                    // Invoke the saved delegate (2)
                    state.OnNetworkAction.Invoke(state);
                }
            }
            catch(Exception e)
            {
                // Catch errors and invoke it on the SocketState
                state.ErrorOccured = true;
                state.ErrorMessage = "An error occurred while attempting to the read the data in ReceiveCallback: " + e.Message;
                state.OnNetworkAction.Invoke(state);
            }
        }

        /// <summary>
        /// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
        /// 
        /// If the socket is closed, does not attempt to send.
        /// 
        /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
        /// </summary>
        /// <param name="socket">The socket on which to send the data</param>
        /// <param name="data">The string to send</param>
        /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
        public static bool Send(Socket socket, string data)
        {
            // Need to check if the socket is closed and not attempt to send
            if (!socket.Connected)
            {
                return false;
            }

            try
            {
                // Convert the string to byte
                byte[] byteData = Encoding.UTF8.GetBytes(data);

                // Begin sending data
                socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, SendCallback, socket);
                return true;
            }
            catch (Exception)
            {
                // Close the socket before returning
               socket.Close();
               return false;
            }
        }

        /// <summary>
        /// To be used as the callback for finalizing a send operation that was initiated by Send.
        ///
        /// Uses EndSend to finalize the send.
        /// 
        /// This method must not throw, even if an error occured during the Send operation.
        /// </summary>
        /// <param name="ar">
        /// This is the Socket (not SocketState) that is stored with the callback when
        /// the initial BeginSend is called.
        /// </param>
        private static void SendCallback(IAsyncResult ar)
        {
            // Create a new socket with the passed in state, then finalize the send operation
            Socket sendSocket = (Socket)ar.AsyncState;
            sendSocket.EndSend(ar);
        }


        /// <summary>
        /// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
        /// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
        /// 
        /// If the socket is closed, does not attempt to send.
        /// 
        /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
        /// </summary>
        /// <param name="socket">The socket on which to send the data</param>
        /// <param name="data">The string to send</param>
        /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
        public static bool SendAndClose(Socket socket, string data)
        {
            // Make sure the socket isn't closed
            if (!socket.Connected)
            {
                // Don't allow a send attempt
                return false;
            }

            try
            {
                // Convert the string to byte 
                byte[] byteData = Encoding.UTF8.GetBytes(data);

                // Begin sending data
                socket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, SendAndCloseCallback, socket);
                return true;
            }
            catch (Exception)
            {
                // Close the socket if an error occurs
                socket.Close();
                return false;
            }
        }

        /// <summary>
        /// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
        ///
        /// Uses EndSend to finalize the send, then closes the socket.
        /// 
        /// This method must not throw, even if an error occured during the Send operation.
        /// 
        /// This method ensures that the socket is closed before returning.
        /// </summary>
        /// <param name="ar">
        /// This is the Socket (not SocketState) that is stored with the callback when
        /// the initial BeginSend is called.
        /// </param>
        private static void SendAndCloseCallback(IAsyncResult ar)
        {
            // Create a new socket using the passed in parameter, then finalize the send and close the socket
            Socket sendSocket = (Socket)ar.AsyncState;
            sendSocket.EndSend(ar);
            sendSocket.Close();
        }

    }
}