using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace RavenM;

public class DevtoolsPipeServer
{
    private TcpListener _tcpListener;
    private Thread _tcpListenerThread;
    private TcpClient _connectedTcpClient;

    public EventHandler<string> OnTcpClientConnected;

    public DevtoolsPipeServer()
    {
        _tcpListenerThread = new Thread(new ThreadStart(ServerThread_Read))
        {
            IsBackground = true
        };
        _tcpListenerThread.Start();
    }

    private void ServerThread_Read()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 13347);
            _tcpListener.Start();

            Plugin.logger.LogInfo("Devtools pipe - Listening on 127.0.0.1, port 13347");

            byte[] bytes = new byte[1024];

            while (true)
            {
                using (_connectedTcpClient = _tcpListener.AcceptTcpClient())
                {
                    Plugin.logger.LogInfo("Devtools pipe - Client connected");
                    OnTcpClientConnected?.Invoke(this, "Connected");
                    using (NetworkStream stream = _connectedTcpClient.GetStream())
                    {
                        int length;
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incomingData = new byte[length];
                            Array.Copy(bytes, 0, incomingData, 0, length);
                            string clientMessage = Encoding.ASCII.GetString(incomingData);
                            Plugin.logger.LogInfo("Devtools pipe - Received message: " + clientMessage);
                        }
                    }
                }
            }
        }
        catch (SocketException e)
        {
            Plugin.logger.LogError("Devtools pipe - socket exception reading: " + e);
        }
    }

    public void SendMessage(string message)
    {
        ServerThread_Write(message);
    }

    private void ServerThread_Write(string message)
    {
        if (_connectedTcpClient == null) 
        {             
            return;         
        }  		
		
        try 
        { 			
            NetworkStream stream = _connectedTcpClient.GetStream(); 			
            if (stream.CanWrite)
            {
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);               
                Plugin.logger.LogInfo("Devtools pipe - Sent message: " + message);           
            }       
        } 		
        catch (SocketException e) 
        {             
            Plugin.logger.LogError("Devtools pipe - socket exception writing: " + e);         
        } 	
    }
}