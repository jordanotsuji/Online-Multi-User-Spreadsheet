#include <thread>
#include<iostream>
#include <chrono>
#include <list>
#include <sstream>
#include <vector>
#include <unordered_map>
#include <random>
#include <string>
#include <regex>
#include <atomic>

#include <SFML/Network.hpp>
#include <nlohmann/json.hpp>

#include "Spreadsheet/Spreadsheet.h"

const int NUM_TESTS = 17;
const int TEST_TIMEOUT_MS = 2000;

static std::stringstream overflow;
static std::atomic<bool> _quit;
int RunTest(int testNumber, std::string host, ushort port);
bool TryParse(const std::string& s, double& num);
bool GetID(std::string & message, sf::TcpSocket *socket, double & ID);
std::string generateRandomString();

/*
 * Reads from the given client and returns the read message.
 * Blocks until an entire message is recieved where a message
 * is terminated by '\n'. Returns "" if client timed out.
 */
std::string receiveMessage(sf::TcpSocket *socket, int timeoutMS)
{
    socket->setBlocking(false);

    const int BUFF_SIZE = 100;

    std::vector<char> buffer(BUFF_SIZE);
    std::size_t received;
    std::string message;
    bool connected = true;

    auto start = std::chrono::high_resolution_clock::now();
    while (true)
    {
        buffer.clear();
        if (socket->receive(buffer.data(), BUFF_SIZE, received) != sf::Socket::Done)
        {
            connected = false;
        }

        std::copy(buffer.begin(), buffer.begin() + received, std::ostream_iterator<char>(overflow));

        std::string data = overflow.str();

        auto pos = data.find_first_of('\n');

        if (pos != std::string::npos)
        {
            message = data.substr(0, pos);
            overflow = std::stringstream(std::string());
            overflow << data.substr(pos+1);
            socket->setBlocking(true);
            return message;
        }

        if (_quit)
            throw "timeout";

        if (std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::high_resolution_clock::now() - start).count() > timeoutMS)
        {
            socket->setBlocking(true);
            return "";
        }
    }
}

int main(int argc, char *argv[])
{
    _quit = false;

	if (argc == 1)
    {
        std::cout << NUM_TESTS << "\n";
        return 0;
    }

    if (argc != 3)
    {
        std::cout << "2 Arguments are required. No more. No less.\n";
        return 1;
    }

    // Parse commmand line arguments
    int testNumber = std::stoi(argv[1]);
    std::stringstream serverParams(argv[2]);

    std::string host;
    std::getline(serverParams, host, ':');

    std::string portStr;
    std::getline(serverParams, portStr, ':');
    ushort port = std::stoi(portStr);

    std::thread testThread(&RunTest, testNumber, host, port);
    testThread.detach();

    auto start = std::chrono::high_resolution_clock::now();
    while (!testThread.joinable())
    {
        if (std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::high_resolution_clock::now() - start).count() > TEST_TIMEOUT_MS)
        {
            _quit = true;
            break;
        }
    }

    if (testThread.joinable())
        testThread.join();
}

int RunTest(int testNumber, std::string host, ushort port)
{
    sf::TcpSocket serverSocket;
    serverSocket.connect(host, port);

    try
    {
    switch(testNumber)
    {
        case 1:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing getting ID, test 1\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "1\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;
                if(TryParse(message, ID))
                {
                    std::cout << "Pass" << "\n";
                }
                else
                {
                    std::cout << "Fail" << "\n";
                    return 1;
                }
            }
            catch(const std::exception& e)
                {
                    std::cerr << e.what() << '\n';
                    std::cout << "Fail\n";
                }
            break;
        case 2:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing send edit request before cell select, test 2\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "2\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail \n";
                    return 1;
                }

                std::string cellname = "A1";
                std::string contents = "5";
                std::string cellUpdate = "{\"requestType\": \"editCell\" , \"cellName\" : \"" + cellname + "\",\"contents\" : \"" + contents +"\"}\n";
                serverSocket.send(cellUpdate.c_str(), sizeof(char) * cellUpdate.length());

                message = receiveMessage(&serverSocket, 100);
                std::string expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellname +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 3:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing cell selection, test 3\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "3\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;
                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail \n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string contents = "5";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                std::string stringID = std::to_string((int)ID);


                message = receiveMessage(&serverSocket, 100);
                std::string expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + stringID + "\",\"selectorName\":\"" + "Test_Client" + "\"}";

                if(message == expected)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 4:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing valid Formula, test 4\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }

                std::string fileSelect = "4\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;
                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail" << "\n";
                    return 1;
                }


                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 100);

                cellName = "A1";
                std::string contents = "5";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 100);

                cellName = "B1";
                toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 100);

                cellName = "B1";
                contents = "=A1*6";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 100);

                std::string expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }

            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 5:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing Invalid message, test 5\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "5\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail \n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string contents = "5";
                std::string toSend = "\"\": \"\", \"\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                message = receiveMessage(&serverSocket, 100);
                cellName = "";
                std::string expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 6:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing Invalid Cell Name, test 6\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "6\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;
                if(!TryParse(message, ID))
                {
                    std::cout << "Fail" << "\n";
                    return 1;
                }

                std::string cellName = ":/;1";
                std::string contents = "5";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                message = receiveMessage(&serverSocket, 100);
                std::string expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 7:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Test Undo error, test 7\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "7\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;
                if(!TryParse(message, ID))
                {
                    std::cout << "Fail" << "\n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string contents = "5";
                std::string toSend = "{\"requestType\": \"undo\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                message = receiveMessage(&serverSocket, 100);
                std::string expectedCellName = "";
                std::string expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + expectedCellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 8:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing Revert Error, test 8\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "8\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;
                if(!TryParse(message, ID))
                {
                    std::cout << "Fail" << "\n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"revertCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                message = receiveMessage(&serverSocket, 100);
                std::string expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 9:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing No Request Type, test 9\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "9\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;
                if(!TryParse(message, ID))
                {
                    std::cout << "Fail" << "\n";
                }

                std::string cellName = "A1";
                std::string contents = "5";
                std::string toSend = "{\"requestType\": \"\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                message = receiveMessage(&serverSocket, 100);
                std::string expected = "";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 10:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing Cell Edit on unselected cell, test 10\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "10\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail \n";
                    return 1;
                }

                // Selected cell A1
                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 100);
                // Sent edit request to B1
                cellName = "B1";
                std::string contents = "5";

                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                message = receiveMessage(&serverSocket, 100);
                std::string expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 11:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing simple circular dependency, test 11\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "11\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;
                if(!TryParse(message, ID))
                {
                    std::cout << "Fail" << "\n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 100);
                std::string expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                std::string contents = "=A1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";

                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                message = receiveMessage(&serverSocket, 100);
                expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 12:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing long chain circular dependency, test 12\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "12\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    return 1;
                }

                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                std::string expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                std::string contents = "=B1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "B1";
                toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "B1";
                contents = "=C1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "C1";
                toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "C1";
                contents = "=D1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "D1";
                toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "D1";
                contents = "=E1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "E1";
                toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID)+ "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "E1";
                contents = "=A1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);

                expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 13:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing No Contents valid Json Edit Command, test 13\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "13\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail \n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 100);

                cellName = "A1";
                std::string contents = "=A1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\"}\n";

                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());

                message = receiveMessage(&serverSocket, 100);
                std::string expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 14:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing No CellName valid Json Select Command, test 14\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "14\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail \n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 100);

                cellName = "";
                std::string expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 15:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing revert into circular error, test 15\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "15\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                std::string expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << message << "\n";
                    std::cout << expected << "\n";
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                std::string contents = "=B1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                contents = "100";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "B1";
                toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "B1";
                contents = "=A1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                toSend = "{\"requestType\": \"revertCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);

                expected = "{\"messageType\": \"requestError\", \"cellName\": \"" + cellName +
                    "\",\"message\":";

                if(message.find(expected) != std::string::npos)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << e.what() << "\n";
                std::cout << "Fail\n";
            }
            break;
        case 16:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing revert functionality, test 16\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "16\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail\n";
                   return 1;
                }

                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                std::string expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                std::string contents = "100";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                toSend = "{\"requestType\": \"revertCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + "" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                contents = "1";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                toSend = "{\"requestType\": \"revertCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + "" + "\"}";

                if(message == expected)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        case 17:
            std::cout << (TEST_TIMEOUT_MS / 1000.0) << "\n";
            std::cout << "Testing revert + undo functionality, test 17\n";
            try
            {
                std::string name = "Test_Client\n";

                if (serverSocket.send(name.c_str(), sizeof(char) * name.length()) != sf::Socket::Done)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::vector<std::string> fileNames;
                std::string message = "test";

                while(message != "")
                {
                    message = receiveMessage(&serverSocket, 100);
                    fileNames.push_back(message);
                }
                std::string fileSelect = "17\n";

                serverSocket.send(fileSelect.c_str(), sizeof(char) * fileSelect.length());
                message = receiveMessage(&serverSocket, 100);
                double ID;

                if(!GetID(message, &serverSocket, ID))
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                std::string cellName = "A1";
                std::string toSend = "{\"requestType\": \"selectCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                std::string expected = "{\"messageType\": \"cellSelected\" , \"cellName\" : \""+ cellName + "\",\"selector\" : \""
                    + std::to_string((int)ID) + "\",\"selectorName\":\"" + "Test_Client" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                std::string contents = "Test";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                contents = "table";
                toSend = "{\"requestType\": \"editCell\", \"cellName\": \"" + cellName + "\",\"contents\": \"" + contents + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                // This json might be wrong vvv
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + contents + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                toSend = "{\"requestType\": \"revertCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + "Test" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                cellName = "A1";
                toSend = "{\"requestType\": \"revertCell\", \"cellName\": \"" + cellName + "\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + "" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                toSend = "{\"requestType\": \"undo\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + "Test" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                toSend = "{\"requestType\": \"undo\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + "table" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                toSend = "{\"requestType\": \"undo\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + "Test" + "\"}";
                if(message != expected)
                {
                    std::cout << "Fail\n";
                    return 1;
                }

                toSend = "{\"requestType\": \"undo\"}\n";
                serverSocket.send(toSend.c_str(), sizeof(char) * toSend.length());
                message = receiveMessage(&serverSocket, 10000);
                expected ="{\"messageType\": \"cellUpdated\" , \"cellName\" : \""+ cellName + "\",\"contents\" : \"" + "" + "\"}";

                if(message == expected)
                {
                    std::cout << "Pass\n";
                }
                else
                {
                    std::cout << "Fail\n";
                }
            }
            catch(const std::exception& e)
            {
                std::cerr << e.what() << '\n';
                std::cout << "Fail\n";
            }
            break;
        default:
            break;
    }
}
    catch(...)
    {
        std::cout << "Fail\n";
    }

    return 0;
}


bool TryParse(const std::string& s, double& num)
{
    bool isNum = std::regex_match(s, std::regex("^[-+]?\\d+$"));

    if (!isNum)
    {
        return false;
    }

    try
    {
        double i = std::stoi(s);
        num = i;
        return true;
    }
    catch(const std::exception& e)
    {
        return false;
    }

}

bool GetID(std::string &message, sf::TcpSocket *socket, double& ID)
{
    int maxLoops = 2574;
    int currentLoop = 0;

    while(currentLoop < maxLoops && !TryParse(message, ID))
    {
        message = receiveMessage(socket, 100);
    }

    if(currentLoop >= maxLoops)
    {
        return false;
    }
    return true;
}


std::string generateRandomString()
{
    std::string str("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
    std::random_device rd;
    std::mt19937 generator(rd());

    std::shuffle(str.begin(), str.end(), generator);

    return str.substr(0, 32);
}
