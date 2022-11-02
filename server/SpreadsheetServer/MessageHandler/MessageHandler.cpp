#include <iostream>

#include "MessageHandler.h"
#include <nlohmann/json.hpp>

void MessageHandler::ParseAndRespond(
    std::unordered_map<std::string, std::vector<int>> &connectionMap,
    std::vector<ClientMetadata> &metadataVec, int ID, std::string message,
    std::vector<sf::TcpSocket *> &clients,
    std::map<std::string, Spreadsheet> &spreadsheetMap) {
  sf::TcpSocket *client = clients[ID];
  if (!message.empty() && message[message.length() - 1] == '\n') {
    message.erase(message.length() - 1);
  }

  if (!metadataVec.at(ID).handshakeFinished) {
    if (metadataVec.at(ID).name == "") {
      metadataVec.at(ID).name = message;

      std::stringstream filelist;

      for (auto kv : spreadsheetMap)
        filelist << kv.first << "\n";
      filelist << "\n";

      client->send(filelist.str().c_str(),
                   sizeof(char) * filelist.str().length());
    } else {
      if (spreadsheetMap.count(message) == 0) {
        Spreadsheet s = Spreadsheet();
        spreadsheetMap.emplace(message, s);
        std::vector<int> clients;
        connectionMap.emplace(message, clients);
        s.Save(message);
      }

      metadataVec.at(ID).spreadsheet = message;
      connectionMap.at(message).push_back(ID);
      std::string s = std::to_string(ID) + "\n";
      metadataVec.at(ID).handshakeFinished = true;

      std::vector<cell_states> states =
          spreadsheetMap.at(message).GetCellStatesFromUndoStack();

      for (auto cs : states) {
        std::string contents;

        if (cs.cell_contents.type == 1) {
          contents = "=" + cs.cell_contents.formula.ToString();
        } else if (cs.cell_contents.type == 2) {
          contents = std::to_string(cs.cell_contents.number);
        } else {
          contents = cs.cell_contents.reason;
        }
        std::string cellUpdate =
            "{\"messageType\": \"cellUpdated\" , \"cellName\" : \"" +
            cs.cell_name + "\",\"contents\" : \"" + contents + "\"}\n";
        client->send(cellUpdate.c_str(), sizeof(char) * cellUpdate.length());
      }

      client->send(s.c_str(), sizeof(char) * s.length());
    }
  } else {
    std::string currentSpreadsheet = metadataVec.at(ID).spreadsheet;
    std::string cellName = "";

    try {
      auto json = nlohmann::json::parse(message);

      if (json.at("requestType") == "editCell") {
        std::string contents;
        json.at("cellName").get_to(cellName);
        json.at("contents").get_to(contents);
        Spreadsheet &currentSpreadsheet =
            spreadsheetMap.find(metadataVec.at(ID).spreadsheet)->second;

        if (metadataVec.at(ID).selectedCell != cellName) {
          std::string errorMessage = "Cannot edit cell that is not selected";
          std::string invalidRequest =
              "{\"messageType\": \"requestError\", \"cellName\": \"" +
              cellName + "\",\"message\":\"" + errorMessage + "\"}\n";
          client->send(invalidRequest.c_str(),
                       sizeof(char) * invalidRequest.length());
        } else {
          std::string errorMessage =
              trySetContents(currentSpreadsheet, cellName, contents,
                             metadataVec.at(ID).spreadsheet);
          if (errorMessage.empty()) {
            std::string cellUpdate =
                "{\"messageType\": \"cellUpdated\" , \"cellName\" : \"" +
                cellName + "\",\"contents\" : \"" + contents + "\"}\n";
            std::vector<int> &connections =
                connectionMap.find(metadataVec.at(ID).spreadsheet)->second;
            std::vector<int>::iterator it;
            for (it = connections.begin(); it != connections.end(); it++) {
              clients.at(*it)->send(cellUpdate.c_str(),
                                    sizeof(char) * cellUpdate.length());
            }
          } else {

            std::string invalidRequest =
                "{\"messageType\": \"requestError\", \"cellName\": \"" +
                cellName + "\",\"message\":\"" + errorMessage + "\"}\n";
            client->send(invalidRequest.c_str(),
                         sizeof(char) * invalidRequest.length());
          }
        }
      }
      if (json.at("requestType") == "selectCell") {
        std::string cellName = json.at("cellName");
        std::string selectorName = metadataVec.at(ID).name;
        std::string selectorID = std::to_string(ID);

        if (spreadsheetMap.find(metadataVec.at(ID).spreadsheet)
                ->second.CellNameValid(cellName)) {
          metadataVec.at(ID).selectedCell = cellName;
          std::string cellSelected =
              "{\"messageType\": \"cellSelected\" , \"cellName\" : \"" +
              cellName + "\",\"selector\" : \"" + selectorID +
              "\",\"selectorName\":\"" + selectorName + "\"}\n";
          std::vector<int> &connections =
              connectionMap.find(metadataVec.at(ID).spreadsheet)->second;
          for (auto client : connections) {
            clients.at(client)->send(cellSelected.c_str(),
                                     sizeof(char) * cellSelected.length());
          }
        } else {
          std::string invalidRequest =
              "{\"messageType\": \"requestError\", \"cellName\": \"" +
              cellName + "\",\"message\":\" cellName " + cellName +
              " was invalid \"}\n";
          client->send(invalidRequest.c_str(),
                       sizeof(char) * invalidRequest.length());
        }
      }
      if (json.at("requestType") == "revertCell") {
        std::string cellName = json.at("cellName");
        std::tuple<std::string, Result> tuple =
            spreadsheetMap.find(metadataVec.at(ID).spreadsheet)
                ->second.revert(cellName);

        if (std::get<1>(tuple).type == 4) {
          std::string invalidRequest =
              "{\"messageType\": \"requestError\", \"cellName\": \"" +
              cellName + "\",\"message\":\" cellName " + cellName +
              " cannot be reverted because" + std::get<1>(tuple).reason +
              " \"}\n";
          std::vector<int> &connections =
              connectionMap.find(metadataVec.at(ID).spreadsheet)->second;
          std::vector<int>::iterator it;

          client->send(invalidRequest.c_str(),
                       sizeof(char) * invalidRequest.length());

        } else {
          Result result = std::get<1>(tuple);
          std::string contents = result.ToString();
          // iterate throug clients and send
          std::string cellUpdate =
              "{\"messageType\": \"cellUpdated\" , \"cellName\" : \"" +
              cellName + "\",\"contents\" : \"" + contents + "\"}\n";
          std::vector<int> &connections =
              connectionMap.find(metadataVec.at(ID).spreadsheet)->second;
          std::vector<int>::iterator it;
          spreadsheetMap.find(metadataVec.at(ID).spreadsheet)
              ->second.Save(metadataVec.at(ID).spreadsheet);
          for (it = connections.begin(); it != connections.end(); it++) {
            clients.at(*it)->send(cellUpdate.c_str(),
                                  sizeof(char) * cellUpdate.length());
          }
        }
      }
      if (json.at("requestType") == "undo") {
        std::tuple<std::string, Result> tuple =
            spreadsheetMap.find(metadataVec.at(ID).spreadsheet)->second.undo();

        std::string cellName = std::get<0>(tuple);
        if (cellName == "") {
          std::string invalidRequest =
              "{\"messageType\": \"requestError\", \"cellName\": \"" +
              cellName + "\",\"message\":\" cannot undo\"}\n";
          client->send(invalidRequest.c_str(),
                       sizeof(char) * invalidRequest.length());
        } else {
          Result result = std::get<1>(tuple);
          std::string contents;
          if (result.type == 1) {
            contents = "=" + result.formula.ToString();
          } else if (result.type == 2) {
            contents = std::to_string(result.number);
          } else {
            contents = result.reason;
          }
          // iterate throug clients and send
          std::string cellUpdate =
              "{\"messageType\": \"cellUpdated\" , \"cellName\" : \"" +
              cellName + "\",\"contents\" : \"" + contents + "\"}\n";
          std::vector<int> &connections =
              connectionMap.find(metadataVec.at(ID).spreadsheet)->second;
          std::vector<int>::iterator it;
          spreadsheetMap.find(metadataVec.at(ID).spreadsheet)
              ->second.Save(metadataVec.at(ID).spreadsheet);
          for (it = connections.begin(); it != connections.end(); it++) {
            clients.at(*it)->send(cellUpdate.c_str(),
                                  sizeof(char) * cellUpdate.length());
          }
        }
      }
    } catch (...) {
      std::string invalidRequest =
          "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
          "\",\"message\":\" received unparsable message\"}\n";
      client->send(invalidRequest.c_str(),
                   sizeof(char) * invalidRequest.length());
    }
  }
}

/*
 * Returns the error message that the spreadsheet throws if this does not work,
 * if it works returns an empty string.
 */
std::string MessageHandler::trySetContents(Spreadsheet &s, std::string cellname,
                                           std::string contents,
                                           std::string spreadsheetName) {
  try {
    s.SetContentsOfCell(cellname, contents);
    s.Save(spreadsheetName);
    return "";
  } catch (const CircularException &e) {
    return "Entry would create a circular exception";
  } catch (const std::domain_error &e) {
    return e.what();
  }
}

void MessageHandler::sendDisconnect(
    int clientID,
    std::unordered_map<std::string, std::vector<int>> &connectionMap,
    std::vector<sf::TcpSocket *> &clients,
    std::vector<ClientMetadata> &metadataVec) {
  std::string disconnect = "{\"messageType\":\"disconnected\",\"user\":\"" +
                           std::to_string(clientID) + "\"}\n";
  std::vector<int> &connections =
      connectionMap.find(metadataVec.at(clientID).spreadsheet)->second;
  std::vector<int>::iterator it;
  for (it = connections.begin(); it != connections.end(); it++) {
    clients.at(*it)->send(disconnect.c_str(),
                          sizeof(char) * disconnect.length());
  }
}

void MessageHandler::sendServerShutdown(std::vector<sf::TcpSocket *> &clients,
                                        std::string message) {
  std::string disconnect =
      "{\"messageType\":\"serverError\",\"message\":\"" + message + "\"}\n";
  for (auto client : clients) {
    client->send(disconnect.c_str(), sizeof(char) * disconnect.length());
  }
}
