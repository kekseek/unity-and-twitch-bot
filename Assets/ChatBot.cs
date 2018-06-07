using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using UnityEngine;

public class ChatBot : MonoBehaviour {
    GameObject sphere;
    Rigidbody sphereBody;
    List<ChatMoveCommand> chatCommands;

    TcpClient tcpClient;
    StreamReader reader;
    StreamWriter writer;

    string userName, password, channelName, prefixForSendingChatMessages;
    DateTime lastMessageSendTime;

    Queue<string> sendMessageQueue;
    int iFixedUpdateCounter = 0;

    public void Start() {
        chatCommands = new List<ChatMoveCommand>();
        sendMessageQueue = new Queue<string>();

        /* account data */
        this.userName = "USER_NAME".ToLower();
        this.channelName = userName;
        this.password = File.ReadAllText("password.txt");
        prefixForSendingChatMessages = String.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :", userName, channelName);
        /* ============ */

        /* unity objects */ 
        sphere = GameObject.Find("Sphere");
        sphereBody = sphere.GetComponent<Rigidbody>();
        /* ============= */

        Reconnect();
    }

    public void SendTwitchMessage(string message) {
        sendMessageQueue.Enqueue(message);
    }

    /* log in */
    void Reconnect() {
        tcpClient = new TcpClient("irc.twitch.tv", 6667);
        reader = new StreamReader(tcpClient.GetStream());
        writer = new StreamWriter(tcpClient.GetStream());
        writer.AutoFlush = true;

        writer.WriteLine("PASS {0}\r\nNICK {1}\r\nUser {1} 8 * :{1}", password, userName);
        writer.WriteLine("JOIN #USER_NAME");
        lastMessageSendTime = DateTime.Now;
    }

    void Update() {
        if (!tcpClient.Connected) {
            Reconnect();
        }

        TrySendingMessages();
        TryReceiveMessages();
    }

    /* processing best move */
    private void FixedUpdate() {
        iFixedUpdateCounter++;
        if (iFixedUpdateCounter >= 100) {
            iFixedUpdateCounter = 0;

            if (chatCommands.Count > 0) {
                print(String.Format("There are {0} items in queue", chatCommands.Count));

                int bestMoveCount = 0;
                ChatMoveCommand.MoveType bestMove = ChatMoveCommand.MoveType.forward;
                foreach (ChatMoveCommand.MoveType moveType in Enum.GetValues(typeof(ChatMoveCommand.MoveType))) {
                    int moveTypeCount = 0;
                    foreach (var command in chatCommands) {
                        if (command.type == moveType) {
                            moveTypeCount++;
                        }
                    }

                    if (moveTypeCount >= bestMoveCount) {
                        bestMoveCount = moveTypeCount;
                        bestMove = moveType;
                    }

                    Vector3 force;
                    switch (bestMove) {
                        case ChatMoveCommand.MoveType.left:
                            force = Vector3.left;
                            break;
                        case ChatMoveCommand.MoveType.forward:
                            force = Vector3.forward;
                            break;
                        case ChatMoveCommand.MoveType.back:
                            force = Vector3.back;
                            break;
                        case ChatMoveCommand.MoveType.right:
                        default:
                            force = Vector3.right;
                            break;
                    }
                    force *= 1000;
                    sphereBody.AddForce(force);

                    var dropCommandsIssuedBefore = DateTime.Now - TimeSpan.FromSeconds(30);
                    for (int i = chatCommands.Count - 1; i >= 0; i--) {
                        if (chatCommands[i].time < dropCommandsIssuedBefore) {
                            print("dropping a command");
                            chatCommands.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }

    /* METHODS for receive and display chat messages */
    void TryReceiveMessages() {
        if (tcpClient.Available > 0) {
            var message = reader.ReadLine();

            print(String.Format("\r\nNew message: {0}", message));

            var iCollon = message.IndexOf(":", 1);
            if (iCollon > 0) {
                var command = message.Substring(1, iCollon);
                if (command.Contains("PRIVMSG #")) {
                    var iBang = command.IndexOf("!");

                    if (iBang > 0) {
                        var speaker = command.Substring(0, iBang);
                        var chatMessage = message.Substring(iCollon + 1);

                        ReceiveMessage(speaker, chatMessage);
                    }

                }
            }

        }
    }

    void ReceiveMessage(string speaker, string message) {
        print(String.Format("\r\n {0}: {1}", speaker, message));

        if (message.StartsWith("!hi")) {
            SendTwitchMessage(String.Format("Hello, {0}", speaker));
        }

        /* play with a ball (xD) */
        var amount = 1000;
        if (message.ToLower().Contains("left")) {
            chatCommands.Add(new ChatMoveCommand(ChatMoveCommand.MoveType.left));
        }
        if (message.ToLower().Contains("right")) {
            chatCommands.Add(new ChatMoveCommand(ChatMoveCommand.MoveType.right));
        }
        if (message.ToLower().Contains("forward")) {
            chatCommands.Add(new ChatMoveCommand(ChatMoveCommand.MoveType.forward));
        }
        if (message.ToLower().Contains("back")) {
            chatCommands.Add(new ChatMoveCommand(ChatMoveCommand.MoveType.back));
        }

    }

    void TrySendingMessages() {
        if (DateTime.Now - lastMessageSendTime > TimeSpan.FromSeconds(10)) {
            if (sendMessageQueue.Count > 0) {
                var message = sendMessageQueue.Dequeue();
                writer.WriteLine(String.Format("{0}{1}", prefixForSendingChatMessages, message));
                lastMessageSendTime = DateTime.Now;
            }
        }
    }
}

