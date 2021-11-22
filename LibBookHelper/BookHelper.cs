using System;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.IO;
using LibData;
using System.Text;

namespace BookHelper
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
    public class SequentialHelper
    {
        public Setting setting;
        public Socket localSocket;

        public SequentialHelper()
        {
            //todo: implement the body. Add extra fields and methods to the class if needed
            this.setting = JsonSerializer.Deserialize<Setting>(File.ReadAllText(@"../ClientServerConfig.json")); 
            this.localSocket =  new Socket(AddressFamily.InterNetwork,
                                SocketType.Stream, ProtocolType.Tcp);
        }

        public void start()
        {   
            //todo: implement the body. Add extra fields and methods to the class if needed
            try
            {
                IPAddress bookserverIpadress = IPAddress.Parse(setting.BookHelperIPAddress);
                IPEndPoint bookserverEndpoint = new IPEndPoint(bookserverIpadress, setting.BookHelperPortNumber);     
                localSocket.Bind(bookserverEndpoint);
                localSocket.Listen(setting.ServerListeningQueue);
                
                Console.WriteLine("\n Waiting for server..");   
                Socket handler = this.localSocket.Accept();
                bool communicate = true;
                Console.WriteLine("Connected!");
 

                while(communicate)
                {
                    Message data = receiveMessage(handler);

                    Console.WriteLine(data.Type + " " + data.Content);
                    if (data.Type == MessageType.BookInquiry)
                    {
                        string jsonObject = checkJson(data);
                        if (string.IsNullOrEmpty(jsonObject))
                        {
                            sendMessage(MessageType.NotFound, "BookNotFound", handler);
                        }
                        else{
                            sendMessage(MessageType.BookInquiryReply,jsonObject,handler);
                            Console.WriteLine(jsonObject);
                        }
                    }

                    else if (data.Type == MessageType.EndCommunication)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        communicate = !communicate;
                    }
                }
            }

            catch (Exception e)
            {
                Console.Out.WriteLine("[Server Exception] {0}", e.Message);
            }
        }
        
        public string checkJson(Message obj)
        {
            string jsonobject = null;
            BookData[] information = JsonSerializer.Deserialize<BookData[]>(File.ReadAllText(@"./Books.json"));
            for (int i=0; i<information.Length; i++){
                if (obj.Content == information[i].Title)
                    jsonobject = JsonSerializer.Serialize(information[i]);
            }
        return jsonobject;
        }
      
 
        
        public void sendMessage(MessageType msg, string cont, Socket handler){
            Message message = new Message{
                Type = msg,
                Content = cont
            };
            string output = JsonSerializer.Serialize(message);
            Byte[] data = Encoding.ASCII.GetBytes(output);
            //Send has 4 parameters (buffer(actual data), offset, messagesize, socketflags)
            handler.Send(data,0,data.Length,SocketFlags.None);
        }

        public Message receiveMessage(Socket handler)
        {
            // Buffer to store the response bytes.
            byte[] buffer = new Byte[256];
            int messageLength = handler.Receive(buffer);
            string data = Encoding.ASCII.GetString(buffer, 0, messageLength);  
            return JsonSerializer.Deserialize<Message>(data);
        }


    }
}
