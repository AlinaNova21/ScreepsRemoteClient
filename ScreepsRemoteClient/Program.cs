using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;

namespace ScreepsRemoteClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Task main = MainAsync(args);
            main.Wait();
        }

        static async Task MainAsync(string[] args)
        {
            Console.WriteLine("MainAsync");
            // Change userid below to your userid obtained from steam client console.
            Client client = new Client("screeps-test.ags131.ovh",21028,"5779bae444eb975");
            Console.WriteLine("Awaiting terrain data");
            dynamic terrainData = await client.GetTerrainData();
            Console.WriteLine("Looping");
            while (true)
            {
                dynamic runtimeData = await client.GetRuntimeData();
                dynamic ret = RunCode(terrainData, runtimeData);
                client.SendTick(ret);
                Console.WriteLine("Tick {0}", runtimeData.time);
            }
        }

        static dynamic RunCode(dynamic terrainData,dynamic runtimeData)
        {
            ReturnData ret = new ReturnData();
            ret.Console.Log.Add(String.Format("Hello from C#! {0}",runtimeData.time));
            List<dynamic> roomObjects = (from ro in ((JObject)runtimeData.roomObjects).Values<JProperty>()
                                         select ((JProperty)ro).Value).ToList<dynamic>();
            string spawnid = (from ro in roomObjects
                              where ro.type == "spawn"
                              select ro._id).SingleOrDefault();
            if(spawnid != null)
                ret.AddIntent("W2N5", spawnid, new CreateCreepIntent("testing",new List<string>() { "move" }));
            List<dynamic> creeps = (from ro in roomObjects
                                    where ro.type == "creep"
                                    select ro).ToList<dynamic>();
            Random rand = new Random();
            foreach(dynamic creep in creeps) {
                ret.AddIntent((string)creep.room, (string)creep._id, new MoveIntent(rand.Next(0, 9)));
                ret.AddIntent((string)creep.room, (string)creep._id, new SayIntent("Random!"));
            }
            return ret;
        }

    }
}
