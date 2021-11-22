using System;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.IO;
using LibData;
using System.Text;


namespace LibServer
{
    // Note: Do not change this class.
    public class Setting
    {
        public int ServerPortNumber { get; set; }
        public int BookHelperPortNumber { get; set; }
        public int UserHelperPortNumber { get; set; }
        public string ServerIPAddress { get; set; }
        public string BookHelperIPAddress { get; set; }
        public string UserHelperIPAddress { get; set; }
        public int ServerListeningQueue { get; set; }
    }


    // Note: Complete the implementation of this class. You can adjust the structure of this class. 
    public class SequentialServer
    {

        public Setting setting;
        public Socket bookhelperSocket;
        public Socket localSocket;
        public Socket userhelperSocket;

        public SequentialServer()
        {
            this.setting = JsonSerializer.Deserialize<Setting>(File.ReadAllText(@"../ClientServerConfig.json")); 
            this.bookhelperSocket = new Socket(AddressFamily.InterNetwork,
                                    SocketType.Stream, ProtocolType.Tcp);
            this.localSocket =      new Socket(AddressFamily.InterNetwork,
                                    SocketType.Stream, ProtocolType.Tcp);
            this.userhelperSocket = new Socket(AddressFamily.InterNetwork,
                                    SocketType.Stream, ProtocolType.Tcp);
            //todo: implement the body. Add extra fields and methods to the class if it is needed
        }

        public void start()
        {
            try
            {
                IPAddress bookserveripAddress = IPAddress.Parse(setting.BookHelperIPAddress);
                IPEndPoint bookserverEndpoint = new IPEndPoint(bookserveripAddress, setting.BookHelperPortNumber);
                bookhelperSocket.Connect(bookserverEndpoint);
                IPAddress localipAddress = IPAddress.Parse(setting.ServerIPAddress);
                IPEndPoint localEndpoint = new IPEndPoint(localipAddress, setting.ServerPortNumber);   
                IPAddress userserveripAdress = IPAddress.Parse(setting.UserHelperIPAddress);
                IPEndPoint userserverEndpoint = new IPEndPoint(userserveripAdress, setting.UserHelperPortNumber);
                userhelperSocket.Connect(userserverEndpoint);
                localSocket.Bind(localEndpoint); 
                localSocket.Listen(setting.ServerListeningQueue);


                while(true)
                {   
                    Console.WriteLine("waiting for clients");
                    Socket client = localSocket.Accept();
                    Console.WriteLine("Connected!");
                    if (Communication(client) == 0)
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Server Exception] {0}", e.Message);
            }
            //todo: implement the body. Add extra fields and methods to the class if it is needed

        }


    public int Communication(Socket client)
    {
            bool communication = true;
            while (communication) 
                    {      
                        Message JsonObj = receiveMessage(client); 
                        // Sends the First Hello message                           
                        if (JsonObj.Type == MessageType.Hello) 
                        {
                            sendMessage(MessageType.Welcome,"", client);
                        }  
                        //Check if Response from client is bookinquiry 
                        else if (JsonObj.Type == MessageType.BookInquiry){
                            // Sends message to the bookhelper with the book inquiry
                            sendMessage(MessageType.BookInquiry, JsonObj.Content, bookhelperSocket);
                            // Receives the response from the bookhelper server
                            Message bookHelperResponse = receiveMessage(bookhelperSocket);

                            // Check if the response is a bookinquiry reply
                            if (bookHelperResponse.Type == MessageType.BookInquiryReply)
                            {              
                                // Sends message to the client with the content of the book information                                                
                                sendMessage(MessageType.BookInquiryReply, bookHelperResponse.Content, client);               
                                Message userInquiryResponse = receiveMessage(client);
                                // Check if receive message is a userinquiry from the client
                                if (userInquiryResponse.Type ==  MessageType.UserInquiry)
                                {
                                    sendMessage(userInquiryResponse.Type, userInquiryResponse.Content, userhelperSocket);
                                    // Receive the response from userhelper socket
                                    Message responseFromUserHelper = receiveMessage(userhelperSocket); 

                                    if (responseFromUserHelper.Type == MessageType.NotFound){
                                        sendMessage(responseFromUserHelper.Type, responseFromUserHelper.Content, client);
                                        break;
                                    }


                                    // Send the response to the client 
                                    sendMessage(responseFromUserHelper.Type, responseFromUserHelper.Content, client);
                                    break;
                                }
                                // Workaround for if book is available else the program will crash 
                                else if(userInquiryResponse.Type == MessageType.Error){
                                    break;
                                }
                            }
                                     
                        // Check if response is a not found messagetype
                        else if (bookHelperResponse.Type == MessageType.NotFound)
                        {   
                            // Sends message to the client with the notfound message
                            sendMessage(bookHelperResponse.Type, bookHelperResponse.Content, client);
                            break;
                        }


                        }
                        // Check if receive message is a endcommunication message from the client and sends it to the helperservers
                        else if (JsonObj.Type == MessageType.EndCommunication)
                        {
                            sendMessage(MessageType.EndCommunication,"", bookhelperSocket);
                            sendMessage(MessageType.EndCommunication,"", userhelperSocket);
                            Console.WriteLine("Closing socket");
                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                            return 0;
                        }
                    }
                return 1;
    }
    
        public void sendMessage(MessageType msg, string cont, Socket s){
            Message message = new Message{
                Type = msg,
                Content = cont
            };
            string output = JsonSerializer.Serialize(message);
            Byte[] data = Encoding.ASCII.GetBytes(output);
            //Semd has 4 parameters (buffer(actual data), offset, messagesize, socketflags)
           s.Send(data,0,data.Length,SocketFlags.None);
            
        }

        public Message receiveMessage(Socket s)
        {
            // Buffer to store the response bytes.
            byte[] buffer = new Byte[256];
            int messageLength = s.Receive(buffer);
            string data = Encoding.ASCII.GetString(buffer, 0, messageLength);  
            return JsonSerializer.Deserialize<Message>(data);
        }

    }

}



