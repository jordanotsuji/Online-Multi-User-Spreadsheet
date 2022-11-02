#include <signal.h>
#include <atomic>
#include <experimental/filesystem>
#include <iostream>
#include <list>
#include <sstream>
#include <unordered_map>
#include <cctype>

#include <SFML/Network.hpp>
#include <nlohmann/json.hpp>
#include <regex>

#include "MessageHandler/MessageHandler.h"
#include "Spreadsheet/Spreadsheet.h"

namespace fs = std::experimental::filesystem;

std::atomic<bool> _quit;
const ushort PORT = 1100;
const std::string SPREADSHEET_DIR = "/spreadsheets/";

void quit(int s) {
  (void)s; // We don't need 's' to handle the SIGINT

  std::cout << "\nQuiting like a big quitter.\n";
  _quit = true;
}

int main() {
  _quit = false;

  // Handle quiting the program using Ctrl+C
  struct sigaction sigIntHandler;
  sigIntHandler.sa_handler = quit;
  sigemptyset(&sigIntHandler.sa_mask);
  sigIntHandler.sa_flags = 0;
  sigaction(SIGINT, &sigIntHandler, nullptr);
  std::unordered_map<std::string, std::vector<int>> connectionMap =
      std::unordered_map<std::string, std::vector<int>>();

  std::map<std::string, Spreadsheet> spreadsheetMap =
      std::map<std::string, Spreadsheet>();

  for (auto &file : fs::directory_iterator(SPREADSHEET_DIR)) {
    std::string filename = file.path().filename();
    filename.erase(std::remove(filename.begin(), filename.end(), '\"'),
                   filename.end());
    Spreadsheet sprd = Spreadsheet(
        file.path().string(),
        [](std::string cellname) {
          return std::regex_match(cellname, std::regex("^[a-zA-Z]+[0-9]+$"));
        },
        [](std::string s) {
          std::transform(s.begin(), s.end(), s.begin(),
                         [](unsigned char c) { return std::toupper(c); });
          return s;
        },
        "1.0"); // TODO: Replace with actual functions
    spreadsheetMap.emplace(filename, sprd);
    std::vector<int> connectionID;
    connectionMap.emplace(filename, connectionID);
  }

  sf::TcpListener listener;
  if (listener.listen(PORT))
    return 1;
  std::vector<sf::TcpSocket *> clients;
  std::vector<ClientMetadata> clientMetadataVec = std::vector<ClientMetadata>();
  sf::SocketSelector selector;
  selector.add(listener);
  bool internalError = false;
  try {

    // Main server loop
    while (!_quit) {
      // Make the selector wait for data on any socket
      if (selector.wait(sf::milliseconds(10))) {
        // Test the listener
        if (selector.isReady(listener)) {
          // The listener is ready: there is a pending connection
          sf::TcpSocket *client = new sf::TcpSocket;
          if (listener.accept(*client) == sf::Socket::Done) {
            // Add the new client to the clients list
            clients.push_back(client);
            ClientMetadata meta;
            meta.clientID = clientMetadataVec.size();
            clientMetadataVec.push_back(meta);

            // Add the new client to the selector so that we will
            // be notified when he sends something
            selector.add(*client);
            std::cout << "New client connected\n";
          } else {
            // Error, we won't get a new connection, delete the socket
            std::cout << "Error connecting client\n";
            delete client;
          }
        } else {
          int clientID = -1;
          // The listener socket is not ready, test all other sockets (the
          // clients)
          for (std::vector<sf::TcpSocket *>::iterator it = clients.begin();
               it != clients.end(); ++it) {
            clientID++;
            sf::TcpSocket &client = **it;
            if (selector.isReady(client)) {
              // The client has sent some data, we can receive it
              const int BUFF_SIZE = 100;

              std::vector<char> buffer(BUFF_SIZE);
              std::size_t received;

              while (!clientMetadataVec.at(clientID).disconnected) {
                std::string message;
                buffer.clear();
                if (client.receive(buffer.data(), BUFF_SIZE, received) !=
                    sf::Socket::Done) {
                  clientMetadataVec.at(clientID).disconnected = true;

                  std::string clientSpreadsheet =
                      clientMetadataVec.at(clientID).spreadsheet;

                  if (connectionMap.count(clientSpreadsheet)) {

                    std::vector<int> &spreadsheetClients =
                        connectionMap.at(clientSpreadsheet);

                    std::vector<int>::iterator it =
                        find(spreadsheetClients.begin(),
                             spreadsheetClients.end(), clientID);
                    if (it != spreadsheetClients.end()) {
                      spreadsheetClients.erase(it);
                    }

                    MessageHandler::sendDisconnect(clientID, connectionMap,
                                                   clients, clientMetadataVec);

                    std::cout << "Client " << clientID << " Disconnected\n";
                  }

                  break;
                }

                std::stringstream newData;
                std::copy(buffer.begin(), buffer.begin() + received,
                          std::ostream_iterator<char>(newData));
                clientMetadataVec.at(clientID).overflow << newData.str();

                std::string data =
                    clientMetadataVec.at(clientID).overflow.str();

                auto pos = data.find_first_of('\n');

                if (pos != std::string::npos) {
                  message = data.substr(0, pos);
                  clientMetadataVec.at(clientID).overflow =
                      std::stringstream(std::string());
                  clientMetadataVec.at(clientID).overflow
                      << data.substr(pos + 1);
                  MessageHandler::ParseAndRespond(
                      connectionMap, clientMetadataVec, clientID, message,
                      clients, spreadsheetMap);
                  break;
                } else if (received != BUFF_SIZE) {
                  break;
                }
              }
            }
          }
        }
      }
    }
  } catch (const std::exception &ex) {
    listener.close();
    throw ex;
  } catch (const std::string &ex) {
    listener.close();
    throw ex;
  } catch (...) {
    listener.close();
    internalError = true;
  }

  // Cleanup any open sockets
  listener.close();
  std::string message = "The server was closed";
  if (internalError)
    message = "The server encountered an error";
  MessageHandler::sendServerShutdown(clients, message);
  for (auto &client : clients) {
    client->disconnect();
    delete client;
  }

  std::cout << "Goodbye...\n";
  return internalError;
}
