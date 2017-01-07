using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace ScreepsRemoteClient
{
    class Client
    {
        Socket socket;
        NetworkStream ns;
        StreamReader sr;
        StreamWriter sw;
        dynamic TerrainData = null;
        TaskCompletionSource<dynamic> RuntimeDataCompletion;
        TaskCompletionSource<dynamic> TerrainDataCompletion;

        public Client(string host, int port, string userID) {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(host,port);
            Trace.WriteLine(String.Format("Connected {0}",socket.Connected));
            ns = new NetworkStream(socket, true);
            UTF8Encoding encoding = new UTF8Encoding(false);
            sr = new StreamReader(ns, encoding);
            sw = new StreamWriter(ns, encoding);
            sw.AutoFlush = true;
            SendEvent(new RemoteEvent() { name = "auth", data = userID });
            Thread readThread = new Thread(new ThreadStart(() =>
            {
                Task task = ReadLoop();
                task.Wait();
            }));
            readThread.Start();
        }

        public async Task<dynamic> GetRuntimeData()
        {
            RuntimeDataCompletion = new TaskCompletionSource<dynamic>();
            return await RuntimeDataCompletion.Task;
        }

        public async Task<dynamic> GetTerrainData()
        {
            TerrainDataCompletion = new TaskCompletionSource<dynamic>();
            if (TerrainData != null)
            {
                TerrainDataCompletion.SetResult(TerrainData);
            }
            return await TerrainDataCompletion.Task;
        }

        async Task ReadLoop()
        {
            string line;
            RemoteEvent ev;
            while (socket.Connected)
            {
                line = await sr.ReadLineAsync();
                if (line == null) continue;
                ev = JsonConvert.DeserializeObject<RemoteEvent>(line);
                Trace.WriteLine(String.Format("RecvEvent {0} {1}", ev.name, line));
                switch (ev.name)
                {
                    case "terrainData":
                        if (TerrainDataCompletion != null)
                            TerrainDataCompletion.SetResult(ev.data);
                        TerrainData = ev.data;
                        break;
                    case "runtimeData":
                        if (RuntimeDataCompletion != null)
                            RuntimeDataCompletion.SetResult(ev.data);
                        break;
                }
            }
        }

        public void SendTick(ReturnData data)
        {
            SendEvent(new RemoteEvent() { name = "tick", data = data });
        }

        void SendEvent(RemoteEvent e)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(e);
            Trace.WriteLine(String.Format("SendEvent {0} -- {1}",e.name,json));
            sw.WriteLine(json);
        }
    }

    class RemoteEvent {
        [JsonProperty(PropertyName="event",Required = Required.Always)]
        public string name;
        [JsonProperty(Required = Required.Always)]
        public dynamic data;
    }

    class ReturnData
    {
        [JsonProperty("console")]
        public ScreepsConsole Console = new ScreepsConsole();
        [JsonProperty("intents")]
        public Dictionary<string, Dictionary<string, Dictionary<string, Intent>>> Intents = new Dictionary<string, Dictionary<string, Dictionary<string, Intent>>>();
        public void AddIntent(string room, string id, Intent intent)
        {
            if (!Intents.ContainsKey(room))
                Intents.Add(room, new Dictionary<string, Dictionary<string, Intent>> ());
            if (!Intents[room].ContainsKey(id))
                Intents[room].Add(id, new Dictionary<string, Intent>());
            Intents[room][id].Add(intent.IntentType, intent);
        }
    }

    class Intent
    {
        [JsonIgnore()]
        public string IntentType;
    }

    class CreateCreepIntent : Intent
    {
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("body")]
        public List<string> Body = new List<string>();
        [JsonProperty("memory")]
        public dynamic Memory;
        public CreateCreepIntent(string name, List<string> body, dynamic memory = null)
        {
            IntentType = "createCreep";
            Name = name;
            Body = body;
            Memory = memory;
        }
    }

    class MoveIntent : Intent
    {
        [JsonProperty("direction")]
        public int Direction;
        public MoveIntent(int direction = 0)
        {
            IntentType = "move";
            Direction = direction;
        }
    }

    class SayIntent : Intent
    {
        [JsonProperty("message")]
        public string Message;
        public SayIntent(string message = "")
        {
            IntentType = "say";
            Message = message;
        }
    }

    class ScreepsConsole
    {
        [JsonProperty("log")]
        public List<string> Log = new List<string>();
        [JsonProperty("error")]
        public List<string> Error = new List<string>();
        [JsonProperty("result")]
        public List<string> Result = new List<string>();
    }
}