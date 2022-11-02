#ifndef MESSAGEHANDLER_H
#define MESSAGEHANDLER_H

#include <unordered_map>
#include <sstream>

#include <SFML/Network.hpp>

#include "Spreadsheet/Spreadsheet.h"

struct ClientMetadata {
    int clientID;
    std::string name;
    bool handshakeFinished;
    bool disconnected;
    std::stringstream overflow;
    std::string spreadsheet;
    std::string selectedCell;

    ClientMetadata() :
        clientID{-1},
        name{""},
        handshakeFinished{false},
        disconnected{false},
        spreadsheet{""},
        selectedCell{""}
    {
    }

    ClientMetadata(const ClientMetadata& other) :
        clientID{other.clientID},
        name{other.name},
        handshakeFinished{other.handshakeFinished},
        disconnected{other.disconnected},
        spreadsheet{other.spreadsheet},
        selectedCell{other.selectedCell}
    {
    }
};

class MessageHandler
{
    public:
        static void ParseAndRespond(std::unordered_map<std::string, std::vector<int>> &connectionMap, std::vector<ClientMetadata> &metadataVec, int ID, std::string message, std::vector<sf::TcpSocket*> &clients, std::map<std::string, Spreadsheet> &spreadsheetMap);
        static void sendDisconnect(int clientID, std::unordered_map<std::string, std::vector<int>> &connectionMap, std::vector<sf::TcpSocket*> &clients, std::vector<ClientMetadata> &metadataVec);
        static void sendServerShutdown(std::vector<sf::TcpSocket*> &clients, std::string message);
    private:
        static std::string trySetContents(Spreadsheet &s, std::string cellName, std::string contents, std::string spreadsheetName);
};

#endif // MESSAGEHANDLER_H
